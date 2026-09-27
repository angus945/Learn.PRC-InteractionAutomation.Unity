using Project.InteractionAutomationLab.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Presentation
{
    public sealed class LabProductController : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryPage;
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private RectTransform dragPresentationRoot;
        [SerializeField] private CanvasGroup gameplayLockGroup;
        [SerializeField] private CanvasGroup quickBypassGroup;
        [SerializeField] private Text headerText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Text eventLogText;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;

        private readonly LabState state = new LabState();

        public LabState State => state;
        public RectTransform DragPresentationRoot => dragPresentationRoot;
        public CanvasGroup GameplayLockGroup => gameplayLockGroup;
        public CanvasGroup QuickBypassGroup => quickBypassGroup;

        private void Start()
        {
            ShowInventory();
            ApplyStateToUi();
        }

        public void ResetProduct()
        {
            state.Reset();
            gameplayLockGroup.interactable = true;
            quickBypassGroup.interactable = true;
            quickBypassGroup.ignoreParentGroups = true;
            ShowInventory();
            ApplyStateToUi();
        }

        public void ShowInventory()
        {
            inventoryPage.SetActive(true);
            settingsPage.SetActive(false);
            ApplyStateToUi();
        }

        public void ShowSettings()
        {
            inventoryPage.SetActive(false);
            settingsPage.SetActive(true);
            ApplyStateToUi();
        }

        public void SelectItem(string itemId)
        {
            state.SelectItem(itemId);
            ApplyStateToUi();
        }

        public void ToggleSort()
        {
            state.ToggleSort();
            ApplyStateToUi();
        }

        public void InspectSelected()
        {
            state.InspectSelected();
            ApplyStateToUi();
        }

        public void CompleteCharge()
        {
            state.CompleteCharge();
            ApplyStateToUi();
        }

        public void BuyPotion()
        {
            state.BuyPotion();
            ApplyStateToUi();
        }

        public void UsePotion()
        {
            state.UsePotion();
            ApplyStateToUi();
        }

        public void OpenHelp()
        {
            state.OpenHelp();
            ApplyStateToUi();
        }

        public void EmergencyAction()
        {
            state.EmergencyAction();
            ApplyStateToUi();
        }

        public void MusicChanged(bool enabled)
        {
            state.SetMusic(enabled);
            ApplyStateToUi();
        }

        public void SfxChanged(bool enabled)
        {
            state.SetSfx(enabled);
            ApplyStateToUi();
        }

        public void ActivateCanary()
        {
            state.ActivateCanary();
            ApplyStateToUi();
        }

        public void TryEquip(string itemId, string slotId)
        {
            state.TryEquip(itemId, slotId);
            ApplyStateToUi();
        }

        public void SetFixtureState(LabGameMode mode, bool equipmentLocked, int gold, int potionCount)
        {
            state.SetFixtureState(mode, equipmentLocked, gold, potionCount);
            ApplyStateToUi();
        }

        public LabBusinessSnapshot CaptureBusinessState()
        {
            return LabBusinessSnapshot.Capture(state);
        }

        private void ApplyStateToUi()
        {
            if (headerText != null)
            {
                headerText.text = "Mode " + state.GameMode + "   Gold " + state.Gold + "   Potion " + state.PotionCount +
                    "   Weapon " + ValueOrDash(state.EquippedWeaponId) + "   Offhand " + ValueOrDash(state.EquippedOffhandId);
            }
            if (detailsText != null)
            {
                detailsText.text = "Selected: " + state.SelectedItemId + "\nSort: " + state.SortMode + "\nDetails: " +
                    (state.DetailsExpanded ? "Expanded" : "Collapsed") + "\nLast: " + state.LastOutcome;
            }
            if (eventLogText != null)
            {
                eventLogText.text = "Buy " + state.BuySuccessCount + "/" + state.BuyAttemptCount +
                    "  Use " + state.UseSuccessCount + "/" + state.UseAttemptCount +
                    "\nHelp " + state.HelpOpenCount + "  Emergency " + state.EmergencyActionCount +
                    "  Charge " + state.ChargeCompletedCount + "  Canary " + state.CanaryActionCount;
            }
            if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(state.MusicEnabled);
            if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(state.SfxEnabled);
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }
    }
}
