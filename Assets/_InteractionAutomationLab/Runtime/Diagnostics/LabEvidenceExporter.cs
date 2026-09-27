using System;
using System.IO;
using Project.InteractionAutomationLab.Scenarios;
using Project.InteractionAutomationLab.Verification;
using UnityEngine;

namespace Project.InteractionAutomationLab.Diagnostics
{
    [Serializable]
    internal sealed class LabEvidenceManifest
    {
        public string runId;
        public string reportType;
        public string projectRevision;
        public string generatedUtc;
        public string scope;
        public string policyProfile;
    }

    [Serializable]
    internal sealed class LabStepEvidence
    {
        public string runId;
        public string preset;
        public int seed;
        public int sequence;
        public string coverageKey;
        public string verdict;
    }

    public static class LabEvidenceExporter
    {
        public static string Export(LabCampaignReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            string directory = CreateDirectory(report.runId);
            LabEvidenceManifest manifest = new LabEvidenceManifest();
            manifest.runId = report.runId;
            manifest.reportType = "FrozenMonkeyCampaign";
            manifest.projectRevision = report.projectRevision;
            manifest.generatedUtc = DateTime.UtcNow.ToString("O");
            manifest.scope = "FrozenMonkeyScope";
            manifest.policyProfile = "Structural+UnityUiState+LabProject";
            File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonUtility.ToJson(manifest, true));
            File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(report, true));
            using (StreamWriter writer = new StreamWriter(Path.Combine(directory, "steps.jsonl"), false))
            {
                foreach (LabMonkeyRunSummary run in report.runs)
                {
                    for (int index = 0; index < run.sequence.Count; index++)
                    {
                        LabStepEvidence step = new LabStepEvidence();
                        step.runId = report.runId;
                        step.preset = run.preset.ToString();
                        step.seed = run.seed;
                        step.sequence = index + 1;
                        step.coverageKey = run.sequence[index];
                        step.verdict = run.hostAndProductPassed ? "Passed" : run.verdict;
                        writer.WriteLine(JsonUtility.ToJson(step));
                    }
                }
            }
            return directory;
        }

        public static string Export(LabAcceptanceReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            string directory = CreateDirectory(report.runId);
            LabEvidenceManifest manifest = new LabEvidenceManifest();
            manifest.runId = report.runId;
            manifest.reportType = "ScriptedAcceptance";
            manifest.projectRevision = "unknown";
            manifest.generatedUtc = DateTime.UtcNow.ToString("O");
            manifest.scope = "ProductScope";
            manifest.policyProfile = "Structural+UnityUiState+LabProject / ProductNegativeInputRuntime for S10";
            File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonUtility.ToJson(manifest, true));
            File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(directory, "steps.jsonl"), string.Empty);
            return directory;
        }

        private static string CreateDirectory(string runId)
        {
            string root = Path.Combine(Application.persistentDataPath, "InteractionAutomationLab", runId);
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
