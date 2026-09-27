using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.Verification.Oracle;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Domain;
using Project.InteractionAutomationLab.Presentation;

namespace Project.InteractionAutomationLab.Verification
{
    public sealed class LabMonkeyRunAdapter : IUnityPointerMonkeyScenarioVerification, IUnityPointerMonkeyRunObserver
    {
        private readonly LabProductController product;
        private readonly LabTargetBindingSource bindings;
        private LabBusinessSnapshot before;
        private UnityPointerDropObserver dropObserver;

        public LabMonkeyRunAdapter(LabProductController product, LabTargetBindingSource bindings)
        {
            this.product = product ?? throw new ArgumentNullException(nameof(product));
            this.bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public void ResetObservation(PointerMonkeyStep step)
        {
            before = product.CaptureBusinessState();
            dropObserver = null;
            if (!step.DestinationTargetId.HasValue) return;
            InteractionTargetBinding destination = Resolve(step.DestinationTargetId.Value.Value);
            dropObserver = destination.GetComponent<UnityPointerDropObserver>();
            if (dropObserver == null) throw new InvalidOperationException("Drag destination is missing UnityPointerDropObserver.");
            dropObserver.ResetObservation();
        }

        public EvaluationReport AddVerification(PointerMonkeyStep step, EvaluationReport hostReport)
        {
            LabBusinessSnapshot after = product.CaptureBusinessState();
            OracleResult productResult = LabProductStepVerifier.Evaluate(step, before, after);
            List<OracleResult> results = new List<OracleResult>(hostReport.Results);
            results.Add(productResult);
            TestVerdict verdict = Combine(hostReport.Verdict, productResult.Verdict);
            if (step.DestinationTargetId.HasValue)
            {
                bool observed = dropObserver != null && dropObserver.DropCount > 0;
                OracleResult dropResult = new OracleResult(observed ? TestVerdict.Passed : TestVerdict.Failed,
                    "lab.host.drop-observed", observed ? "Destination observed one or more drops." : "Destination observed no drop.");
                results.Add(dropResult);
                verdict = Combine(verdict, dropResult.Verdict);
            }
            return new EvaluationReport("lab.monkey.host-and-product", step.CoverageKey.ToString(), verdict, results, hostReport.Errors);
        }

        public void OnSelected(PointerMonkeyStep step, PointerMonkeyCoverageSnapshot coverage) { }
        public ValueTask BeforeExecutionAsync(PointerMonkeyStep step, CancellationToken cancellationToken) { return default; }
        public void OnCompleted(UnityPointerMonkeyStepResult result) { }
        public ValueTask AfterExecutionAsync(PointerMonkeyStep step, CancellationToken cancellationToken) { return default; }
        public void OnSelectionFailure(int sequence, string detail) { }

        private InteractionTargetBinding Resolve(string targetId)
        {
            foreach (InteractionTargetBinding binding in bindings.CaptureBindings())
            {
                if (binding.TargetId == targetId) return binding;
            }
            throw new InvalidOperationException("Binding was not found: " + targetId);
        }

        private static TestVerdict Combine(TestVerdict left, TestVerdict right)
        {
            return Rank(right) > Rank(left) ? right : left;
        }

        private static int Rank(TestVerdict verdict)
        {
            if (verdict == TestVerdict.InfrastructureError) return 5;
            if (verdict == TestVerdict.Failed) return 4;
            if (verdict == TestVerdict.Inconclusive) return 3;
            if (verdict == TestVerdict.Passed) return 2;
            return 1;
        }
    }
}
