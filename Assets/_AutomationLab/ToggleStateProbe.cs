using UnityEngine;

public sealed class ToggleStateProbe : MonoBehaviour
{
    public bool LastValue { get; private set; }

    public int ChangeCount { get; private set; }

    public void ResetObservation(
        bool initialValue = false)
    {
        LastValue = initialValue;
        ChangeCount = 0;
    }

    public void HandleValueChanged(
        bool value)
    {
        LastValue = value;
        ChangeCount++;

        Debug.Log(
            "TOGGLE NORMAL CALLBACK\n" +
            $"Value={value}\n" +
            $"ChangeCount={ChangeCount}");
    }
}