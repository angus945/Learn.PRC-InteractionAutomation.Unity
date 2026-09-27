# Interaction Automation State Lab

`InteractionAutomationStateLab` 是既有 AutoLab 之外的獨立複合 fixture。它保留舊場景與 probes，並直接復用 Runner、Monkey、Unity UI-state availability、pointer observers 與 verification profiles 的公開能力。

## 開啟與操作

場景：`Assets/_InteractionAutomationLab/Scenes/InteractionAutomationStateLab.unity`

開啟場景後直接 Play。左側是可手動操作的背包／裝備／商店／設定產品介面；右側 Lab Control 不屬於 automation target scope。

- `Run Scripted Acceptance`：執行 S01–S10。每個 scenario 都有獨立 setup、正式 Runner 輸入、Host observation、Product outcome 與 finally cleanup。
- `Run Frozen Monkey Campaign`：執行 M0–M3 × seeds `12345`、`7`、`98765`，每組 64 steps，並重跑一次比較 coverage-key 序列。
- `Stop`：取消目前 run，等待 gesture release cleanup 後才回到 Idle。
- `Reset`：僅可在 Idle 使用，還原產品、UI 與結果。
- `Export Last Result`：輸出最近一次 completed report。

Evidence 位置：

```text
<Application.persistentDataPath>/InteractionAutomationLab/<RunId>/
  manifest.json
  steps.jsonl
  summary.json
```

若 revision 無法由 runtime 可靠取得，manifest 明確寫入 `unknown`，不填假 SHA。

## Authoring 與驗證

Unity 選單：

```text
Tools/Interaction Automation Lab/Rebuild State Lab
Tools/Interaction Automation Lab/Validate State Lab
```

CLI：

```powershell
unity run . --editor-version 6000.3.20f1 -- -executeMethod Project.InteractionAutomationLab.Editor.LabSceneAuthoring.Build
unity run . --editor-version 6000.3.20f1 -- -executeMethod Project.InteractionAutomationLab.Editor.LabSceneValidator.ValidateSceneAsset
```

Rebuild 是冪等操作，且只寫入 `Assets/_InteractionAutomationLab` 與將新場景加入 Editor Build Settings。它不修改 `_AutomationLab`。場景和三個 prefab 都是 authoring-time 資產；Play 時不會用巨型 MonoBehaviour 動態補建 20 個 bindings。

Validator 檢查：20 個固定 TargetId、ID 唯一、capabilities 與 catalog 一致、必要 observer 完整、每個 target 有同物件且可 raycast 的 Graphic。

## Scope 與 composition

- `ProductScope`：SubjectRoot 下當下 active 的 bindings，用於 scripted journey。
- `FrozenMonkeyScope`：相同來源但排除兩個 navigation targets，用於狀態固定 campaign。
- Operational discovery 排除 inactive objects；頁面切換後 target 是 `NotDiscovered`，不偽造成 Available。
- Production admission = structural + `UseUnityUiStateAvailability()` + `LabProjectAvailabilityPolicy`。
- S10 的 `ProductNegativeInputRuntime` 只保留 structural evaluator，用來驗證產品自身防線；不會進一般 campaign。
- 一個 session 只建立一個虛擬滑鼠。CLI PlayMode 無 Game View focus 時，session 暫時把 InputSystem focus routing 設為可處理背景虛擬輸入，cleanup 時還原原值。

## 固定 manifests

Expected manifests 是 `LabExpectedManifests` 中的獨立固定資料，不由 production evaluator 產生：

| Preset | Eligible keys |
| --- | ---: |
| M0 Normal | 29 |
| M1 Parent UI Locked | 18 |
| M2 Cutscene | 17 |
| M3 Settings | 9 |

每次主要 run 要求 64/64、前 E 個成功 steps 無重複且完整覆蓋 expected set、全部選擇位於 expected set、Host/Product verification 通過、trace 無覆寫，並與同 preset/seed/初始狀態的重跑序列一致。

## 實際測試結果（2026-09-27）

| 項目 | 狀態 | 結果 |
| --- | --- | --- |
| Scene validator | Passed | 20 unique targets；observer/control mapping 完整 |
| State Lab EditMode | Passed | 3/3，包括 faulty -9 Gold product oracle detection |
| Scripted acceptance | Passed | S01–S10，包含 selection invalidation 零 input/coverage 與 Hold/Drag cancellation cleanup |
| Frozen Monkey campaign | Passed | 12/12 主要 runs；每 run 64/64；29/18/17/9；deterministic replay Passed |
| Fault injection | Passed | missing observer、duplicate TargetId、faulty purchase arithmetic 均被檢出 |
| 全專案 EditMode regression | Passed | 150/150，0 failure，0 skipped |
| 全專案 PlayMode regression | Passed | 12/12，0 failure，0 skipped |
| 1280×720／1920×1080 人工視覺檢查 | NotRun | 本次 CLI batch Game View 為 640×480；已使用 anchors 與 1920×1080 CanvasScaler，但未宣稱人工驗收 |
| OS-level input isolation | NotRun | 不在本 fixture 宣稱範圍 |

代表性 campaign report：`M0Normal / seed 12345` 為 `64/64`、`coverage 29/29`、`coverageFirst=true`、`deterministicReplay=true`、`hostAndProductPassed=true`。一次實際 export 產生 768 筆主要 run step JSONL。

## 明確不在範圍

遮擋／Modal、裁切後可達點搜尋、多 Camera／Camera Stack／RenderTexture／多 Display、additive scene／場景轉換／同 ID incarnation 替換、UI Toolkit、文字與 Keyboard 自動化、長 gesture 途中持續重新授權、任意遊戲狀態 deterministic replay。這些項目不可由本 Lab 的 Passed 結果推論為已驗證。
