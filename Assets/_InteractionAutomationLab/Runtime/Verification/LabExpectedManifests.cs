using System;
using System.Collections.Generic;

namespace Project.InteractionAutomationLab.Verification
{
    public enum LabMonkeyPreset
    {
        M0Normal = 0,
        M1ParentUiLocked = 1,
        M2Cutscene = 2,
        M3Settings = 3
    }

    public static class LabExpectedManifests
    {
        private static readonly IReadOnlyDictionary<LabMonkeyPreset, IReadOnlyList<string>> Values =
            new Dictionary<LabMonkeyPreset, IReadOnlyList<string>>
            {
                [LabMonkeyPreset.M0Normal] = Lines(
                    "pointer.hold:lab.actions.charge|pointer.hover:lab.actions.charge|pointer.click:lab.actions.use-potion|pointer.hover:lab.actions.use-potion|" +
                    "pointer.hover:lab.canary.disabled-selectable|pointer.click:lab.canary.nonselectable|pointer.hover:lab.canary.nonselectable|pointer.double-click:lab.details.inspect|" +
                    "pointer.hover:lab.details.inspect|pointer.hover:lab.equipment.offhand|pointer.hover:lab.equipment.weapon|pointer.click:lab.inventory.item.key|" +
                    "pointer.hover:lab.inventory.item.key|pointer.click:lab.inventory.item.potion|pointer.hover:lab.inventory.item.potion|pointer.click:lab.inventory.item.shield|" +
                    "pointer.drag:lab.inventory.item.shield->lab.equipment.offhand|pointer.hover:lab.inventory.item.shield|pointer.click:lab.inventory.item.sword|" +
                    "pointer.drag:lab.inventory.item.sword->lab.equipment.weapon|pointer.hover:lab.inventory.item.sword|pointer.click:lab.inventory.sort|pointer.hover:lab.inventory.sort|" +
                    "pointer.click:lab.quick.emergency|pointer.hover:lab.quick.emergency|pointer.click:lab.quick.help|pointer.hover:lab.quick.help|" +
                    "pointer.click:lab.shop.buy-potion|pointer.hover:lab.shop.buy-potion"),
                [LabMonkeyPreset.M1ParentUiLocked] = Lines(
                    "pointer.hover:lab.actions.charge|pointer.hover:lab.actions.use-potion|pointer.hover:lab.canary.disabled-selectable|pointer.click:lab.canary.nonselectable|" +
                    "pointer.hover:lab.canary.nonselectable|pointer.hover:lab.details.inspect|pointer.hover:lab.equipment.offhand|pointer.hover:lab.equipment.weapon|" +
                    "pointer.hover:lab.inventory.item.key|pointer.hover:lab.inventory.item.potion|pointer.hover:lab.inventory.item.shield|pointer.hover:lab.inventory.item.sword|" +
                    "pointer.hover:lab.inventory.sort|pointer.click:lab.quick.emergency|pointer.hover:lab.quick.emergency|pointer.click:lab.quick.help|" +
                    "pointer.hover:lab.quick.help|pointer.hover:lab.shop.buy-potion"),
                [LabMonkeyPreset.M2Cutscene] = Lines(
                    "pointer.hover:lab.actions.charge|pointer.hover:lab.actions.use-potion|pointer.hover:lab.canary.disabled-selectable|pointer.click:lab.canary.nonselectable|" +
                    "pointer.hover:lab.canary.nonselectable|pointer.hover:lab.details.inspect|pointer.hover:lab.equipment.offhand|pointer.hover:lab.equipment.weapon|" +
                    "pointer.hover:lab.inventory.item.key|pointer.hover:lab.inventory.item.potion|pointer.hover:lab.inventory.item.shield|pointer.hover:lab.inventory.item.sword|" +
                    "pointer.hover:lab.inventory.sort|pointer.hover:lab.quick.emergency|pointer.click:lab.quick.help|pointer.hover:lab.quick.help|pointer.hover:lab.shop.buy-potion"),
                [LabMonkeyPreset.M3Settings] = Lines(
                    "pointer.hover:lab.canary.disabled-selectable|pointer.click:lab.canary.nonselectable|pointer.hover:lab.canary.nonselectable|" +
                    "pointer.hover:lab.settings.logscroll|pointer.scroll:lab.settings.logscroll|pointer.click:lab.settings.music|pointer.hover:lab.settings.music|" +
                    "pointer.click:lab.settings.sfx|pointer.hover:lab.settings.sfx")
            };

        public static IReadOnlyList<string> Get(LabMonkeyPreset preset)
        {
            return Values[preset];
        }

        private static IReadOnlyList<string> Lines(string value)
        {
            return Array.AsReadOnly(value.Split('|'));
        }
    }
}
