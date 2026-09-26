using System.Collections;
using System.Threading.Tasks;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using UnityEngine;

public sealed class InteractionRunnerProbe :
    MonoBehaviour
{
    [SerializeField]
    private Camera interactionCamera;

    [SerializeField]
    private ButtonClickCounter buttonClickCounter;

    [SerializeField]
    private ToggleStateProbe toggleStateProbe;

    private UnityPhysicalInputDriver physicalInput;

    private InteractionRunner runner;

    private IEnumerator Start()
    {
        if (buttonClickCounter == null)
        {
            Debug.LogError(
                "ButtonClickCounter is not assigned.");

            yield break;
        }

        if (toggleStateProbe == null)
        {
            Debug.LogError(
                "ToggleStateProbe is not assigned.");

            yield break;
        }

        physicalInput =
            new UnityPhysicalInputDriver();

        var targetSource =
            AutomationLabInteractionComposition
                .CreateTargetSource(
                    interactionCamera);

        runner =
            new InteractionRunner(
                targetSource,
                physicalInput);

        // Allow normal Unity runtime composition to settle.
        yield return null;

        buttonClickCounter.ResetCount();

        toggleStateProbe.ResetObservation(
            initialValue: false);

        //
        // ============================================
        // Button
        // ============================================
        //

        Debug.Log(
            "RUNNER BUTTON START");

        Task buttonClick =
            runner
                .PointerClickAsync(
                    new InteractionTargetId(
                        "button.confirm"))
                .AsTask();

        while (!buttonClick.IsCompleted)
            yield return null;

        if (buttonClick.IsFaulted)
        {
            Debug.LogException(
                buttonClick.Exception?
                    .GetBaseException());

            yield break;
        }

        //
        // Runner completion only means input submission.
        // Give Unity product processing a normal frame.
        //
        yield return null;

        if (buttonClickCounter.Count != 1)
        {
            Debug.LogError(
                "RUNNER BUTTON FAILED\n" +
                $"Expected ClickCount=1\n" +
                $"Actual={buttonClickCounter.Count}");

            yield break;
        }

        Debug.Log(
            "RUNNER BUTTON PASS");

        //
        // ============================================
        // Toggle
        // ============================================
        //

        Debug.Log(
            "RUNNER TOGGLE START");

        Task toggleClick =
            runner
                .PointerClickAsync(
                    new InteractionTargetId(
                        "toggle.music"))
                .AsTask();

        while (!toggleClick.IsCompleted)
            yield return null;

        if (toggleClick.IsFaulted)
        {
            Debug.LogException(
                toggleClick.Exception?
                    .GetBaseException());

            yield break;
        }

        yield return null;

        if (toggleStateProbe.ChangeCount != 1 ||
            !toggleStateProbe.LastValue)
        {
            Debug.LogError(
                "RUNNER TOGGLE FAILED\n" +
                $"ChangeCount={toggleStateProbe.ChangeCount}\n" +
                $"LastValue={toggleStateProbe.LastValue}");

            yield break;
        }

        Debug.Log(
            "PHASE 8 INTERACTION RUNNER PASS\n" +
            "button.confirm -> PointerClick PASS\n" +
            "toggle.music -> PointerClick PASS\n" +
            "Both targets used the same InteractionRunner.");
    }

    private void OnDestroy()
    {
        physicalInput?.Dispose();
    }
}