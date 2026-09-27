using System;
using System.Threading;
using System.Threading.Tasks;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Timing.Unity3D;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Diagnostics;
using Project.InteractionAutomationLab.Presentation;
using Project.InteractionAutomationLab.Verification;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Scenarios
{
    public enum LabSessionState
    {
        Idle = 0,
        Running = 1,
        Stopping = 2
    }

    public sealed class LabSessionController : MonoBehaviour
    {
        [SerializeField] private LabProductController product;
        [SerializeField] private Transform subjectRoot;
        [SerializeField] private UnityFrameInputProcessingBoundary inputProcessingBoundary;
        [SerializeField] private Text diagnosticsText;

        private CancellationTokenSource runCancellation;
        private UnityPhysicalInputDriver physicalInput;
        private CountingPhysicalInputDriver countingInput;
        private Task activeRun;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
        private bool inputSettingsOverridden;

        public LabSessionState State { get; private set; }
        public LabAcceptanceReport LastAcceptanceReport { get; private set; }
        public LabCampaignReport LastCampaignReport { get; private set; }
        public string LastExportPath { get; private set; }

        public async void RunScriptedAcceptance()
        {
            try
            {
                await RunScriptedAcceptanceAsync();
            }
            catch (OperationCanceledException)
            {
                UpdateDiagnostics("Scripted acceptance cancelled after cleanup.");
            }
            catch (Exception exception)
            {
                UpdateDiagnostics("Scripted acceptance infrastructure failure:\n" + exception.Message);
                Debug.LogException(exception);
            }
        }

        public async Task<LabAcceptanceReport> RunScriptedAcceptanceAsync()
        {
            EnsureCanStart();
            State = LabSessionState.Running;
            runCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = runCancellation.Token;
            activeRun = RunAcceptanceCoreAsync(cancellationToken);
            try
            {
                await activeRun;
                return LastAcceptanceReport;
            }
            finally
            {
                DisposeRunResources();
                activeRun = null;
                State = LabSessionState.Idle;
            }
        }

        public async void RunFrozenMonkeyCampaign()
        {
            try
            {
                await RunFrozenMonkeyCampaignAsync();
            }
            catch (OperationCanceledException)
            {
                UpdateDiagnostics("Monkey campaign cancelled after cleanup.");
            }
            catch (Exception exception)
            {
                UpdateDiagnostics("Monkey campaign infrastructure failure:\n" + exception.Message);
                Debug.LogException(exception);
            }
        }

        public async Task<LabCampaignReport> RunFrozenMonkeyCampaignAsync()
        {
            EnsureCanStart();
            State = LabSessionState.Running;
            runCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = runCancellation.Token;
            activeRun = RunCampaignCoreAsync(cancellationToken);
            try
            {
                await activeRun;
                return LastCampaignReport;
            }
            finally
            {
                DisposeRunResources();
                activeRun = null;
                State = LabSessionState.Idle;
            }
        }

        public async void Stop()
        {
            if (State != LabSessionState.Running || runCancellation == null) return;
            State = LabSessionState.Stopping;
            runCancellation.Cancel();
            try
            {
                if (activeRun != null) await activeRun;
            }
            catch (OperationCanceledException)
            {
                UpdateDiagnostics("Stopped. Pointer cleanup completed before returning to Idle.");
            }
        }

        public void ResetLab()
        {
            if (State != LabSessionState.Idle) throw new InvalidOperationException("Reset is available only while the Lab is Idle.");
            product.ResetProduct();
            LastAcceptanceReport = null;
            LastCampaignReport = null;
            LastExportPath = string.Empty;
            UpdateDiagnostics("Manual Explore\nReady. Last result cleared.");
        }

        public void ExportLastResult()
        {
            if (State != LabSessionState.Idle) throw new InvalidOperationException("Export is available only while the Lab is Idle.");
            if (LastCampaignReport != null) LastExportPath = LabEvidenceExporter.Export(LastCampaignReport);
            else if (LastAcceptanceReport != null) LastExportPath = LabEvidenceExporter.Export(LastAcceptanceReport);
            else throw new InvalidOperationException("There is no completed result to export.");
            UpdateDiagnostics("Evidence exported:\n" + LastExportPath);
        }

        private async Task RunAcceptanceCoreAsync(CancellationToken cancellationToken)
        {
            ConfigureSessionInputFocus();
            physicalInput = new UnityPhysicalInputDriver();
            InputSystem.EnableDevice(physicalInput.Mouse);
            EnableUiActions();
            countingInput = new CountingPhysicalInputDriver(physicalInput);
            LabTargetBindingSource bindings = new LabTargetBindingSource(subjectRoot, LabBindingScope.ProductScope);
            LabAcceptanceSuite suite = new LabAcceptanceSuite(product, bindings, countingInput, inputProcessingBoundary, physicalInput.Mouse.deviceId);
            UpdateDiagnostics("Running scripted acceptance S01-S08 and S10...");
            await inputProcessingBoundary.WaitAsync(cancellationToken);
            LastAcceptanceReport = await suite.RunAsync(cancellationToken);
            string status = LastAcceptanceReport.Passed ? "PASS" : "FAIL";
            UpdateDiagnostics("Scripted Acceptance " + status + "\n" + FormatScenarios(LastAcceptanceReport));
        }

        private async Task RunCampaignCoreAsync(CancellationToken cancellationToken)
        {
            ConfigureSessionInputFocus();
            physicalInput = new UnityPhysicalInputDriver();
            InputSystem.EnableDevice(physicalInput.Mouse);
            EnableUiActions();
            countingInput = new CountingPhysicalInputDriver(physicalInput);
            LabTargetBindingSource bindings = new LabTargetBindingSource(subjectRoot, LabBindingScope.FrozenMonkeyScope);
            LabMonkeyCampaignSuite suite = new LabMonkeyCampaignSuite(product, countingInput, physicalInput, inputProcessingBoundary, bindings);
            UpdateDiagnostics("Running Frozen Monkey: 4 presets x 3 seeds x 64 steps, plus deterministic replays...");
            await inputProcessingBoundary.WaitAsync(cancellationToken);
            LastCampaignReport = await suite.RunAsync(cancellationToken);
            string status = LastCampaignReport.Passed ? "PASS" : "FAIL";
            UpdateDiagnostics("Frozen Monkey " + status + "\n" + FormatCampaign(LastCampaignReport));
        }

        private void EnsureCanStart()
        {
            if (State != LabSessionState.Idle) throw new InvalidOperationException("A Lab session is already running or stopping.");
            if (product == null || subjectRoot == null || inputProcessingBoundary == null) throw new InvalidOperationException("Lab session composition is incomplete.");
        }

        private static void EnableUiActions()
        {
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) throw new InvalidOperationException("No active EventSystem was found.");
            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null || module.actionsAsset == null) throw new InvalidOperationException("InputSystemUIInputModule actions are unavailable.");
            module.actionsAsset.Enable();
        }

        private void DisposeRunResources()
        {
            runCancellation?.Dispose();
            runCancellation = null;
            physicalInput?.Dispose();
            physicalInput = null;
            countingInput = null;
            RestoreSessionInputFocus();
        }

        private void ConfigureSessionInputFocus()
        {
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            inputSettingsOverridden = true;
        }

        private void RestoreSessionInputFocus()
        {
            if (!inputSettingsOverridden) return;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
            inputSettingsOverridden = false;
        }

        private void UpdateDiagnostics(string message)
        {
            if (diagnosticsText != null) diagnosticsText.text = message;
        }

        private static string FormatScenarios(LabAcceptanceReport report)
        {
            string value = "";
            foreach (LabScenarioResult scenario in report.scenarios)
            {
                value += scenario.id + " " + scenario.status + " input=" + scenario.physicalSubmissionDelta + "\n";
                if (scenario.status != LabScenarioStatus.Passed) value += scenario.detail + "\n";
            }
            return value;
        }

        private static string FormatCampaign(LabCampaignReport report)
        {
            string value = "";
            foreach (LabMonkeyRunSummary run in report.runs)
            {
                value += run.preset + " seed=" + run.seed + " " + (run.Passed ? "PASS" : "FAIL") +
                    " coverage=" + run.coveredCount + "/" + run.actualEligibleCount + " steps=" + run.completedIterations + "/" + run.requestedIterations + "\n";
            }
            return value;
        }

        private void OnDisable()
        {
            runCancellation?.Cancel();
        }

        private void OnDestroy()
        {
            runCancellation?.Cancel();
        }
    }
}
