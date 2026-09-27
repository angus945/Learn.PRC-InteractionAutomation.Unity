public readonly struct AutoLabToggleObservation
{
    public AutoLabToggleObservation(
        int changeCount,
        bool value)
    {
        ChangeCount = changeCount;
        Value = value;
    }

    public int ChangeCount { get; }
    public bool Value { get; }
}
