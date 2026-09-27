public readonly struct AutoLabButtonClickObservation
{
    public AutoLabButtonClickObservation(int callbackCount)
    {
        CallbackCount = callbackCount;
    }

    public int CallbackCount { get; }
}
