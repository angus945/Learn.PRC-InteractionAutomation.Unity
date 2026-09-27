using System;
using System.Collections.Generic;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Availability;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing;
using Project.InteractionAutomationLab.Presentation;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Automation
{
    public static class LabComposition
    {
        public static UnityInteractionAutomationRuntime CreateProductionRuntime(LabProductController product,
            IPhysicalInputDriver input, IHostInputProcessingBoundary boundary, LabTargetBindingSource bindings,
            IInteractionAvailabilityEvaluator additionalPolicy = null)
        {
            UnityInteractionAutomationBuilder builder = UnityInteractionAutomationBuilder.Create()
                .UseLoadedSceneTargets(CreateKinds(), CreateGeometry(), bindings)
                .UseUnityUiStateAvailability()
                .AddAvailabilityPolicy(new LabProjectAvailabilityPolicy(product.State))
                .UsePhysicalInput(input)
                .UseInputProcessingBoundary(boundary);
            if (additionalPolicy != null) builder.AddAvailabilityPolicy(additionalPolicy);
            return builder.Build();
        }

        public static UnityInteractionAutomationRuntime CreateProductNegativeInputRuntime(IPhysicalInputDriver input,
            IHostInputProcessingBoundary boundary, LabTargetBindingSource bindings)
        {
            return UnityInteractionAutomationBuilder.Create()
                .UseLoadedSceneTargets(CreateKinds(), CreateGeometry(), bindings)
                .UsePhysicalInput(input)
                .UseInputProcessingBoundary(boundary)
                .Build();
        }

        private static UnityInteractionTargetKindRegistry CreateKinds()
        {
            InteractionTargetKindRegistry core = new InteractionTargetKindRegistry();
            core.Register<LabButtonTargetKind>();
            core.Register<LabToggleTargetKind>();
            core.Register<LabScrollTargetKind>();
            core.Register<LabItemTargetKind>();
            core.Register<LabEquipmentTargetKind>();
            core.Register<LabInspectTargetKind>();
            core.Register<LabChargeTargetKind>();
            core.Register<LabNonSelectableTargetKind>();
            UnityInteractionTargetKindRegistry unity = new UnityInteractionTargetKindRegistry(core);
            unity.Register<Button, LabButtonTargetKind>();
            unity.Register<Toggle, LabToggleTargetKind>();
            unity.Register<ScrollRect, LabScrollTargetKind>();
            unity.Register<LabItemCardControl, LabItemTargetKind>();
            unity.Register<LabEquipmentSlotControl, LabEquipmentTargetKind>();
            unity.Register<LabInspectControl, LabInspectTargetKind>();
            unity.Register<LabChargeControl, LabChargeTargetKind>();
            unity.Register<LabNonSelectableControl, LabNonSelectableTargetKind>();
            return unity;
        }

        private static IReadOnlyList<IUnityInteractionTargetGeometryProvider> CreateGeometry()
        {
            return new IUnityInteractionTargetGeometryProvider[] { new UnityRectTransformTargetGeometryProvider() };
        }
    }
}
