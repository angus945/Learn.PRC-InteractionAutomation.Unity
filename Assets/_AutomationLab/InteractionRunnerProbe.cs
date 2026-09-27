using System.Collections;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Framework.InteractionAutomation.Runner;
using Module.InteractionAutomation.Timing.Unity3D;
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

    [SerializeField]
    private UnityPointerDragObserver dragObserver;

    [SerializeField]
    private UnityPointerDropObserver dropObserver;

    [SerializeField]
    private UnityPointerHoverObserver pointerHoverObserver;

    [SerializeField]
    private UnityPointerClickObserver pointerClickObserver;

    [SerializeField]
    private UnityPointerPressObserver pointerPressObserver;

    [SerializeField]
    private UnityPointerScrollObserver pointerScrollObserver;

    [SerializeField]
    private UnityFrameInputProcessingBoundary inputProcessingBoundary;

    private UnityPhysicalInputDriver physicalInput;

    private IEnumerator Start()
    {
        if (!ValidateComposition())
            yield break;

        physicalInput =
            new UnityPhysicalInputDriver();

        InteractionRunner runner =
            AutomationLabInteractionComposition
                .CreateRunner(
                    interactionCamera,
                    physicalInput,
                    inputProcessingBoundary);

        yield return null;

        var buttonClick =
            new AutoLabButtonClickScenario(
                runner,
                buttonClickCounter);

        yield return buttonClick.Run();

        if (!buttonClick.Passed)
            yield break;

        var toggleClick =
            new AutoLabToggleClickScenario(
                runner,
                toggleStateProbe);

        yield return toggleClick.Run();

        if (!toggleClick.Passed)
            yield break;

        var drag =
            new AutoLabDragScenario(
                runner,
                dragObserver,
                dropObserver);

        yield return drag.Run();

        if (!drag.Passed)
            yield break;

        var scroll =
            new AutoLabScrollScenario(
                runner,
                pointerScrollObserver);

        yield return scroll.Run();

        if (!scroll.Passed)
            yield break;

        var hover =
            new AutoLabHoverScenario(
                runner,
                physicalInput,
                pointerHoverObserver);

        yield return hover.Run();

        if (!hover.Passed)
            yield break;

        var doubleClick =
            new AutoLabDoubleClickScenario(
                runner,
                pointerClickObserver);

        yield return doubleClick.Run();

        if (!doubleClick.Passed)
            yield break;

        var hold =
            new AutoLabHoldScenario(
                runner,
                pointerPressObserver);

        yield return hold.Run();

        if (!hold.Passed)
            yield break;

        Debug.Log(
            "PHASE 11 OBSERVATION / VERIFICATION PASS\n" +
            "Execution / observation / host verification / " +
            "AutoLab product verification are separated.");
    }

    private bool ValidateComposition()
    {
        if (interactionCamera == null ||
            buttonClickCounter == null ||
            toggleStateProbe == null ||
            dragObserver == null ||
            dropObserver == null ||
            pointerHoverObserver == null ||
            pointerClickObserver == null ||
            pointerPressObserver == null ||
            pointerScrollObserver == null ||
            inputProcessingBoundary == null)
        {
            Debug.LogError(
                "InteractionRunnerProbe composition is incomplete.");
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        physicalInput?.Dispose();
    }
}
