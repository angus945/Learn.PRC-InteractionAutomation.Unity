using System;
using Framework.InteractionAutomation.Runner;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing;
using UnityEngine;
using UnityEngine.UI;

public static class AutomationLabInteractionComposition
{
    public static UnityInteractionAutomationRuntime CreateRuntime(
        Camera interactionCamera,
        IPhysicalInputDriver physicalInput,
        IHostInputProcessingBoundary inputProcessing)
    {
        if (physicalInput == null)
            throw new ArgumentNullException(nameof(physicalInput));
        if (inputProcessing == null)
            throw new ArgumentNullException(nameof(inputProcessing));

        // The Project chooses adapters; the Framework owns pointer composition.
        return UnityInteractionAutomationBuilder
            .Create()
            .UseTargetSource(CreateTargetSource(interactionCamera))
            .UseAvailability(new SnapshotInteractionAvailabilityEvaluator())
            .UsePhysicalInput(physicalInput)
            .UseInputProcessingBoundary(inputProcessing)
            .Build();
    }

    public static InteractionRunner CreateRunner(
        Camera interactionCamera,
        IPhysicalInputDriver physicalInput,
        IHostInputProcessingBoundary inputProcessing)
    {
        return CreateRuntime(
            interactionCamera,
            physicalInput,
            inputProcessing).Runner;
    }

    public static UnityInteractionTargetSource CreateTargetSource(
        Camera interactionCamera = null)
    {
        // AutoLab's existing single-camera fixture is retained for migration
        // regression. Dynamic view selection is a separate integration slice.
        Camera camera = interactionCamera != null ? interactionCamera : Camera.main;
        if (camera == null)
            throw new InvalidOperationException("Automation Lab requires an interaction camera.");

        var coreKinds = new InteractionTargetKindRegistry();
        coreKinds.Register<ButtonTargetKind>();
        coreKinds.Register<ToggleTargetKind>();
        coreKinds.Register<ChestTargetKind>();

        var unityKinds = new UnityInteractionTargetKindRegistry(coreKinds);
        unityKinds
            .Register<Button, ButtonTargetKind>()
            .Register<Toggle, ToggleTargetKind>()
            .Register<Renderer, ChestTargetKind>();

        return new UnityInteractionTargetSource(
            unityKinds,
            new IUnityInteractionTargetGeometryProvider[]
            {
                new UnityRectTransformTargetGeometryProvider(),
                new UnityRendererTargetGeometryProvider(camera)
            });
    }
}
