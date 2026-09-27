using Module.InteractionAutomation.Observation.Unity3D;
using Module.Verification.Oracle;

public static class AutoLabDropVerificationProfile
{
    public static OracleSet<UnityPointerDropObservation> Create()
    {
        return new OracleSet<UnityPointerDropObservation>(
            "autolab.drag.drop-on-toggle",
            new ITestOracle<UnityPointerDropObservation>[]
            {
                new AutoLabDropObservedOracle()
            });
    }
}
