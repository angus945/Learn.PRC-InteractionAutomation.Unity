using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;

public sealed class MusicToggleExperimentProbe :
    MonoBehaviour
{
    private const string TargetId =
        "toggle.music";

    private const float TimeoutSeconds =
        3f;

    [SerializeField]
    private ToggleStateProbe stateProbe;

    private UnityPhysicalInputDriver input;

    private IEnumerator Start()
    {
        input =
            new UnityPhysicalInputDriver();

        // Normal runtime staging.
        yield return null;

        if (stateProbe == null)
        {
            Debug.LogError(
                "ToggleStateProbe is not assigned.");

            yield break;
        }

        //
        // Target discovery
        //

        var source =
            new UnityInteractionTargetSource();

        Task<IReadOnlyList<InteractionTargetSnapshot>>
            capture =
                source
                    .GetTargetsAsync()
                    .AsTask();

        while (!capture.IsCompleted)
            yield return null;

        if (capture.IsFaulted)
        {
            Debug.LogException(
                capture.Exception?
                    .GetBaseException());

            yield break;
        }

        InteractionTargetSnapshot? selected =
            capture
                .GetAwaiter()
                .GetResult()
                .Where(
                    target =>
                        target.Id.Value ==
                        TargetId)
                .Cast<InteractionTargetSnapshot?>()
                .FirstOrDefault();

        if (!selected.HasValue)
        {
            Debug.LogError(
                $"Target '{TargetId}' " +
                "was not discovered.");

            yield break;
        }

        InteractionTargetSnapshot target =
            selected.Value;

        //
        // Verify metadata only.
        //

        if (target.Role !=
            InteractionTargetRole.Toggle)
        {
            Debug.LogError(
                $"Expected Toggle role, " +
                $"actual={target.Role}");

            yield break;
        }

        if ((target.Capabilities &
             PhysicalInteractionCapabilities.PointerClick) ==
            0)
        {
            Debug.LogError(
                "Target does not expose " +
                "PointerClick capability.");

            yield break;
        }

        if (target.Bounds.Space !=
            InteractionCoordinateSpace.ApplicationSpace)
        {
            Debug.LogError(
                "Target bounds must use " +
                "ApplicationSpace.");

            yield break;
        }

        InteractionPoint point =
            target.Bounds.Center;

        Debug.Log(
            "MUSIC TOGGLE TARGET\n" +
            $"Id={target.Id}\n" +
            $"Role={target.Role}\n" +
            $"Capabilities={target.Capabilities}\n" +
            $"Center=({point.X}, {point.Y})");

        stateProbe.ResetObservation(
            initialValue: false);

        //
        // --------------------------------
        // Interaction 1
        // False -> True
        // --------------------------------
        //

        Debug.Log(
            "FIRST POINTER CLICK");

        Task firstSubmission =
            SubmitPointerClickAsync(point)
                .AsTask();

        while (!firstSubmission.IsCompleted)
            yield return null;

        if (firstSubmission.IsFaulted)
        {
            Debug.LogException(
                firstSubmission.Exception?
                    .GetBaseException());

            yield break;
        }

        yield return WaitForChangeCount(1);

        if (stateProbe.ChangeCount < 1)
            yield break;

        if (!stateProbe.LastValue)
        {
            Debug.LogError(
                "FIRST CLICK FAILED\n" +
                "Expected False -> True.");

            yield break;
        }

        Debug.Log(
            "FIRST CLICK PASS\n" +
            "False -> True");

        //
        // This separates two distinct user-level
        // interactions.
        //
        // It is NOT a processing barrier inside
        // Move / Down / Up.
        //
        yield return null;

        //
        // --------------------------------
        // Interaction 2
        // True -> False
        // --------------------------------
        //

        Debug.Log(
            "SECOND POINTER CLICK");

        Task secondSubmission =
            SubmitPointerClickAsync(point)
                .AsTask();

        while (!secondSubmission.IsCompleted)
            yield return null;

        if (secondSubmission.IsFaulted)
        {
            Debug.LogException(
                secondSubmission.Exception?
                    .GetBaseException());

            yield break;
        }

        yield return WaitForChangeCount(2);

        if (stateProbe.ChangeCount < 2)
            yield break;

        if (stateProbe.LastValue)
        {
            Debug.LogError(
                "SECOND CLICK FAILED\n" +
                "Expected True -> False.");

            yield break;
        }

        Debug.Log(
            "EXP-IA-001E PASS\n" +
            $"Target={target.Id}\n" +
            "False -> True -> False\n" +
            "Both interactions used the same " +
            "PointerClick physical-input route.");
    }

    private async ValueTask
        SubmitPointerClickAsync(
            InteractionPoint point)
    {
        //
        // IMPORTANT:
        //
        // No Toggle API.
        // No EventSystem API.
        // No processing barrier.
        //

        await input.MovePointerAsync(
            point);

        await input.PointerDownAsync(
            PointerButton.Left);

        await input.PointerUpAsync(
            PointerButton.Left);
    }

    private IEnumerator WaitForChangeCount(
        int expectedCount)
    {
        float startedAt =
            Time.realtimeSinceStartup;

        while (stateProbe.ChangeCount <
               expectedCount)
        {
            if (Time.realtimeSinceStartup -
                startedAt >=
                TimeoutSeconds)
            {
                Debug.LogError(
                    "TOGGLE CALLBACK TIMEOUT\n" +
                    $"ExpectedChangeCount=" +
                    $"{expectedCount}\n" +
                    $"ActualChangeCount=" +
                    $"{stateProbe.ChangeCount}\n" +
                    $"LastValue=" +
                    $"{stateProbe.LastValue}");

                yield break;
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }
}