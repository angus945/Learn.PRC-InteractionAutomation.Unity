using Module.Verification.Oracle;

public static class AutoLabButtonClickVerificationProfile
{
    public static OracleSet<AutoLabButtonClickObservation> Create()
    {
        return new OracleSet<AutoLabButtonClickObservation>(
            "autolab.button.confirm",
            new ITestOracle<AutoLabButtonClickObservation>[]
            {
                new AutoLabButtonClickOracle()
            });
    }
}
