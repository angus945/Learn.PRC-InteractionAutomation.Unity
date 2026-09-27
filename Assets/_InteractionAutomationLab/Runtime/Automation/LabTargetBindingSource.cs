using System;
using System.Collections.Generic;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;

namespace Project.InteractionAutomationLab.Automation
{
    public enum LabBindingScope
    {
        ProductScope = 0,
        FrozenMonkeyScope = 1
    }

    public sealed class LabTargetBindingSource : IUnityInteractionTargetBindingSource
    {
        private readonly Transform subjectRoot;
        private readonly LabBindingScope scope;

        public LabTargetBindingSource(Transform subjectRoot, LabBindingScope scope)
        {
            this.subjectRoot = subjectRoot != null ? subjectRoot : throw new ArgumentNullException(nameof(subjectRoot));
            this.scope = scope;
        }

        public IReadOnlyList<InteractionTargetBinding> CaptureBindings()
        {
            InteractionTargetBinding[] current = subjectRoot.GetComponentsInChildren<InteractionTargetBinding>(false);
            List<InteractionTargetBinding> result = new List<InteractionTargetBinding>(current.Length);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (InteractionTargetBinding binding in current)
            {
                if (binding == null || !binding.gameObject.activeInHierarchy) continue;
                if (scope == LabBindingScope.FrozenMonkeyScope && IsNavigation(binding.TargetId)) continue;
                if (!ids.Add(binding.TargetId)) throw new InvalidOperationException("More than one active binding uses TargetId '" + binding.TargetId + "'.");
                result.Add(binding);
            }
            return result.AsReadOnly();
        }

        private static bool IsNavigation(string targetId)
        {
            return targetId == LabTargetCatalog.NavigationInventory || targetId == LabTargetCatalog.NavigationSettings;
        }
    }
}
