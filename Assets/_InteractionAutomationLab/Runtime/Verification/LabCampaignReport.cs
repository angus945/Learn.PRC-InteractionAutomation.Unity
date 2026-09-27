using System;
using System.Collections.Generic;

namespace Project.InteractionAutomationLab.Verification
{
    [Serializable]
    public sealed class LabMonkeyRunSummary
    {
        public LabMonkeyPreset preset;
        public int seed;
        public int requestedIterations;
        public int completedIterations;
        public int expectedEligibleCount;
        public int actualEligibleCount;
        public int coveredCount;
        public bool coverageFirst;
        public bool deterministicReplay;
        public bool hostAndProductPassed;
        public string verdict;
        public string failure;
        public List<string> sequence = new List<string>();

        public bool Passed => completedIterations == requestedIterations && expectedEligibleCount == actualEligibleCount &&
            coveredCount == expectedEligibleCount && coverageFirst && deterministicReplay && hostAndProductPassed && string.IsNullOrEmpty(failure);
    }

    [Serializable]
    public sealed class LabCampaignReport
    {
        public string runId;
        public string startedUtc;
        public string completedUtc;
        public string projectRevision = "unknown";
        public int traceCapacity = 128;
        public int iterationsPerRun = 64;
        public List<LabMonkeyRunSummary> runs = new List<LabMonkeyRunSummary>();

        public bool Passed
        {
            get
            {
                if (runs.Count != 12) return false;
                foreach (LabMonkeyRunSummary run in runs)
                {
                    if (!run.Passed) return false;
                }
                return true;
            }
        }
    }
}
