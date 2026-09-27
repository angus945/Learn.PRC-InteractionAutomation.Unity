using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using Project.InteractionAutomationLab.Scenarios;
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
    }
}
