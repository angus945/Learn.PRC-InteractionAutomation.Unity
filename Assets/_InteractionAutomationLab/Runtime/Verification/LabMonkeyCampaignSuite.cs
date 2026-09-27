using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Timing;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Domain;
using Project.InteractionAutomationLab.Presentation;

namespace Project.InteractionAutomationLab.Verification
{
    public sealed class LabMonkeyCampaignSuite
    {
        private static readonly int[] Seeds = { 12345, 7, 98765 };
        private readonly LabProductController product;
        private readonly CountingPhysicalInputDriver input;
        private readonly UnityPhysicalInputDriver unityInput;
        private readonly IHostInputProcessingBoundary boundary;
        private readonly LabTargetBindingSource bindings;

        public LabMonkeyCampaignSuite(LabProductController product, CountingPhysicalInputDriver input,
            UnityPhysicalInputDriver unityInput, IHostInputProcessingBoundary boundary, LabTargetBindingSource bindings)
        {
            this.product = product ?? throw new ArgumentNullException(nameof(product));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.unityInput = unityInput ?? throw new ArgumentNullException(nameof(unityInput));
            this.boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public async ValueTask<LabCampaignReport> RunAsync(CancellationToken cancellationToken)
        {
            LabCampaignReport campaign = new LabCampaignReport();
            campaign.runId = "campaign-" + Guid.NewGuid().ToString("N");
            campaign.startedUtc = DateTime.UtcNow.ToString("O");
            foreach (LabMonkeyPreset preset in Enum.GetValues(typeof(LabMonkeyPreset)))
            {
                foreach (int seed in Seeds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    RunCapture primary = await ExecuteAsync(preset, seed, cancellationToken);
                    RunCapture replay = await ExecuteAsync(preset, seed, cancellationToken);
                    LabMonkeyRunSummary summary = CreateSummary(preset, seed, primary);
                    summary.deterministicReplay = SequencesEqual(primary.Sequence, replay.Sequence);
                    if (!replay.Passed && string.IsNullOrEmpty(summary.failure)) summary.failure = "Deterministic replay did not independently pass.";
                    campaign.runs.Add(summary);
                }
            }
            campaign.completedUtc = DateTime.UtcNow.ToString("O");
            return campaign;
        }

        private async ValueTask<RunCapture> ExecuteAsync(LabMonkeyPreset preset, int seed, CancellationToken cancellationToken)
        {
            ApplyPreset(preset);
            await boundary.WaitAsync(cancellationToken);
            UnityInteractionAutomationRuntime runtime = LabComposition.CreateProductionRuntime(product, input, boundary, bindings);
            SeededPointerMonkey monkey = UnityPointerMonkeyFactory.Create(runtime,
                new PointerMonkeyConfiguration(seed, new ScrollDelta(0, -1), TimeSpan.FromMilliseconds(250)), new LabDragDestinationPolicy());
            UnityPointerMonkeyVerifier verifier = new UnityPointerMonkeyVerifier(bindings, unityInput.Mouse.deviceId);
            LabMonkeyRunAdapter adapter = new LabMonkeyRunAdapter(product, bindings);
            UnityPointerMonkeyQaRunner qa = new UnityPointerMonkeyQaRunner(monkey, verifier, adapter, adapter);
            UnityPointerMonkeyRunReport report = await qa.RunAsync(64, 128, cancellationToken);
            List<string> sequence = new List<string>(report.RetainedSteps.Count);
            foreach (UnityPointerMonkeyStepResult step in report.RetainedSteps)
            {
                string key = step.DestinationTargetId.Length == 0 ? step.ActionId + ":" + step.TargetId :
                    step.ActionId + ":" + step.TargetId + "->" + step.DestinationTargetId;
                sequence.Add(key);
            }
            return new RunCapture(report, sequence);
        }

        private void ApplyPreset(LabMonkeyPreset preset)
        {
            product.ResetProduct();
            product.QuickBypassGroup.ignoreParentGroups = true;
            product.QuickBypassGroup.interactable = true;
            if (preset == LabMonkeyPreset.M1ParentUiLocked) product.GameplayLockGroup.interactable = false;
            if (preset == LabMonkeyPreset.M2Cutscene)
                product.SetFixtureState(LabGameMode.Cutscene, false, product.State.Gold, product.State.PotionCount);
            if (preset == LabMonkeyPreset.M3Settings) product.ShowSettings();
        }

        private static LabMonkeyRunSummary CreateSummary(LabMonkeyPreset preset, int seed, RunCapture capture)
        {
            IReadOnlyList<string> expected = LabExpectedManifests.Get(preset);
            HashSet<string> expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
            LabMonkeyRunSummary summary = new LabMonkeyRunSummary();
            summary.preset = preset;
            summary.seed = seed;
            summary.requestedIterations = capture.Report.RequestedIterations;
            summary.completedIterations = capture.Report.CompletedIterations;
            summary.expectedEligibleCount = expected.Count;
            summary.actualEligibleCount = capture.Report.Coverage.EligibleCount;
            summary.coveredCount = capture.Report.Coverage.CoveredCount;
            summary.hostAndProductPassed = capture.Report.Passed;
            summary.verdict = capture.Report.Verdict.ToString();
            summary.sequence.AddRange(capture.Sequence);
            summary.coverageFirst = CoverageFirstMatches(capture.Sequence, expectedSet);
            foreach (string key in capture.Sequence)
            {
                if (!expectedSet.Contains(key))
                {
                    summary.failure = "Selected key is outside expected manifest: " + key;
                    break;
                }
            }
            if (capture.Report.OverwrittenStepCount != 0 && string.IsNullOrEmpty(summary.failure))
                summary.failure = "Trace capacity did not retain the complete run.";
            return summary;
        }

        private static bool CoverageFirstMatches(IReadOnlyList<string> sequence, HashSet<string> expected)
        {
            if (sequence.Count < expected.Count) return false;
            HashSet<string> firstCoverage = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < expected.Count; index++)
            {
                if (!expected.Contains(sequence[index]) || !firstCoverage.Add(sequence[index])) return false;
            }
            return firstCoverage.SetEquals(expected);
        }

        private static bool SequencesEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count) return false;
            for (int index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private sealed class RunCapture
        {
            public RunCapture(UnityPointerMonkeyRunReport report, IReadOnlyList<string> sequence)
            {
                Report = report;
                Sequence = sequence;
            }

            public UnityPointerMonkeyRunReport Report { get; }
            public IReadOnlyList<string> Sequence { get; }
            public bool Passed => Report.Passed;
        }
    }
}
