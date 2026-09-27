using Module.Verification.Oracle;

public static class DragVerificationProfile
{
    public static OracleSet<DragObservation> Create()
    {
        return new OracleSet<DragObservation>(
            "pointer.drag",
            new ITestOracle<DragObservation>[]
            {
                new DragLifecycleOracle(),
                new DropObservedOracle()
            });
    }
}
