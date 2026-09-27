using Module.Verification.Oracle;

public static class AutoLabToggleVerificationProfile
{
    public static OracleSet<AutoLabToggleObservation> Create()
    {
        return new OracleSet<AutoLabToggleObservation>(
            "autolab.toggle.music",
            new ITestOracle<AutoLabToggleObservation>[]
            {
                new AutoLabToggleChangedOracle()
            });
    }
}
