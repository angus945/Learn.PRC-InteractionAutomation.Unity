using System;
using System.Collections.Generic;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Project.InteractionAutomationLab.Automation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Editor
{
    public static class LabSceneValidator
    {
        private const string ScenePath = "Assets/_InteractionAutomationLab/Scenes/InteractionAutomationStateLab.unity";

        [MenuItem("Tools/Interaction Automation Lab/Validate State Lab")]
        public static void ValidateSceneAsset()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("CSL scene validation passed: 20 unique targets, observers and control mappings are complete.");
        }

        public static void ValidateScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("State Lab scene is not loaded.");
            InteractionTargetBinding[] bindings = UnityEngine.Object.FindObjectsByType<InteractionTargetBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Dictionary<string, InteractionTargetBinding> found = new Dictionary<string, InteractionTargetBinding>(StringComparer.Ordinal);
            foreach (InteractionTargetBinding binding in bindings)
            {
                if (!found.TryAdd(binding.TargetId, binding)) throw new InvalidOperationException("Duplicate TargetId: " + binding.TargetId);
                ValidateBinding(binding);
            }
            if (found.Count != LabTargetCatalog.All.Count) throw new InvalidOperationException("Expected 20 targets but found " + found.Count + ".");
            foreach (LabTargetDescriptor descriptor in LabTargetCatalog.All)
            {
                if (!found.TryGetValue(descriptor.Id, out InteractionTargetBinding binding)) throw new InvalidOperationException("Missing target: " + descriptor.Id);
                if (binding.Capabilities != descriptor.Capabilities) throw new InvalidOperationException("Capability mismatch: " + descriptor.Id);
            }
        }

        private static void ValidateBinding(InteractionTargetBinding binding)
        {
            PhysicalInteractionCapabilities capabilities = binding.Capabilities;
            bool click = Has(capabilities, PhysicalInteractionCapabilities.PointerClick) || Has(capabilities, PhysicalInteractionCapabilities.PointerDoubleClick);
            if (click) Require<UnityPointerClickObserver>(binding);
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerHover)) Require<UnityPointerHoverObserver>(binding);
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerDrag)) Require<UnityPointerDragObserver>(binding);
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerHold)) Require<UnityPointerPressObserver>(binding);
            if (Has(capabilities, PhysicalInteractionCapabilities.Scroll)) Require<UnityPointerScrollObserver>(binding);
            Graphic graphic = binding.GetComponent<Graphic>();
            if (graphic == null) throw new InvalidOperationException("Target has no same-object Graphic: " + binding.TargetId);
            if (!graphic.raycastTarget) throw new InvalidOperationException("Target Graphic does not receive raycasts: " + binding.TargetId);
        }

        private static bool Has(PhysicalInteractionCapabilities value, PhysicalInteractionCapabilities flag)
        {
            return (value & flag) != 0;
        }

        private static void Require<T>(InteractionTargetBinding binding) where T : Component
        {
            if (binding.GetComponent<T>() == null) throw new InvalidOperationException("Target " + binding.TargetId + " requires " + typeof(T).Name + ".");
        }
    }
}
