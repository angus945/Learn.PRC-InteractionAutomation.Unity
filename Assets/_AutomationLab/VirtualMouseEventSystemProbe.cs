using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class VirtualMouseEventSystemProbe :
    MonoBehaviour
{
    private const string TargetId = "button.confirm";

    private const float EnterTimeoutSeconds = 3f;
    private const float SeparationObservationSeconds = 3f;

    [SerializeField]
    private UnityPointerHoverObserver confirmButtonHoverProbe;

    private UnityPhysicalInputDriver input;

    private IEnumerator Start()
    {
        //
        // ------------------------------------------------
        // Phase 0
        // Composition
        // ------------------------------------------------
        //

        input =
            new UnityPhysicalInputDriver();

        yield return null;

        if (!TryValidateEnvironment())
            yield break;

        int virtualMouseDeviceId =
            input.Mouse.deviceId;

        Debug.Log(
            "VIRTUAL POINTER EXPERIMENT\n" +
            $"VirtualMouseDeviceId={virtualMouseDeviceId}");

        //
        // ------------------------------------------------
        // Phase 1
        // Target Discovery
        // ------------------------------------------------
        //

        var source =
            AutomationLabInteractionComposition.CreateTargetSource();

        Task<IReadOnlyList<InteractionTargetSnapshot>> capture =
            source
                .GetTargetsAsync()
                .AsTask();

        while (!capture.IsCompleted)
            yield return null;

        if (capture.IsFaulted)
        {
            Debug.LogException(
                capture.Exception?.GetBaseException());

            yield break;
        }

        InteractionTargetSnapshot? selected =
            capture
                .GetAwaiter()
                .GetResult()
                .Where(target =>
                    target.Id.Value == TargetId)
                .Cast<InteractionTargetSnapshot?>()
                .FirstOrDefault();

        if (!selected.HasValue)
        {
            Debug.LogError(
                $"Target '{TargetId}' was not discovered.");

            yield break;
        }

        InteractionTargetSnapshot target =
            selected.Value;

        InteractionPoint center =
            target.Bounds.Center;

        Debug.Log(
            "TARGET SELECTED\n" +
            $"Id={target.Id}\n" +
            $"Bounds=(" +
            $"{target.Bounds.X}, " +
            $"{target.Bounds.Y}, " +
            $"{target.Bounds.Width}, " +
            $"{target.Bounds.Height})\n" +
            $"Center=({center.X}, {center.Y})");

        //
        // ------------------------------------------------
        // Phase 2
        // Move Virtual Pointer
        // ------------------------------------------------
        //

        confirmButtonHoverProbe.ResetObservation();

        var submission =
            input.MovePointerAsync(center);

        Debug.Log(
            $"Move submission completed: " +
            $"{submission.IsCompleted}");

        //
        // ------------------------------------------------
        // Phase 3
        // Wait specifically for Virtual Mouse PointerEnter
        // ------------------------------------------------
        //

        float startedAt =
            Time.realtimeSinceStartup;

        while (!confirmButtonHoverProbe.IsHoveredByDevice(
                   virtualMouseDeviceId))
        {
            if (Time.realtimeSinceStartup - startedAt >=
                EnterTimeoutSeconds)
            {
                Debug.LogError(
                    "VIRTUAL POINTER ENTER TIMEOUT\n" +
                    $"VirtualMouseDeviceId={virtualMouseDeviceId}\n" +
                    $"MouseAdded={input.Mouse.added}\n" +
                    $"MouseEnabled={input.Mouse.enabled}\n" +
                    $"MousePosition=" +
                    $"{input.Mouse.position.ReadValue()}");

                yield break;
            }

            yield return null;
        }

        Debug.Log(
            "VIRTUAL POINTER ENTER VERIFIED\n" +
            $"Target={TargetId}\n" +
            $"VirtualMouseDeviceId={virtualMouseDeviceId}\n" +
            $"EnterCount=" +
            $"{confirmButtonHoverProbe.GetEnterCount(virtualMouseDeviceId)}");

        //
        // ------------------------------------------------
        // Phase 4
        // Pointer Separation Observation
        // ------------------------------------------------
        //
        // During the next few seconds:
        //
        // MOVE YOUR PHYSICAL MOUSE.
        //
        // The virtual pointer should remain hovered on ConfirmButton.
        //

        Debug.Log(
            "POINTER SEPARATION TEST STARTED\n" +
            $"Move the physical mouse for the next " +
            $"{SeparationObservationSeconds:0.#} seconds.\n" +
            "The virtual pointer must remain on ConfirmButton.");

        float observationStartedAt =
            Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup -
               observationStartedAt <
               SeparationObservationSeconds)
        {
            if (!confirmButtonHoverProbe.IsHoveredByDevice(
                    virtualMouseDeviceId))
            {
                Debug.LogError(
                    "POINTER SEPARATION FAILED\n" +
                    "The virtual pointer exited ConfirmButton while " +
                    "another pointer was being used.\n" +
                    $"VirtualMouseDeviceId={virtualMouseDeviceId}");

                yield break;
            }

            yield return null;
        }

        //
        // ------------------------------------------------
        // PASS
        // ------------------------------------------------
        //

        Debug.Log(
            "EXP-IA-001C POINTER SEPARATION PASS\n" +
            $"Target={TargetId}\n" +
            $"VirtualMouseDeviceId={virtualMouseDeviceId}\n" +
            "Physical mouse activity did not displace " +
            "the virtual EventSystem pointer.");
    }

    private bool TryValidateEnvironment()
    {
        if (confirmButtonHoverProbe == null)
        {
            Debug.LogError(
                "ConfirmButton UnityPointerHoverObserver is not assigned.");

            return false;
        }

        if (!input.Mouse.added)
        {
            Debug.LogError(
                "Virtual mouse is not registered.");

            return false;
        }

        if (!input.Mouse.enabled)
        {
            Debug.LogError(
                "Virtual mouse is disabled.");

            return false;
        }

        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogError(
                "No active EventSystem was found.");

            return false;
        }

        InputSystemUIInputModule module =
            eventSystem.GetComponent<
                InputSystemUIInputModule>();

        if (module == null)
        {
            Debug.LogError(
                "EventSystem does not use " +
                "InputSystemUIInputModule.");

            return false;
        }

        if (module.pointerBehavior !=
            UIPointerBehavior.AllPointersAsIs)
        {
            Debug.LogError(
                "InputSystemUIInputModule.PointerBehavior must be " +
                "AllPointersAsIs for this experiment.\n" +
                $"Current={module.pointerBehavior}");

            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }
}