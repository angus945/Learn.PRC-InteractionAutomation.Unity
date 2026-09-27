using System;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.Runner;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;
using UnityEngine.UI;

public static class AutomationLabInteractionComposition
{
    public static InteractionRunner CreateRunner(
        Camera interactionCamera,
        IPhysicalInputDriver physicalInput,
        IHostInputProcessingBoundary inputProcessing)
    {
        if (physicalInput == null)
            throw new ArgumentNullException(nameof(physicalInput));

        if (inputProcessing == null)
            throw new ArgumentNullException(nameof(inputProcessing));

        UnityInteractionTargetSource targetSource =
            CreateTargetSource(
                interactionCamera);

        var availability =
            new SnapshotInteractionAvailabilityEvaluator();

        var delay =
            new SystemInteractionDelay();

        InteractionRegistryBuilder registry =
            PointerInteractionRegistryProfile
                .CreateBuilder();

        registry
            .Register<PointerClickRequest>(
                new PointerClickInteraction(
                    targetSource,
                    availability,
                    physicalInput))
            .Register<PointerDoubleClickRequest>(
                new PointerDoubleClickInteraction(
                    targetSource,
                    availability,
                    physicalInput,
                    inputProcessing))
            .Register<PointerDragRequest>(
                new PointerDragInteraction(
                    targetSource,
                    availability,
                    physicalInput,
                    inputProcessing))
            .Register<PointerScrollRequest>(
                new PointerScrollInteraction(
                    targetSource,
                    availability,
                    physicalInput))
            .Register<PointerHoldRequest>(
                new PointerHoldInteraction(
                    targetSource,
                    availability,
                    physicalInput,
                    inputProcessing,
                    delay))
            .Register<PointerHoverRequest>(
                new PointerHoverInteraction(
                    targetSource,
                    availability,
                    physicalInput));

        return new InteractionRunner(
            registry.Build());
    }

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
