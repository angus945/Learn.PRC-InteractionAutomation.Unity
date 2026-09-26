# InteractionAutomation.Unity3D Learning Roadmap

## 1. 目前目標

專案：

`https://github.com/angus945/Learn.PRC-InteractionAutomation.Unity`

目前暫停 WebSocket / JSON-RPC / 外部 Console 學習線，重新把重點放回：

> **在 Unity Play Mode 中，建立一套能發現 Interaction Target，並透過接近真實玩家輸入路徑操作目標的 Interaction Automation。**

第一階段全部在單一 Unity process、單一場景內完成。

```text
Unity Scene
    ↓
Interaction Target Discovery
    ↓
InteractionTargetSnapshot
    ↓
選擇 Target
    ↓
Physical Input
    ↓
Unity 原本的 Input / EventSystem / Gameplay route
    ↓
實際產品行為
```

核心原則：

```text
Automation 決定「在哪裡輸入」
而不是直接呼叫「要做什麼行為」
```

例如：

```text
正確：

Target Bounds
→ 中心點
→ Mouse Move
→ Mouse Down
→ Mouse Up
→ EventSystem
→ Button click


不採用：

Automation
→ button.onClick.Invoke()

Automation
→ ExecuteEvents.Execute()

Automation
→ Door.Open()
```

---

# 2. 暫時不做的項目

目前先排除：

- WebSocket
- JSON-RPC
- 外部 .NET Console
- Remote automation
- Overlay
- Playwright
- Verification
- Monkey Framework
- Replay
- CI automation
- 多 process
- 多場景 orchestration

Communication modules 可以繼續留在 Project，但不屬於目前學習主線。

---

# 3. 已安裝的相關 Submodule

Project 目前有：

```text
Assets/CraftyRacoon/
├─ Workspace.InteractionAutomation/
└─ Workspace.InteractionAutomation.Unity3D/
```

## Workspace.InteractionAutomation

Repo：

`https://github.com/crafty-racoon/Workspace.InteractionAutomation`

Project 固定 revision：

```text
b660e088a1f37948ed3c9b9cecbdaf47b2071b17
```

目前 coordinate contract 已固定為 top-left origin、+X right、+Y down。
`IPhysicalInputDriver` completion semantics 亦已明確定義為「input 已提交給 host input system」，不代表 host / UI / gameplay 已處理。

目前提供三個基礎 Module：

```text
Module.InteractionAutomation.Coordinates
Module.InteractionAutomation.Targets
Module.InteractionAutomation.PhysicalInput
```

已知主要 contracts：

```text
Coordinates
├─ InteractionCoordinateSpace
├─ InteractionPoint
├─ InteractionSize
└─ InteractionRect

Targets
├─ InteractionTargetId
├─ InteractionTargetRole
├─ PhysicalInteractionCapabilities
├─ InteractionTargetSnapshot
└─ IInteractionTargetSource

PhysicalInput
├─ IPhysicalInputDriver
├─ PhysicalKey
├─ PointerButton
└─ ScrollDelta
```

---

## Workspace.InteractionAutomation.Unity3D

Repo：

`https://github.com/crafty-racoon/Workspace.InteractionAutomation.Unity3D`

Project 固定 revision：

```text
16da7f4567b4a6f30a5c51c7c60e0fd69af857a2
```

目前已包含：

```text
Workspace.InteractionAutomation.Unity3D
│
├─ Module.InteractionAutomation.Coordinates.Unity3D
│  └─ UnityScreenCoordinates
│
├─ Module.InteractionAutomation.Targets.Unity3D
│  ├─ InteractionTargetBinding
│  ├─ UnityInteractionTargetSource
│  └─ UnityUiScreenGeometry
│
└─ Module.InteractionAutomation.PhysicalInput.Unity3D
   └─ UnityPhysicalInputDriver
```

目前已完成 target discovery、UI screen geometry、canonical ↔ Unity screen conversion、Virtual Mouse 建立與 pointer move submission。Physical Input PlayMode test 已開始驗證 queued input 在 Unity lifecycle 推進後可由 Mouse state 觀察。

不要一開始就建立大型 Unity automation framework。

---

# 4. 第一階段最終目標

建立一個單場景實驗：

```text
InteractionAutomationLab
│
├─ UI
│  ├─ ConfirmButton
│  └─ MusicToggle
│
├─ World
│  └─ Chest / Cube
│
└─ Automation
   └─ ExperimentRunner
```

按下：

```text
Run One Interaction
```

Automation 可以做到：

```text
1. Capture targets

2. 取得：
   button.confirm
   toggle.music
   chest.001

3. 選 button.confirm

4. 取得它的 Screen Bounds

5. 計算中心點

6. 送出：
   pointer move
   pointer down
   pointer up

7. Unity 正常 Input Route 收到輸入

8. Button 的正常產品行為發生
```

理想 debug output：

```text
Targets: 3

[0] button.confirm
    Bounds=(420,280,160,60)
    Capabilities=PointerClick

[1] toggle.music
    Bounds=(420,360,160,60)
    Capabilities=PointerClick

[2] chest.001
    Bounds=(700,380,90,120)
    Capabilities=PointerClick

Selected:
    button.confirm

Input:
    Move (500,310)
    Down Left
    Up Left
```

---

# 5. Roadmap

---

## EXP-IA-001A — 第一個 Unity Interaction Target

### 目的

先不做輸入。

只完成：

```text
GameObject
    ↓
InteractionTargetBinding
    ↓
UnityInteractionTargetSource
    ↓
IInteractionTargetSource
    ↓
InteractionTargetSnapshot
```

### 第一個 Target

使用普通 uGUI Button：

```text
ConfirmButton
├─ RectTransform
├─ Image
├─ Button
└─ InteractionTargetBinding
```

`InteractionTargetBinding` 第一版只需要：

```text
TargetId
Role
Capabilities
```

例如：

```text
TargetId = button.confirm
Role = Button
Capabilities = PointerClick
```

### 驗收

Play Mode 中呼叫：

```text
CaptureTargets
```

能得到：

```text
button.confirm
```

不要求 Bounds 正確。

不要求點擊。

### 本切片學習內容

- Unity Component 作為 adapter metadata
- `IInteractionTargetSource`
- Domain contract 與 Unity object 的邊界
- explicit target registration

---

## EXP-IA-001B — UI Target Geometry

### 目的

讓：

```text
RectTransform
```

轉換成：

```text
InteractionRect
```

並建立明確 coordinate-space 語意。

至少區分：

```text
Viewport
Window
Screen
```

第一版只選定一種 canonical output。

建議先用：

```text
Screen
```

### 路徑

```text
RectTransform
    ↓
World Corners
    ↓
Canvas / Camera conversion
    ↓
Screen Rect
    ↓
InteractionTargetSnapshot.Bounds
```

### 驗收

畫面上的 Button：

```text
位置 / 尺寸
```

與 Capture 出來的 Bounds 一致。

在 Game View 改 resolution 後重新 Capture，也仍然正確。

### 不做

- occlusion
- 3D target
- UI Toolkit
- multi-display
- complex Canvas nesting optimization

---

## EXP-IA-001C — Virtual Pointer 最小實驗

### 目的

開始研究真正的 input injection。

目標 route：

```text
Automation
    ↓
Unity Input System
    ↓
Virtual Mouse / queued input
    ↓
InputSystemUIInputModule
    ↓
EventSystem
    ↓
GraphicRaycaster
    ↓
UI
```

### 重要要求

不是：

```text
PointerClickHandler 直接呼叫
Button.onClick.Invoke()
ExecuteEvents.Execute()
```

而是建立實際輸入事件。

### 第一版能力

只需要：

```text
MovePointerAsync
PointerDownAsync
PointerUpAsync
```

暫時不做：

```text
Keyboard
Scroll
Drag
Multi-touch
Gamepad
```

### 驗收

畫面能看見 virtual pointer position 改變。

EventSystem 能觀察到 pointer state。

但本切片還不要求 Button click 成功。

---

## EXP-IA-001D — 第一個真正自動 Click

### 目的

整合：

```text
Target Discovery
+
Bounds
+
Physical Input
```

Runner：

```text
Capture target
    ↓
找 button.confirm
    ↓
Bounds.Center
    ↓
Move
    ↓
Down
    ↓
Up
```

### 驗收

場景 Button 的正常 callback：

```text
ClickCount++
```

Automation 沒有直接存取：

```text
Button
UnityEvent
callback
```

只能透過 Target Snapshot + Physical Input。

這是第一階段最重要的 milestone。

---

## EXP-IA-001E — Toggle / 不同 UI 控制元件

### 目的

證明方案不是只對單一 Button 特製。

加入：

```text
MusicToggle
```

同樣只暴露：

```text
Target Snapshot
Capabilities = PointerClick
```

Automation 點擊它。

### 驗收

```text
False → True
True → False
```

都經過 Unity 原始 UI input route。

### 重點

Automation 不需要知道：

```text
Button
Toggle
```

的具體產品行為。

Automation 只看到：

```text
Target
Bounds
Capabilities
```

---

## EXP-IA-002A — 3D World Target

完成 UI 後才加入 3D。

例如：

```text
Chest
├─ Collider
├─ ChestBehaviour
└─ InteractionTargetBinding
```

Targets.Unity3D 需要把：

```text
Collider / Renderer bounds
```

投影成：

```text
Screen target geometry
```

### 驗收

Capture：

```text
chest.001
Role = WorldObject
Capabilities = PointerClick
Bounds = ...
```

---

## EXP-IA-002B — 用同一套 Pointer 點 3D Target

產品側自己有正常 picking：

```text
Mouse position
    ↓
Camera.ScreenPointToRay
    ↓
Physics.Raycast
    ↓
Chest
```

Automation 仍只做：

```text
Move
Down
Up
```

### 驗收

```text
UI Button
```

與：

```text
3D Chest
```

可以使用同一個：

```text
IPhysicalInputDriver
```

操作。

---

# 6. 第二階段：Target Availability

等最基本 click 成立後，再改善：

```text
InteractionTargetSnapshot
```

目前只有類似：

```text
IsVisible
IsEnabled
```

未來需要逐步區分：

```text
Existence
Geometry validity
Visual visibility
Input reachability
Occlusion
Gameplay eligibility
```

例如：

```text
Target exists
    = true

Visible
    = true

Input reachable
    = false

Reason
    = CoveredByModal
```

這是後續，不應阻塞第一個 click。

---

# 7. 第三階段：Interaction Runner

完成 Target + PhysicalInput 後再建立：

```text
InteractionRunner
```

最小流程：

```csharp
targets = CaptureTargets();

target = SelectTarget(targets);

point = target.Bounds.Center;

await input.MovePointerAsync(point);
await input.PointerDownAsync(Left);
await input.PointerUpAsync(Left);
```

第一版：

```text
Deterministic
Explicit target
One action
```

不要直接開始 random Monkey。

---

# 8. 第四階段：Monkey

有穩定 Runner 後才進入：

```text
while (...)
{
    targets = Capture();

    candidate = SelectCandidate(targets);

    action = SelectSupportedAction(candidate);

    Execute(action);
}
```

第一版 Monkey 可以只有：

```text
Random Target
+
Pointer Click
```

再逐步加入：

```text
Scroll
Drag
Keyboard
Timing
Sequences
```

---

# 9. 第五階段：Observation / Verification

等 input route 成熟後，再加入：

```text
Before
Action
After
```

例如：

```text
Before:
    ClickCount = 3

Action:
    button.confirm click

After:
    ClickCount = 4
```

此時再考慮接：

```text
Workspace.Verification
```

現在不要提前整合。

---

# 10. 第六階段：Remote

最後才重新回到先前暫停的 Communication。

屆時：

```text
External Tool
    ↓
Communication
    ↓
InteractionAutomation API
    ↓
Target Capture / Physical Input
    ↓
Unity
```

Communication 只負責：

```text
Call / Notify / Transport
```

InteractionAutomation 自己擁有：

```text
interaction.targets.capture
interaction.input.submit
```

等 remote semantic binding。

這時才恢復：

```text
WebSocket
JSON-RPC
.NET Console
Overlay
external Monkey
```

---

# 11. 重要架構邊界

## `Workspace.InteractionAutomation`

只擁有 host-neutral contracts：

```text
Coordinates
Targets
PhysicalInput
```

不能依賴：

```text
UnityEngine
Unity Input System
uGUI
UI Toolkit
```

---

## `Workspace.InteractionAutomation.Unity3D`

負責 Unity adapter：

```text
Unity types
    ↕
InteractionAutomation contracts
```

可能逐步形成：

```text
Module.InteractionAutomation.Targets.Unity3D
Module.InteractionAutomation.PhysicalInput.Unity3D
```

不要放：

```text
Monkey workflow
QA scenario
project gameplay logic
remote RPC endpoint
```

---

## Project

`Learn.PRC-InteractionAutomation.Unity`

負責：

```text
Scene
Composition
Experiment Runner
Test UI
Concrete target bindings
Actual package/submodule revisions
```

也就是：

```text
Reusable capability → Module / Workspace
學習流程與組裝 → Project
```

---

# 12. 下一個 Chat 的起始任務

直接從：

# `EXP-IA-001C：確認 EventSystem 能觀察 Virtual Pointer`

繼續。

目前已確認的責任邊界：

```text
MovePointerAsync completion
    =
input submission completed

!= InputSystem processing completed
!= EventSystem processing completed
!= gameplay behavior completed
```

下一步應驗證：

```text
Canonical InteractionPoint
    ↓
UnityPhysicalInputDriver
    ↓
Virtual Mouse event queued
    ↓
Unity Input System processes event
    ↓
InputSystemUIInputModule / EventSystem observes pointer
```

暫時不要進 PointerDown / PointerUp click sequence，也不要建立 generic synchronization framework。

---

# 13. 當前進度總結

```text
[完成]
Workspace.InteractionAutomation 基礎 contracts
Canonical coordinate contract
EXP-IA-001A Target Discovery
EXP-IA-001B UI Target Geometry
Virtual Mouse device 建立
Canonical Screen → Unity Screen conversion
MovePointerAsync input submission
IPhysicalInputDriver completion semantics
Physical Input PlayMode processing verification test added

[進行中]
EXP-IA-001C Virtual Pointer
- 新增的 Physical Input PlayMode test 尚待在 Unity Test Runner 執行確認
- EventSystem / InputSystemUIInputModule observation 尚待驗證

[尚未]
Pointer Down / Up
Button Automation Click
Toggle
World Target
Monkey
Verification
Remote Communication
```

目前 Project pins：

```text
Workspace.InteractionAutomation
b660e088a1f37948ed3c9b9cecbdaf47b2071b17

Workspace.InteractionAutomation.Unity3D
16da7f4567b4a6f30a5c51c7c60e0fd69af857a2
```

下一個工作不要重新研究 WebSocket 或 Communication，繼續 EXP-IA-001C 的 EventSystem pointer observation。
