using System;
using System.Collections.Generic;
using System.IO;
using Module.InteractionAutomation.Observation.Unity3D;
using Module.InteractionAutomation.Targets;
using Module.InteractionAutomation.Targets.Unity3D;
using Module.InteractionAutomation.Timing.Unity3D;
using Project.InteractionAutomationLab.Automation;
using Project.InteractionAutomationLab.Presentation;
using Project.InteractionAutomationLab.Scenarios;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.InteractionAutomationLab.Editor
{
    public static class LabSceneAuthoring
    {
        private const string RootPath = "Assets/_InteractionAutomationLab";
        private const string ScenePath = RootPath + "/Scenes/InteractionAutomationStateLab.unity";
        private const string PrefabPath = RootPath + "/Prefabs";

        private static readonly Color Background = new Color(0.055f, 0.071f, 0.11f, 1f);
        private static readonly Color Panel = new Color(0.10f, 0.13f, 0.19f, 1f);
        private static readonly Color Control = new Color(0.16f, 0.22f, 0.31f, 1f);
        private static readonly Color Accent = new Color(0.12f, 0.60f, 0.78f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.95f, 1f, 1f);

        [MenuItem("Tools/Interaction Automation Lab/Rebuild State Lab")]
        public static void Build()
        {
            EnsureFolder(RootPath);
            EnsureFolder(RootPath + "/Scenes");
            EnsureFolder(PrefabPath);
            string itemPrefabPath = CreateItemPrefab();
            string slotPrefabPath = CreateSlotPrefab();
            string actionPrefabPath = CreateActionPrefab();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "InteractionAutomationStateLab";

            GameObject sceneRoot = new GameObject("InteractionAutomationStateLab");
            CreateEventSystem(sceneRoot.transform);
            GameObject labHost = CreateObject("LabHost", sceneRoot.transform);
            UnityFrameInputProcessingBoundary boundary = labHost.AddComponent<UnityFrameInputProcessingBoundary>();
            LabSessionController session = labHost.AddComponent<LabSessionController>();

            GameObject canvasObject = CreateObject("Canvas", sceneRoot.transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject background = CreatePanel("Background", canvasObject.transform, Background);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject subjectRoot = CreatePanel("SubjectRoot", canvasObject.transform, Background);
            Stretch(subjectRoot.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.74f, 1f), new Vector2(16f, 16f), new Vector2(-8f, -16f));
            LabProductController controller = subjectRoot.AddComponent<LabProductController>();

            Text header = CreateText("StateHeader", subjectRoot.transform, "State", 24, TextAnchor.MiddleLeft);
            Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -70f), new Vector2(-16f, -12f));
            GameObject navigation = CreatePanel("Navigation", subjectRoot.transform, Panel);
            Anchor(navigation.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -122f), new Vector2(-16f, -76f));
            Button inventoryNavigation = CreateButton(navigation.transform, "Inventory", LabTargetCatalog.NavigationInventory);
            Anchor(inventoryNavigation.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.49f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));
            Button settingsNavigation = CreateButton(navigation.transform, "Settings", LabTargetCatalog.NavigationSettings);
            Anchor(settingsNavigation.GetComponent<RectTransform>(), new Vector2(0.51f, 0f), new Vector2(1f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));

            GameObject inventoryPage = CreatePanel("InventoryPage", subjectRoot.transform, Background);
            Anchor(inventoryPage.GetComponent<RectTransform>(), new Vector2(0f, 0.19f), new Vector2(1f, 0.88f), new Vector2(16f, 0f), new Vector2(-16f, -8f));
            CanvasGroup gameplayLockGroup = CreatePanel("GameplayLockGroup", inventoryPage.transform, Background).AddComponent<CanvasGroup>();
            gameplayLockGroup.interactable = true;
            gameplayLockGroup.blocksRaycasts = true;
            Stretch(gameplayLockGroup.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject inventoryPanel = CreateLabeledPanel("InventoryPanel", gameplayLockGroup.transform, "Inventory", new Vector2(0f, 0.49f), new Vector2(0.32f, 1f));
            CreateItemInstance(itemPrefabPath, inventoryPanel.transform, "Sword", "sword", LabTargetCatalog.Sword, 0);
            CreateItemInstance(itemPrefabPath, inventoryPanel.transform, "Shield", "shield", LabTargetCatalog.Shield, 1);
            CreateItemInstance(itemPrefabPath, inventoryPanel.transform, "Potion", "potion", LabTargetCatalog.Potion, 2);
            CreateItemInstance(itemPrefabPath, inventoryPanel.transform, "Key", "key", LabTargetCatalog.Key, 3);
            Button sortButton = CreateButton(inventoryPanel.transform, "Sort inventory", LabTargetCatalog.Sort);
            PlaceRow(sortButton.GetComponent<RectTransform>(), 4, 5);

            GameObject equipmentPanel = CreateLabeledPanel("EquipmentPanel", gameplayLockGroup.transform, "Equipment", new Vector2(0.34f, 0.49f), new Vector2(0.66f, 1f));
            CreateSlotInstance(slotPrefabPath, equipmentPanel.transform, "Weapon slot", "weapon", LabTargetCatalog.Weapon, 0);
            CreateSlotInstance(slotPrefabPath, equipmentPanel.transform, "Offhand slot", "offhand", LabTargetCatalog.Offhand, 1);
            GameObject detailsPanel = CreateLabeledPanel("DetailsPanel", gameplayLockGroup.transform, "Details", new Vector2(0.34f, 0f), new Vector2(0.66f, 0.47f));
            GameObject inspect = InstantiatePrefab(actionPrefabPath, detailsPanel.transform, "Inspect (double click)");
            ReplaceActionControl<LabInspectControl>(inspect);
            ConfigureTarget(inspect, LabTargetCatalog.Inspect);
            PlaceRow(inspect.GetComponent<RectTransform>(), 0, 3);
            Text details = CreateText("DetailsReadout", detailsPanel.transform, "Details", 18, TextAnchor.UpperLeft);
            Anchor(details.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.63f), new Vector2(12f, 12f), new Vector2(-12f, -4f));

            GameObject actionPanel = CreateLabeledPanel("ActionPanel", gameplayLockGroup.transform, "Actions", new Vector2(0.68f, 0.49f), new Vector2(1f, 1f));
            GameObject charge = InstantiatePrefab(actionPrefabPath, actionPanel.transform, "Charge (hold)");
            ReplaceActionControl<LabChargeControl>(charge);
            ConfigureTarget(charge, LabTargetCatalog.Charge);
            PlaceRow(charge.GetComponent<RectTransform>(), 0, 4);
            Button usePotion = CreateButton(actionPanel.transform, "Use potion", LabTargetCatalog.UsePotion);
            PlaceRow(usePotion.GetComponent<RectTransform>(), 1, 4);
            Button buyPotion = CreateButton(actionPanel.transform, "Buy potion (10 gold)", LabTargetCatalog.BuyPotion);
            PlaceRow(buyPotion.GetComponent<RectTransform>(), 2, 4);

            CanvasGroup quickBypass = CreatePanel("QuickBypassGroup", actionPanel.transform, Panel).AddComponent<CanvasGroup>();
            quickBypass.interactable = true;
            quickBypass.blocksRaycasts = true;
            quickBypass.ignoreParentGroups = true;
            Anchor(quickBypass.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.25f), new Vector2(8f, 8f), new Vector2(-8f, -4f));
            Button help = CreateButton(quickBypass.transform, "Help", LabTargetCatalog.Help);
            Anchor(help.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));
            Button emergency = CreateButton(quickBypass.transform, "Emergency", LabTargetCatalog.Emergency);
            Anchor(emergency.GetComponent<RectTransform>(), new Vector2(0.52f, 0f), new Vector2(1f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));

            GameObject settingsPage = CreatePanel("SettingsPage", subjectRoot.transform, Panel);
            Anchor(settingsPage.GetComponent<RectTransform>(), new Vector2(0f, 0.19f), new Vector2(1f, 0.88f), new Vector2(16f, 0f), new Vector2(-16f, -8f));
            Toggle music = CreateToggle(settingsPage.transform, "Music", LabTargetCatalog.Music);
            Anchor(music.GetComponent<RectTransform>(), new Vector2(0.04f, 0.82f), new Vector2(0.46f, 0.94f), Vector2.zero, Vector2.zero);
            Toggle sfx = CreateToggle(settingsPage.transform, "SFX", LabTargetCatalog.Sfx);
            Anchor(sfx.GetComponent<RectTransform>(), new Vector2(0.54f, 0.82f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            ScrollRect logScroll = CreateScrollRect(settingsPage.transform);
            Anchor(logScroll.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.76f), Vector2.zero, Vector2.zero);

            GameObject canaryBench = CreateLabeledPanel("CanaryBench", subjectRoot.transform, "Canary bench", new Vector2(0f, 0f), new Vector2(1f, 0.17f));
            Button disabled = CreateButton(canaryBench.transform, "Disabled selectable", LabTargetCatalog.DisabledSelectable);
            Anchor(disabled.GetComponent<RectTransform>(), new Vector2(0.02f, 0.08f), new Vector2(0.48f, 0.68f), Vector2.zero, Vector2.zero);
            disabled.enabled = false;
            GameObject nonSelectable = CreatePanel("NonSelectableAction", canaryBench.transform, Control);
            nonSelectable.AddComponent<LabNonSelectableControl>();
            CreateCenteredLabel(nonSelectable.transform, "Non-selectable action");
            ConfigureTarget(nonSelectable, LabTargetCatalog.NonSelectable);
            Anchor(nonSelectable.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.98f, 0.68f), Vector2.zero, Vector2.zero);

            GameObject labControlPanel = CreateLabeledPanel("LabControlPanel", canvasObject.transform, "LAB CONTROL", new Vector2(0.75f, 0.02f), new Vector2(0.99f, 0.98f));
            Text diagnostics = CreateText("Diagnostics", labControlPanel.transform,
                "Manual Explore\nReady.\n\nNo target in this panel.\n\nScope exclusions:\n- Occlusion\n- Camera Stack\n- Scene transitions", 18, TextAnchor.UpperLeft);
            Anchor(diagnostics.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.70f), Vector2.zero, Vector2.zero);
            Button runAcceptance = CreateControlButton(labControlPanel.transform, "Run Scripted Acceptance");
            Anchor(runAcceptance.GetComponent<RectTransform>(), new Vector2(0.06f, 0.87f), new Vector2(0.94f, 0.93f), Vector2.zero, Vector2.zero);
            Button runMonkey = CreateControlButton(labControlPanel.transform, "Run Frozen Monkey Campaign");
            Anchor(runMonkey.GetComponent<RectTransform>(), new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);
            Button stop = CreateControlButton(labControlPanel.transform, "Stop");
            Anchor(stop.GetComponent<RectTransform>(), new Vector2(0.06f, 0.71f), new Vector2(0.47f, 0.77f), Vector2.zero, Vector2.zero);
            Button reset = CreateControlButton(labControlPanel.transform, "Reset");
            Anchor(reset.GetComponent<RectTransform>(), new Vector2(0.53f, 0.71f), new Vector2(0.94f, 0.77f), Vector2.zero, Vector2.zero);
            Button export = CreateControlButton(labControlPanel.transform, "Export Last Result");
            Anchor(export.GetComponent<RectTransform>(), new Vector2(0.06f, 0.63f), new Vector2(0.94f, 0.69f), Vector2.zero, Vector2.zero);
            GameObject pointerPresentation = CreatePanel("PointerPresentation", canvasObject.transform, Color.clear);
            Image pointerImage = pointerPresentation.GetComponent<Image>();
            pointerImage.raycastTarget = false;
            pointerPresentation.SetActive(false);

            WireController(controller, inventoryPage, settingsPage, gameplayLockGroup, quickBypass, header, details, diagnostics, music, sfx, canvasObject.GetComponent<RectTransform>());
            WireSession(session, controller, subjectRoot.transform, boundary, diagnostics);
            UnityEventTools.AddPersistentListener(inventoryNavigation.onClick, controller.ShowInventory);
            UnityEventTools.AddPersistentListener(settingsNavigation.onClick, controller.ShowSettings);
            UnityEventTools.AddPersistentListener(sortButton.onClick, controller.ToggleSort);
            UnityEventTools.AddPersistentListener(usePotion.onClick, controller.UsePotion);
            UnityEventTools.AddPersistentListener(buyPotion.onClick, controller.BuyPotion);
            UnityEventTools.AddPersistentListener(help.onClick, controller.OpenHelp);
            UnityEventTools.AddPersistentListener(emergency.onClick, controller.EmergencyAction);
            UnityEventTools.AddPersistentListener(music.onValueChanged, controller.MusicChanged);
            UnityEventTools.AddPersistentListener(sfx.onValueChanged, controller.SfxChanged);
            UnityEventTools.AddPersistentListener(runAcceptance.onClick, session.RunScriptedAcceptance);
            UnityEventTools.AddPersistentListener(runMonkey.onClick, session.RunFrozenMonkeyCampaign);
            UnityEventTools.AddPersistentListener(stop.onClick, session.Stop);
            UnityEventTools.AddPersistentListener(reset.onClick, session.ResetLab);
            UnityEventTools.AddPersistentListener(export.onClick, session.ExportLastResult);

            settingsPage.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            LabSceneValidator.ValidateSceneAsset();
            Debug.Log("CSL-1 scene authored successfully: " + ScenePath);
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystemObject = CreateObject("EventSystem", parent);
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            AssignPersistentUiActions(inputModule);
            inputModule.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
        }

        private static void AssignPersistentUiActions(InputSystemUIInputModule module)
        {
            const string inputActionsPath = "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputActionsPath);
            if (asset == null) throw new InvalidOperationException("Unity default UI input actions were not found.");
            module.UnassignActions();
            module.actionsAsset = asset;
            module.point = FindActionReference(inputActionsPath, "Point");
            module.move = FindActionReference(inputActionsPath, "Navigate");
            module.submit = FindActionReference(inputActionsPath, "Submit");
            module.cancel = FindActionReference(inputActionsPath, "Cancel");
            module.leftClick = FindActionReference(inputActionsPath, "Click");
            module.rightClick = FindActionReference(inputActionsPath, "RightClick");
            module.middleClick = FindActionReference(inputActionsPath, "MiddleClick");
            module.scrollWheel = FindActionReference(inputActionsPath, "ScrollWheel");
            module.trackedDevicePosition = FindActionReference(inputActionsPath, "TrackedDevicePosition");
            module.trackedDeviceOrientation = FindActionReference(inputActionsPath, "TrackedDeviceOrientation");
        }

        private static InputActionReference FindActionReference(string assetPath, string actionName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                InputActionReference reference = asset as InputActionReference;
                if (reference != null && reference.action != null && reference.action.name == actionName) return reference;
            }
            throw new InvalidOperationException("Input action reference was not found: " + actionName);
        }

        private static string CreateItemPrefab()
        {
            string path = PrefabPath + "/LabItemCard.prefab";
            GameObject root = CreatePanel("LabItemCard", null, Control);
            root.AddComponent<LabItemCardControl>();
            CreateCenteredLabel(root.transform, "Item");
            ConfigureTarget(root, LabTargetCatalog.Sword);
            SavePrefab(root, path);
            return path;
        }

        private static string CreateSlotPrefab()
        {
            string path = PrefabPath + "/LabEquipmentSlot.prefab";
            GameObject root = CreatePanel("LabEquipmentSlot", null, new Color(0.20f, 0.27f, 0.22f, 1f));
            root.AddComponent<LabEquipmentSlotControl>();
            CreateCenteredLabel(root.transform, "Slot");
            ConfigureTarget(root, LabTargetCatalog.Weapon);
            root.AddComponent<UnityPointerDropObserver>();
            SavePrefab(root, path);
            return path;
        }

        private static string CreateActionPrefab()
        {
            string path = PrefabPath + "/LabActionControl.prefab";
            GameObject root = CreatePanel("LabActionControl", null, Accent);
            root.AddComponent<LabInspectControl>();
            CreateCenteredLabel(root.transform, "Action");
            ConfigureTarget(root, LabTargetCatalog.Inspect);
            SavePrefab(root, path);
            return path;
        }

        private static void CreateItemInstance(string prefabPath, Transform parent, string label, string itemId, string targetId, int row)
        {
            GameObject instance = InstantiatePrefab(prefabPath, parent, label);
            LabItemCardControl control = instance.GetComponent<LabItemCardControl>();
            control.Configure(itemId);
            ConfigureTarget(instance, targetId);
            PlaceRow(instance.GetComponent<RectTransform>(), row, 5);
        }

        private static void CreateSlotInstance(string prefabPath, Transform parent, string label, string slotId, string targetId, int row)
        {
            GameObject instance = InstantiatePrefab(prefabPath, parent, label);
            LabEquipmentSlotControl control = instance.GetComponent<LabEquipmentSlotControl>();
            control.Configure(slotId);
            ConfigureTarget(instance, targetId);
            PlaceRow(instance.GetComponent<RectTransform>(), row, 3);
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, string label)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
            if (instance == null) throw new InvalidOperationException("Failed to instantiate prefab: " + path);
            instance.name = label.Replace(" ", string.Empty);
            Text text = instance.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
            return instance;
        }

        private static void ReplaceActionControl<T>(GameObject target) where T : Selectable
        {
            LabInspectControl existing = target.GetComponent<LabInspectControl>();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing, true);
            target.AddComponent<T>();
        }

        private static Button CreateButton(Transform parent, string label, string targetId)
        {
            GameObject root = CreatePanel(label.Replace(" ", string.Empty), parent, Accent);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            CreateCenteredLabel(root.transform, label);
            ConfigureTarget(root, targetId);
            return button;
        }

        private static Button CreateControlButton(Transform parent, string label)
        {
            GameObject root = CreatePanel(label.Replace(" ", string.Empty), parent, Control);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            CreateCenteredLabel(root.transform, label);
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string label, string targetId)
        {
            GameObject root = CreatePanel(label + "Toggle", parent, Control);
            Toggle toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.graphic = root.GetComponent<Image>();
            toggle.isOn = true;
            CreateCenteredLabel(root.transform, label);
            ConfigureTarget(root, targetId);
            return toggle;
        }

        private static ScrollRect CreateScrollRect(Transform parent)
        {
            GameObject root = CreatePanel("LogViewport", parent, Control);
            ScrollRect scrollRect = root.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
            GameObject content = CreateObject("Content", root.transform);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 1200f);
            Text text = content.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = TextColor;
            text.alignment = TextAnchor.UpperLeft;
            text.text = BuildLogText();
            text.raycastTarget = false;
            scrollRect.viewport = root.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureTarget(root, LabTargetCatalog.LogScroll);
            return scrollRect;
        }

        private static string BuildLogText()
        {
            string value = "";
            for (int index = 1; index <= 32; index++) value += "State Lab event line " + index + "\n";
            return value;
        }

        private static GameObject CreateLabeledPanel(string name, Transform parent, string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = CreatePanel(name, parent, Panel);
            Anchor(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            Text label = CreateText("Title", panel.transform, title, 18, TextAnchor.UpperLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject target = CreateObject(name, parent);
            RectTransform rect = target.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 80f);
            Image image = target.AddComponent<Image>();
            image.color = color;
            return target;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
        {
            GameObject target = CreateObject(name, parent);
            RectTransform rect = target.AddComponent<RectTransform>();
            Text text = target.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateCenteredLabel(Transform parent, string value)
        {
            Text label = CreateText("Label", parent, value, 18, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 3f), new Vector2(-6f, -3f));
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject target = new GameObject(name);
            if (parent != null) target.transform.SetParent(parent, false);
            return target;
        }

        private static void ConfigureTarget(GameObject target, string targetId)
        {
            LabTargetDescriptor descriptor = LabTargetCatalog.Get(targetId);
            InteractionTargetBinding binding = target.GetComponent<InteractionTargetBinding>();
            if (binding == null) binding = target.AddComponent<InteractionTargetBinding>();
            SerializedObject serialized = new SerializedObject(binding);
            serialized.FindProperty("targetId").stringValue = descriptor.Id;
            serialized.FindProperty("capabilities").intValue = (int)descriptor.Capabilities;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ConfigureObservers(target, descriptor.Capabilities);
        }

        private static void ConfigureObservers(GameObject target, PhysicalInteractionCapabilities capabilities)
        {
            bool click = Has(capabilities, PhysicalInteractionCapabilities.PointerClick) || Has(capabilities, PhysicalInteractionCapabilities.PointerDoubleClick);
            if (click && target.GetComponent<UnityPointerClickObserver>() == null) target.AddComponent<UnityPointerClickObserver>();
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerHover) && target.GetComponent<UnityPointerHoverObserver>() == null) target.AddComponent<UnityPointerHoverObserver>();
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerDrag) && target.GetComponent<UnityPointerDragObserver>() == null) target.AddComponent<UnityPointerDragObserver>();
            if (Has(capabilities, PhysicalInteractionCapabilities.PointerHold) && target.GetComponent<UnityPointerPressObserver>() == null) target.AddComponent<UnityPointerPressObserver>();
            if (Has(capabilities, PhysicalInteractionCapabilities.Scroll) && target.GetComponent<UnityPointerScrollObserver>() == null) target.AddComponent<UnityPointerScrollObserver>();
        }

        private static bool Has(PhysicalInteractionCapabilities value, PhysicalInteractionCapabilities flag)
        {
            return (value & flag) != 0;
        }

        private static void WireController(LabProductController controller, GameObject inventoryPage, GameObject settingsPage,
            CanvasGroup gameplayLockGroup, CanvasGroup quickBypassGroup, Text header, Text details, Text diagnostics,
            Toggle music, Toggle sfx, RectTransform dragRoot)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("inventoryPage").objectReferenceValue = inventoryPage;
            serialized.FindProperty("settingsPage").objectReferenceValue = settingsPage;
            serialized.FindProperty("dragPresentationRoot").objectReferenceValue = dragRoot;
            serialized.FindProperty("gameplayLockGroup").objectReferenceValue = gameplayLockGroup;
            serialized.FindProperty("quickBypassGroup").objectReferenceValue = quickBypassGroup;
            serialized.FindProperty("headerText").objectReferenceValue = header;
            serialized.FindProperty("detailsText").objectReferenceValue = details;
            serialized.FindProperty("eventLogText").objectReferenceValue = diagnostics;
            serialized.FindProperty("musicToggle").objectReferenceValue = music;
            serialized.FindProperty("sfxToggle").objectReferenceValue = sfx;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireSession(LabSessionController session, LabProductController controller, Transform subjectRoot,
            UnityFrameInputProcessingBoundary boundary, Text diagnostics)
        {
            SerializedObject serialized = new SerializedObject(session);
            serialized.FindProperty("product").objectReferenceValue = controller;
            serialized.FindProperty("subjectRoot").objectReferenceValue = subjectRoot;
            serialized.FindProperty("inputProcessingBoundary").objectReferenceValue = boundary;
            serialized.FindProperty("diagnosticsText").objectReferenceValue = diagnostics;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceRow(RectTransform rect, int row, int rowCount)
        {
            float height = 0.74f / rowCount;
            float top = 0.84f - row * height;
            float bottom = top - height + 0.02f;
            Anchor(rect, new Vector2(0.05f, bottom), new Vector2(0.95f, top), Vector2.zero, Vector2.zero);
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == ScenePath) return;
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
