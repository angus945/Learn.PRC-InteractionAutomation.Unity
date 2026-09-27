using System;
using Framework.InteractionAutomation.Monkey;
using Module.InteractionAutomation.Targets;
using Module.Verification.Oracle;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Domain;

namespace Project.InteractionAutomationLab.Verification
{
    public static class LabProductStepVerifier
    {
        public static OracleResult Evaluate(PointerMonkeyStep step, LabBusinessSnapshot before, LabBusinessSnapshot after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            bool valid = after.gold >= 0 && after.potionCount >= 0;
            string detail = "Product invariants satisfied.";
            if (step.Capability == PhysicalInteractionCapabilities.PointerHover || step.Capability == PhysicalInteractionCapabilities.Scroll)
            {
                valid = valid && StableBusinessCore(before, after);
                detail = "Query/presentation action did not mutate business state.";
            }
            else if (step.ActionId == "pointer.drag")
            {
                valid = valid && VerifyDrag(step, after);
                detail = "Equipment pair and inventory persistence verified.";
            }
            else if (step.TargetId.Value == LabTargetCatalog.BuyPotion)
            {
                OracleResult purchase = LabProductInvariantOracle.EvaluatePurchase(before, after);
                valid = valid && purchase.Verdict == TestVerdict.Passed;
                detail = purchase.Detail;
            }
            else if (step.TargetId.Value == LabTargetCatalog.UsePotion)
            {
                bool success = before.potionCount > 0;
                valid = valid && after.useAttemptCount == before.useAttemptCount + 1;
                valid = valid && after.potionCount == (success ? before.potionCount - 1 : before.potionCount);
                detail = success ? "Potion use consumed exactly one." : "Empty potion use was rejected without mutation.";
            }
            else if (step.TargetId.Value == LabTargetCatalog.Help)
            {
                valid = valid && after.helpOpenCount == before.helpOpenCount + 1;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Emergency)
            {
                valid = valid && after.emergencyActionCount == before.emergencyActionCount + 1;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Charge)
            {
                valid = valid && after.chargeCompletedCount == before.chargeCompletedCount + 1;
            }
            else if (step.TargetId.Value == LabTargetCatalog.NonSelectable)
            {
                valid = valid && after.canaryActionCount == before.canaryActionCount + 1;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Inspect)
            {
                valid = valid && after.detailsExpanded;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Music)
            {
                valid = valid && after.musicEnabled != before.musicEnabled;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Sfx)
            {
                valid = valid && after.sfxEnabled != before.sfxEnabled;
            }
            else if (step.TargetId.Value == LabTargetCatalog.Sort)
            {
                valid = valid && after.sortMode != before.sortMode;
            }
            else if (step.ActionId == "pointer.click" && step.TargetId.Value.StartsWith("lab.inventory.item.", StringComparison.Ordinal))
            {
                string itemId = step.TargetId.Value.Substring("lab.inventory.item.".Length);
                valid = valid && after.selectedItemId == itemId;
            }
            return new OracleResult(valid ? TestVerdict.Passed : TestVerdict.Failed, "lab.product.outcome", detail);
        }

        private static bool VerifyDrag(PointerMonkeyStep step, LabBusinessSnapshot after)
        {
            if (!step.DestinationTargetId.HasValue) return false;
            if (step.TargetId.Value == LabTargetCatalog.Sword && step.DestinationTargetId.Value.Value == LabTargetCatalog.Weapon)
                return after.equippedWeaponId == "sword";
            if (step.TargetId.Value == LabTargetCatalog.Shield && step.DestinationTargetId.Value.Value == LabTargetCatalog.Offhand)
                return after.equippedOffhandId == "shield";
            return false;
        }

        private static bool StableBusinessCore(LabBusinessSnapshot left, LabBusinessSnapshot right)
        {
            return left.gold == right.gold && left.potionCount == right.potionCount && left.selectedItemId == right.selectedItemId &&
                left.equippedWeaponId == right.equippedWeaponId && left.equippedOffhandId == right.equippedOffhandId &&
                left.buyAttemptCount == right.buyAttemptCount && left.useAttemptCount == right.useAttemptCount &&
                left.helpOpenCount == right.helpOpenCount && left.emergencyActionCount == right.emergencyActionCount &&
                left.chargeCompletedCount == right.chargeCompletedCount && left.canaryActionCount == right.canaryActionCount;
        }
    }
}
