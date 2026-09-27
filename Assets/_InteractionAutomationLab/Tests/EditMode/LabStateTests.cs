using NUnit.Framework;
using Project.InteractionAutomationLab.Domain;

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
    }
}
