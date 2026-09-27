using System;

namespace Project.InteractionAutomationLab.Domain
{
    [Serializable]
    public sealed class LabBusinessSnapshot
    {
        public LabGameMode gameMode;
        public bool equipmentLocked;
        public int gold;
        public int potionCount;
        public string selectedItemId;
        public string equippedWeaponId;
        public string equippedOffhandId;
        public LabSortMode sortMode;
        public bool musicEnabled;
        public bool sfxEnabled;
        public bool detailsExpanded;
        public int helpOpenCount;
        public int emergencyActionCount;
        public int chargeCompletedCount;
        public int canaryActionCount;
        public int buyAttemptCount;
        public int buySuccessCount;
        public int useAttemptCount;
        public int useSuccessCount;
        public string lastOutcome;

        public static LabBusinessSnapshot Capture(LabState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            LabBusinessSnapshot snapshot = new LabBusinessSnapshot();
            snapshot.gameMode = state.GameMode;
            snapshot.equipmentLocked = state.EquipmentLocked;
            snapshot.gold = state.Gold;
            snapshot.potionCount = state.PotionCount;
            snapshot.selectedItemId = state.SelectedItemId;
            snapshot.equippedWeaponId = state.EquippedWeaponId;
            snapshot.equippedOffhandId = state.EquippedOffhandId;
            snapshot.sortMode = state.SortMode;
            snapshot.musicEnabled = state.MusicEnabled;
            snapshot.sfxEnabled = state.SfxEnabled;
            snapshot.detailsExpanded = state.DetailsExpanded;
            snapshot.helpOpenCount = state.HelpOpenCount;
            snapshot.emergencyActionCount = state.EmergencyActionCount;
            snapshot.chargeCompletedCount = state.ChargeCompletedCount;
            snapshot.canaryActionCount = state.CanaryActionCount;
            snapshot.buyAttemptCount = state.BuyAttemptCount;
            snapshot.buySuccessCount = state.BuySuccessCount;
            snapshot.useAttemptCount = state.UseAttemptCount;
            snapshot.useSuccessCount = state.UseSuccessCount;
            snapshot.lastOutcome = state.LastOutcome;
            return snapshot;
        }
    }
}
