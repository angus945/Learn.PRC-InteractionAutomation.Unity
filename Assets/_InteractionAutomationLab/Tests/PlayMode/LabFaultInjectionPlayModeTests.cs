using System;
using System.Collections;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Framework.InteractionAutomation.Runner.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.PhysicalInput;
using Module.InteractionAutomation.PhysicalInput.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing.Unity3D;
using NUnit.Framework;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.InteractionAutomationLab.Tests
{
    public sealed class LabFaultInjectionPlayModeTests
    {
        private const string ScenePath = "Assets/_InteractionAutomationLab/Scenes/InteractionAutomationStateLab.unity";

        [UnityTest]
        public IEnumerator MissingObserverAndDuplicateId_AreDetected()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            LabProductController product = UnityEngine.Object.FindFirstObjectByType<LabProductController>();
            UnityFrameInputProcessingBoundary boundary = UnityEngine.Object.FindFirstObjectByType<UnityFrameInputProcessingBoundary>();
            Transform subjectRoot = product.transform;
            LabTargetBindingSource bindings = new LabTargetBindingSource(subjectRoot, LabBindingScope.FrozenMonkeyScope);
            UnityPhysicalInputDriver driver = new UnityPhysicalInputDriver();
            LabActionScopePolicy policy = new LabActionScopePolicy(PhysicalInteractionCapabilities.PointerClick, LabTargetCatalog.UsePotion);
            UnityInteractionAutomationRuntime runtime = LabComposition.CreateProductionRuntime(product, driver, boundary, bindings, policy);
            SeededPointerMonkey monkey = UnityPointerMonkeyFactory.Create(runtime,
                new PointerMonkeyConfiguration(12345, new ScrollDelta(0, -1), TimeSpan.FromMilliseconds(250)), new LabDragDestinationPolicy());
            Task<PointerMonkeyStep> selection = monkey.SelectNextAsync(1).AsTask();
            while (!selection.IsCompleted) yield return null;
            if (selection.IsFaulted) throw selection.Exception;
            PointerMonkeyStep step = selection.Result;
            InteractionTargetBinding useBinding = Find(bindings, LabTargetCatalog.UsePotion);
            UnityPointerClickObserver observer = useBinding.GetComponent<UnityPointerClickObserver>();
            UnityEngine.Object.Destroy(observer);
            yield return null;
            UnityPointerMonkeyVerifier verifier = new UnityPointerMonkeyVerifier(bindings, driver.Mouse.deviceId);
            bool missingObserverDetected = false;
            try
            {
                verifier.ResetObservation(step);
            }
            catch (InvalidOperationException)
            {
                missingObserverDetected = true;
            }
            Assert.That(missingObserverDetected, Is.True, "Removing a required observer must fail verification setup.");
            useBinding.gameObject.AddComponent<UnityPointerClickObserver>();

            InteractionTargetBinding sword = Find(bindings, LabTargetCatalog.Sword);
            GameObject duplicate = UnityEngine.Object.Instantiate(sword.gameObject, subjectRoot);
            duplicate.name = "FaultInjectionDuplicateTarget";
            yield return null;
            bool duplicateDetected = false;
            try
            {
                bindings.CaptureBindings();
            }
            catch (InvalidOperationException)
            {
                duplicateDetected = true;
            }
            Assert.That(duplicateDetected, Is.True, "Duplicate TargetId must fail fast.");
            UnityEngine.Object.Destroy(duplicate);
            driver.Dispose();
        }

        private static InteractionTargetBinding Find(LabTargetBindingSource bindings, string targetId)
        {
            foreach (InteractionTargetBinding binding in bindings.CaptureBindings())
            {
                if (binding.TargetId == targetId) return binding;
            }
            throw new InvalidOperationException("Binding was not found: " + targetId);
        }
    }
}
