using System;
using System.Collections.Generic;

namespace Project.InteractionAutomationLab.Scenarios
{
    public enum LabScenarioStatus
    {
        Passed = 0,
        Failed = 1,
        Cancelled = 2,
        NotRun = 3
    }

    [Serializable]
    public sealed class LabScenarioResult
    {
        public string id;
        public LabScenarioStatus status;
        public string detail;
        public int physicalSubmissionDelta;
    }

    [Serializable]
    public sealed class LabAcceptanceReport
    {
        public string runId;
        public string startedUtc;
        public string completedUtc;
        public List<LabScenarioResult> scenarios = new List<LabScenarioResult>();

        public bool Passed
        {
            get
            {
                if (scenarios.Count != 10) return false;
                foreach (LabScenarioResult scenario in scenarios)
                {
                    if (scenario.status != LabScenarioStatus.Passed) return false;
                }
                return true;
            }
        }
    }
}
