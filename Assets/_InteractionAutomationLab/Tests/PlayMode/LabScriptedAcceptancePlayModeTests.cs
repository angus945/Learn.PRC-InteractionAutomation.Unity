using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Project.InteractionAutomationLab.Scenarios;
using Project.InteractionAutomationLab.Verification;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.InteractionAutomationLab.Tests
{
    public sealed class LabScriptedAcceptancePlayModeTests
    {
        private const string ScenePath = "Assets/_InteractionAutomationLab/Scenes/InteractionAutomationStateLab.unity";

        [UnityTest]
        public IEnumerator ScriptedAcceptance_ExercisesS01ThroughS08AndS10()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            LabSessionController session = Object.FindFirstObjectByType<LabSessionController>();
            Assert.That(session, Is.Not.Null);
            Task<LabAcceptanceReport> task = session.RunScriptedAcceptanceAsync();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
            LabAcceptanceReport report = task.Result;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.scenarios.Count, Is.EqualTo(9));
            foreach (LabScenarioResult scenario in report.scenarios)
            {
                Assert.That(scenario.status, Is.EqualTo(LabScenarioStatus.Passed), scenario.id + ": " + scenario.detail);
            }
            Assert.That(report.Passed, Is.True);
        }

        [UnityTest]
        public IEnumerator FrozenMonkeyCampaign_ValidatesAllPresetsSeedsAndDeterminism()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            LabSessionController session = Object.FindFirstObjectByType<LabSessionController>();
            Assert.That(session, Is.Not.Null);
            Task<LabCampaignReport> task = session.RunFrozenMonkeyCampaignAsync();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
            LabCampaignReport report = task.Result;
            Assert.That(report.runs.Count, Is.EqualTo(12));
            foreach (LabMonkeyRunSummary run in report.runs)
            {
                Assert.That(run.completedIterations, Is.EqualTo(64), run.preset + " seed=" + run.seed + " " + run.failure);
                Assert.That(run.expectedEligibleCount, Is.EqualTo(run.actualEligibleCount));
                Assert.That(run.coverageFirst, Is.True);
                Assert.That(run.deterministicReplay, Is.True);
                Assert.That(run.hostAndProductPassed, Is.True, run.failure);
                Assert.That(run.Passed, Is.True, run.failure);
            }
            Assert.That(report.Passed, Is.True);
            session.ExportLastResult();
            Assert.That(File.Exists(Path.Combine(session.LastExportPath, "manifest.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(session.LastExportPath, "steps.jsonl")), Is.True);
            Assert.That(File.Exists(Path.Combine(session.LastExportPath, "summary.json")), Is.True);
            string[] evidenceLines = File.ReadAllLines(Path.Combine(session.LastExportPath, "steps.jsonl"));
            Assert.That(evidenceLines.Length, Is.EqualTo(12 * 64));
        }
    }
}
