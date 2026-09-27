using System;
using Module.Verification.Oracle;
using Project.InteractionAutomationLab.Domain;

namespace Project.InteractionAutomationLab.Verification
{
    public static class LabProductInvariantOracle
    {
        public static OracleResult EvaluatePurchase(LabBusinessSnapshot before, LabBusinessSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            bool successExpected = before.gold >= 10;
            bool attempts = after.buyAttemptCount == before.buyAttemptCount + 1;
            bool success = successExpected ? after.buySuccessCount == before.buySuccessCount + 1 :
                after.buySuccessCount == before.buySuccessCount;
            bool arithmetic = successExpected ? after.gold == before.gold - 10 && after.potionCount == before.potionCount + 1 :
                after.gold == before.gold && after.potionCount == before.potionCount;
            bool valid = attempts && success && arithmetic && after.gold >= 0 && after.potionCount >= 0;
            string detail = successExpected ? "Purchase must deduct exactly 10 Gold and add one potion." :
                "Rejected purchase must preserve Gold and potion count.";
            return new OracleResult(valid ? TestVerdict.Passed : TestVerdict.Failed, "lab.product.purchase-arithmetic", detail);
        }
    }
}
