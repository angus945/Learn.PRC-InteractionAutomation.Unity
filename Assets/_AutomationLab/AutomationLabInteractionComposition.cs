using System;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;
using UnityEngine.UI;

public static class AutomationLabInteractionComposition
{
    public static UnityInteractionTargetSource CreateTargetSource(
        Camera interactionCamera = null)
    {
        Camera camera =
            interactionCamera != null
                ? interactionCamera
                : Camera.main;

        if (camera == null)
        {
            throw new InvalidOperationException(
                "Automation Lab requires an interaction camera.");
        }

        var coreKinds =
            new InteractionTargetKindRegistry();

        coreKinds.Register<ButtonTargetKind>();
        coreKinds.Register<ToggleTargetKind>();
        coreKinds.Register<ChestTargetKind>();

        var unityKinds =
            new UnityInteractionTargetKindRegistry(
                coreKinds);

        unityKinds
            .Register<Button, ButtonTargetKind>()
            .Register<Toggle, ToggleTargetKind>()
            .Register<Renderer, ChestTargetKind>();

        return new UnityInteractionTargetSource(
            unityKinds,
            new IUnityInteractionTargetGeometryProvider[]
            {
                new UnityRectTransformTargetGeometryProvider(),
                new UnityRendererTargetGeometryProvider(
                    camera)
            });
    }
}
