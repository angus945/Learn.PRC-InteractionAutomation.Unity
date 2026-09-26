using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;

public sealed class WorldTargetDiscoveryProbe :
    MonoBehaviour
{
    [SerializeField]
    private Camera interactionCamera;

    private IEnumerator Start()
    {
        if (interactionCamera == null)
        {
            Debug.LogError(
                "Interaction Camera is not assigned.");

            yield break;
        }

        var source =
            new UnityInteractionTargetSource(
                new IUnityInteractionTargetGeometryProvider[]
                {
                    new UnityRectTransformTargetGeometryProvider(),

                    new UnityRendererTargetGeometryProvider(
                        interactionCamera)
                });

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

        IReadOnlyList<InteractionTargetSnapshot> targets =
            capture.GetAwaiter()
                .GetResult();

        Debug.Log(
            $"Targets: {targets.Count}");

        foreach (InteractionTargetSnapshot target in targets)
        {
            Debug.Log(
                $"TARGET\n" +
                $"Id={target.Id}\n" +
                $"Role={target.Role}\n" +
                $"Capabilities={target.Capabilities}\n" +
                $"Space={target.Bounds.Space}\n" +
                $"Bounds=(" +
                $"{target.Bounds.X:0.##}, " +
                $"{target.Bounds.Y:0.##}, " +
                $"{target.Bounds.Width:0.##}, " +
                $"{target.Bounds.Height:0.##})\n" +
                $"Center=(" +
                $"{target.Bounds.Center.X:0.##}, " +
                $"{target.Bounds.Center.Y:0.##})");
        }

        InteractionTargetSnapshot? chest =
            targets
                .Where(
                    target =>
                        target.Id.Value ==
                        "chest.001")
                .Cast<InteractionTargetSnapshot?>()
                .FirstOrDefault();

        if (!chest.HasValue)
        {
            Debug.LogError(
                "chest.001 was not discovered.");

            yield break;
        }

        if (chest.Value.Role !=
            InteractionTargetRole.WorldObject)
        {
            Debug.LogError(
                $"Unexpected Role: " +
                $"{chest.Value.Role}");

            yield break;
        }

        if (chest.Value.Bounds.IsEmpty)
        {
            Debug.LogError(
                "chest.001 produced empty bounds.");

            yield break;
        }

        Debug.Log(
            "EXP-IA-002A RENDERER GEOMETRY PASS\n" +
            $"Bounds={chest.Value.Bounds.X:0.##}," +
            $"{chest.Value.Bounds.Y:0.##}," +
            $"{chest.Value.Bounds.Width:0.##}," +
            $"{chest.Value.Bounds.Height:0.##}");
    }
}