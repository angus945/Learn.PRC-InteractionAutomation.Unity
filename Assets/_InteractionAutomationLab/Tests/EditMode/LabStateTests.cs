using Module.Verification.Oracle;
using NUnit.Framework;
using Project.InteractionAutomationLab.Domain;
using Project.InteractionAutomationLab.Verification;

namespace Project.InteractionAutomationLab.Tests
{
    public sealed class LabStateTests
    {
        [Test]
        public void ResourceFailuresRemainAttemptsWithoutInvalidMutation()
        {
            LabState state = new LabState();
            state.SetFixtureState(LabGameMode.Normal, false, 0, 0);
            state.BuyPotion();
            Assert.That(state.Gold, Is.Zero);
            Assert.That(state.PotionCount, Is.Zero);
            Assert.That(state.BuyAttemptCount, Is.EqualTo(1));
            Assert.That(state.BuySuccessCount, Is.Zero);
            Assert.That(state.LastOutcome, Is.EqualTo("InsufficientCurrency"));
            state.UsePotion();
            Assert.That(state.PotionCount, Is.Zero);
            Assert.That(state.UseAttemptCount, Is.EqualTo(1));
            Assert.That(state.UseSuccessCount, Is.Zero);
            Assert.That(state.LastOutcome, Is.EqualTo("NoPotion"));
        }

        [Test]
        public void EquipmentAcceptsOnlySpecifiedPairs()
        {
            LabState state = new LabState();
            Assert.That(state.TryEquip("sword", "offhand"), Is.False);
            Assert.That(state.EquippedOffhandId, Is.Empty);
            Assert.That(state.TryEquip("sword", "weapon"), Is.True);
            Assert.That(state.EquippedWeaponId, Is.EqualTo("sword"));
            Assert.That(state.TryEquip("sword", "weapon"), Is.True);
            Assert.That(state.EquippedWeaponId, Is.EqualTo("sword"));
        }

        [Test]
        public void FaultyNineGoldPurchase_IsDetectedByProductOracle()
        {
            LabBusinessSnapshot before = FaultyPurchaseFixture.CreateBefore();
            LabBusinessSnapshot after = FaultyPurchaseFixture.ApplyNineGoldBug(before);
            OracleResult result = LabProductInvariantOracle.EvaluatePurchase(before, after);
            Assert.That(result.Verdict, Is.EqualTo(TestVerdict.Failed));
            Assert.That(result.Code, Is.EqualTo("lab.product.purchase-arithmetic"));
        }

        private static class FaultyPurchaseFixture
        {
            public static LabBusinessSnapshot CreateBefore()
            {
                LabBusinessSnapshot snapshot = new LabBusinessSnapshot();
                snapshot.gold = 30;
                snapshot.potionCount = 2;
                snapshot.buyAttemptCount = 0;
                snapshot.buySuccessCount = 0;
                return snapshot;
            }

            public static LabBusinessSnapshot ApplyNineGoldBug(LabBusinessSnapshot before)
            {
                LabBusinessSnapshot snapshot = new LabBusinessSnapshot();
                snapshot.gold = before.gold - 9;
                snapshot.potionCount = before.potionCount + 1;
                snapshot.buyAttemptCount = before.buyAttemptCount + 1;
                snapshot.buySuccessCount = before.buySuccessCount + 1;
                return snapshot;
            }
        }
    }
}
