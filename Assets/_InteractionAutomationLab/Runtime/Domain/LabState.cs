using System;

namespace Project.InteractionAutomationLab.Domain
{
    public enum LabGameMode
    {
        Normal = 0,
        Cutscene = 1
    }

    public enum LabSortMode
    {
        Name = 0,
        Category = 1
    }

    [Serializable]
    public sealed class LabState
    {
        public LabGameMode GameMode { get; private set; }
        public bool EquipmentLocked { get; private set; }
        public int Gold { get; private set; }
        public int PotionCount { get; private set; }
        public string SelectedItemId { get; private set; }
        public string EquippedWeaponId { get; private set; }
        public string EquippedOffhandId { get; private set; }
        public LabSortMode SortMode { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool SfxEnabled { get; private set; }
        public bool DetailsExpanded { get; private set; }
        public int HelpOpenCount { get; private set; }
        public int EmergencyActionCount { get; private set; }
        public int ChargeCompletedCount { get; private set; }
        public int CanaryActionCount { get; private set; }
        public int BuyAttemptCount { get; private set; }
        public int BuySuccessCount { get; private set; }
        public int UseAttemptCount { get; private set; }
        public int UseSuccessCount { get; private set; }
        public string LastOutcome { get; private set; }

        public LabState()
        {
            Reset();
        }

        public void Reset()
        {
            GameMode = LabGameMode.Normal;
            EquipmentLocked = false;
            Gold = 30;
            PotionCount = 2;
            SelectedItemId = "sword";
            EquippedWeaponId = string.Empty;
            EquippedOffhandId = string.Empty;
            SortMode = LabSortMode.Name;
            MusicEnabled = true;
            SfxEnabled = true;
            DetailsExpanded = false;
            HelpOpenCount = 0;
            EmergencyActionCount = 0;
            ChargeCompletedCount = 0;
            CanaryActionCount = 0;
            BuyAttemptCount = 0;
            BuySuccessCount = 0;
            UseAttemptCount = 0;
            UseSuccessCount = 0;
            LastOutcome = "Reset";
        }

        public void SetFixtureState(LabGameMode gameMode, bool equipmentLocked, int gold, int potionCount)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (potionCount < 0) throw new ArgumentOutOfRangeException(nameof(potionCount));
            GameMode = gameMode;
            EquipmentLocked = equipmentLocked;
            Gold = gold;
            PotionCount = potionCount;
            LastOutcome = "FixtureSetup";
        }

        public void SelectItem(string itemId)
        {
            if (!IsOwnedItem(itemId)) throw new ArgumentException("Unknown inventory item.", nameof(itemId));
            if (GameMode == LabGameMode.Cutscene)
            {
                LastOutcome = "GameplayInputLocked";
                return;
            }
            SelectedItemId = itemId;
            LastOutcome = "Selected:" + itemId;
        }

        public void ToggleSort()
        {
            if (RejectGameplayInput()) return;
            SortMode = SortMode == LabSortMode.Name ? LabSortMode.Category : LabSortMode.Name;
            LastOutcome = "Sort:" + SortMode;
        }

        public void InspectSelected()
        {
            if (RejectGameplayInput()) return;
            DetailsExpanded = true;
            LastOutcome = "Inspected:" + SelectedItemId;
        }

        public void CompleteCharge()
        {
            if (RejectGameplayInput()) return;
            ChargeCompletedCount++;
            LastOutcome = "ChargeCompleted";
        }

        public void BuyPotion()
        {
            BuyAttemptCount++;
            if (RejectGameplayInput()) return;
            if (Gold < 10)
            {
                LastOutcome = "InsufficientCurrency";
                return;
            }
            Gold -= 10;
            PotionCount++;
            BuySuccessCount++;
            LastOutcome = "PotionPurchased";
        }

        public void UsePotion()
        {
            UseAttemptCount++;
            if (RejectGameplayInput()) return;
            if (PotionCount <= 0)
            {
                LastOutcome = "NoPotion";
                return;
            }
            PotionCount--;
            UseSuccessCount++;
            LastOutcome = "PotionUsed";
        }

        public void OpenHelp()
        {
            HelpOpenCount++;
            LastOutcome = "HelpOpened";
        }

        public void EmergencyAction()
        {
            if (RejectGameplayInput()) return;
            EmergencyActionCount++;
            LastOutcome = "EmergencyAction";
        }

        public void SetMusic(bool enabled)
        {
            MusicEnabled = enabled;
            LastOutcome = "Music:" + enabled;
        }

        public void SetSfx(bool enabled)
        {
            SfxEnabled = enabled;
            LastOutcome = "Sfx:" + enabled;
        }

        public void ActivateCanary()
        {
            CanaryActionCount++;
            LastOutcome = "CanaryAction";
        }

        public bool TryEquip(string itemId, string slotId)
        {
            if (GameMode == LabGameMode.Cutscene)
            {
                LastOutcome = "GameplayInputLocked";
                return false;
            }
            if (EquipmentLocked)
            {
                LastOutcome = "EquipmentLocked";
                return false;
            }
            if (slotId == "weapon" && itemId == "sword")
            {
                EquippedWeaponId = itemId;
                LastOutcome = "Equipped:sword";
                return true;
            }
            if (slotId == "offhand" && itemId == "shield")
            {
                EquippedOffhandId = itemId;
                LastOutcome = "Equipped:shield";
                return true;
            }
            LastOutcome = "IncompatibleEquipment";
            return false;
        }

        private bool RejectGameplayInput()
        {
            if (GameMode != LabGameMode.Cutscene) return false;
            LastOutcome = "GameplayInputLocked";
            return true;
        }

        private static bool IsOwnedItem(string itemId)
        {
            return itemId == "sword" || itemId == "shield" || itemId == "potion" || itemId == "key";
        }
    }
}
