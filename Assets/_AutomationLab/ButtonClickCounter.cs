using UnityEngine;

public sealed class ButtonClickCounter : MonoBehaviour
{
    public int Count { get; private set; }

    public void HandleClick()
    {
        Count++;

        Debug.Log(
            $"BUTTON NORMAL CALLBACK\n" +
            $"ClickCount={Count}");
    }

    public void ResetCount()
    {
        Count = 0;
    }
}