# EXP-RPC-001｜Unity 單場景 × .NET Console 讀寫實驗路線圖

## 目標與完成定義

一個 Unity 場景、一個外部 .NET Console 程序、一條本機 WebSocket 連線。外部可讀取 Unity 當前狀態，也可提交一段文字；Unity 在主執行緒套用文字後回覆，Game View 顯示同一份狀態。退出 Play Mode、關閉 Console 或手動斷線時，不遺留連線或未觀測的例外。

本文件是實驗規劃，不表示下列 Project API、檔案或驗收已完成。使用者本機有尚未 push 的 asmdef 修正；後續實作以本機工作樹為基準，先檢視差異，不重置、不強制更新 submodule、不以遠端版本覆蓋修正。本次規劃沒有修改 repository。

本輪刻意驗證 RPC 讀寫與 Unity 執行緒／生命週期邊界，不是 physical-input 或遊戲玩法測試。直接設定實驗文字的成功，不代表 UI hit-test、Target 或滑鼠模擬已完成。

## 1. 固定範圍

| 項目 | 本輪選擇 |
|---|---|
| Unity | 使用本機現有版本與 asmdef；先跑 Editor Play Mode，後續再驗證 desktop Mono |
| 場景 | RpcLab.unity，一個場景，不切換場景 |
| 外部 | 一個 .NET Console；沿用現有範例的 net8.0 目標，不建立桌面 GUI 或網頁前端 |
| 連線 | Console 在 127.0.0.1:6123 的 /rpc 監聽；Unity 手動連入 |
| 通訊 | 共用 JSON-RPC + WebSocket Module，只有一個活動 Unity peer |
| 使用者操作 | Unity Connect / Disconnect；Console read / send <text> / exit |
| 狀態 | Message、Revision；另回報 InstanceId、Frame、HandlerThreadId |
| 不做 | Target、PhysicalInput、Monkey、Verification、Overlay、動態方法掃描、重連策略、跨網路部署、吞吐量壓測 |

Console 使用既有範例的 ASP.NET Core WebSocket 接收機制即可。它依然是一個命令列程序；本輪不自己解析 HTTP upgrade 或重寫 WebSocket server。

## 2. 程序角色與連線方向

```text
.NET Console（監聽端）
  ├─ 開啟 loopback WebSocket endpoint
  ├─ 接收 Unity 連線
  └─ 命令迴圈：read / send / exit
                   ▲
                   │ Unity 主動 Connect
                   │ 建立後 JSON-RPC 可雙向呼叫
                   ▼
Unity Editor / 單一場景
  ├─ Project bootstrap：註冊方法、建立連線
  ├─ UnityRpcHost：Update 派送與生命週期
  └─ RpcLabState：讀取與寫入實驗狀態
```

監聽端不等於「只能被 RPC 呼叫的一端」。現有 smoke sample 已採用 Console 監聽、Unity 連入，再由 Console 呼叫 sample.unity.frame，Unity 也呼叫 Console 的 sample.echo。[S1][S2][S3]

所有場景存取都走已包裝的 catalog 與 UnityRpcHost，不把 transport callback 直接接到 Unity 狀態。UnityRpcHost.Initialize 必須在主執行緒、Awake 後執行；目前的實作以 Update 呼叫 Pump。[S4]

## 3. 重用與新增範圍

重用 Module.Communication.JsonRpc、Module.Communication.JsonRpc.WebSocket、Module.Communication.Unity3D。已掛載的 Workspace.InteractionAutomation 本輪不需引用其任何能力。

沿用本機已選定的 source / DLL 匯入方式，先確認同一 assembly 只有一個有效實作。遠端 baseline 採用 RPC core 與 Unity adapter source assembly、netstandard2.1 transport DLL；本機未 push 的變更仍需實作時另行檢視，不能直接推定完全相同。[S5]

建議全部新增在學習 Project 內，不建立新的 reusable repository：

```text
Assets/
├─ CraftyRacoon/                    # 既有 submodules；保留本機修正
└─ _Project/RpcLab/
   ├─ Scenes/RpcLab.unity
   ├─ Contracts/                   # 同一份 Project-owned DTO 與方法名稱
   └─ Runtime/
      ├─ RpcLabState.cs
      ├─ RpcLabBootstrap.cs
      └─ RpcLabDebugPanel.cs

tools/
└─ RpcLab.Console/                 # Console 原始碼不放 Assets
   ├─ Program.cs
   └─ RpcLab.Console.csproj
```

Contracts 可建立 Project.RpcLab.Contracts：Unity 透過 asmdef，Console 透過 ProjectReference 使用相同來源；不要把 DTO 複製成兩套，也不必先抽出跨產品的通用 DTO Module。DTO 僅包含 string / int 等 JSON 資料，不暴露 GameObject、Transform、UnityEngine 類型或 CLR 執行個體參考。

Unity 會編譯的新增原始碼使用 C# 9 相容語法。Console 的編譯設定與 Unity 分開，但共用 DTO 必須符合兩端；不因 Console 能編譯就認定 Unity 能匯入。Unity 6.3 官方語言版本為 C# 9.0。[S6]

## 4. 單場景最小配置

```text
RpcLab
├─ Main Camera
└─ RpcLabRoot
   ├─ UnityRpcHost                 # 既有元件
   ├─ RpcLabBootstrap              # 連線、註冊、斷線
   ├─ RpcLabState                  # 唯一狀態 owner
   └─ RpcLabDebugPanel             # 只讀顯示 + 本地測試入口
```

DebugPanel 可先用簡單 OnGUI，不需要另外導入 UI framework 或美術資產。顯示 Connected / Disconnected、Message、Revision、Frame；提供 Local Set、Connect、Disconnect。

RpcLabState 初始 Message 為空字串、Revision 為 0。InstanceId 在每次建立新的 Play Mode 實驗狀態時重新產生。所有文字修改都透過 SetMessage，不直接暴露可任意寫入的 public 狀態欄位。顯示與遠端讀取觀察同一份狀態。

## 5. 學習與實作切片

### EXP-00｜固定可編譯基線

**學習問題：** source、asmdef、預編譯 DLL 與 SDK build 是不同的邊界。

**工作：**

- 保留並盤點本機尚未 push 的 asmdef 修改，不執行 reset / force checkout。
- 先確認空場景能進出 Play Mode；確認引用 RPC core、Unity adapter 與 transport 所需型別時無編譯／載入錯誤。
- 外部 Console 可以單独建置與啟動。
- 檢查沒有重複 RPC core DLL，也沒有 SDK test、bin/obj 或 Console 原始碼進入 Unity compilation。
- SDK build outputs 留在 Project 指定的 Assets 外產物位置。

**驗收：** Unity 能進入 Play Mode；Console 能啟動。此時尚不接網路。若依賴尚未齊全，停在本切片處理，不把它歸因為 RPC 呼叫問題。

### EXP-01｜本地狀態與畫面

**學習問題：** 先建立確定正常的狀態讀寫入口，再接遠端 adapter。

**工作：** 建立 RpcLabState、簡單顯示與 Local Set。從本地入口設定文字為「Local」；讀取當前快照。讀取不得改動 Revision。

**驗收：** 畫面與 ReadState 都看到 Message="Local"、Revision=1。兩次讀取不增加 Revision。重進 Play Mode 後為新 InstanceId、空文字與 Revision=0。

**限制：** 不持久化到場景或檔案，不先寫背包、角色或其他玩法模型。

### EXP-02｜兩程序建立連線

**學習問題：** 連線方向與 RPC 呼叫方向不同；Connected 不等於業務操作已驗證。

**工作：**

- Console 先監聽 ws://127.0.0.1:6123/rpc；拒絕第二個活動 peer，不做多連線管理。
- 沿用既有 smoke sample 的明確 token 與 loopback 限制。Token 由環境提供，不存進場景、source、URL 或 log。
- Unity Enter Play Mode 後按 Connect，先建立 catalog，再交給 UnityRpcHost.Initialize 包裝並啟動 transport。
- 用現有 sample.echo / sample.unity.frame 完成第一個 smoke round trip。
- Console 監聽持續運行，Unity連線期間保持 server handler 的生命週期；關閉後釋放該 peer。

**驗收：** Console 看得到 Unity frame 回覆；Unity 看得到 echo 回覆。未 Connect 前，不應有 RPC handler 自動執行。Unity 尚未完成初始化時，不開始學習用指令。

**環境注意：** 現有 token 範例要求在啟動 Unity Editor 與 Console 之前設定 COMMUNICATION_PROBE_TOKEN；已啟動的 Editor 不會取得之後在別的 shell 新設的變數。[S1]

### EXP-03｜Console 讀取 Unity 狀態

**學習問題：** Request / Response、DTO、真正的 Unity 主執行緒執行位置。

**工作：** 把 smoke sample 的固定每秒輪詢改成明確的 Console 命令 read。呼叫新方法 lab.state.read；handler 經 UnityRpcHost.Update 派送後，讀取狀態與 Time.frameCount，回傳 DTO。

**回覆欄位：** InstanceId、Message、Revision、Frame、HandlerThreadId。Unity 在 Awake 記錄自己的 main-thread ID，用來比對實際 handler 執行緒；不假設主執行緒 ID 必定是 1。

**驗收：** 同一個 Play session 兩次 read 的 InstanceId 一樣，Frame 隨 Unity更新前進，Message 與 Revision 在沒有寫入時不變，HandlerThreadId 等於 Unity記錄的主執行緒 ID。讀取不修改狀態。

大多數 Unity API 必須從主執行緒呼叫，不能只因一次執行沒報錯就認定背景執行緒安全。[S7]

### EXP-04｜Console 傳送文字，Unity 套用後回覆

**學習問題：** 傳輸已完成與應用已完成不同；Mutation 應有正式入口與可觀測結果。

**工作：** 新增 send <text>，將 send 後方整段內容當作文字，呼叫 lab.message.set。Unity handler 驗證輸入、呼叫 RpcLabState.SetMessage、增加 Revision，然後才回傳更新後的 LabStateDto。

第一版文字規則：接受空字串與 Unicode，不自動 trim；拒絕 null 或 text.Length > 256（.NET UTF-16 code unit 數）。每次成功 SetMessage 都只增加一次 Revision，即使文字與前次相同；無效輸入不改狀態。這是實驗規則，不是通訊 Module 的規則。

**驗收範例（預期輸出，不是實測）：**

```text
> read
message="" revision=0 frame=120 instance=...

> send Hello from console
APPLIED message="Hello from console" revision=1 frame=180

> read
message="Hello from console" revision=1 frame=240
```

Game View 顯示同樣文字與 Revision。APPLIED 代表狀態已修改，不宣稱 GPU已完成下一畫面呈現。

接著送入中文、含空格的文字與超限輸入。無效輸入應回傳可理解的錯誤，下一次 read 的內容與 Revision 不變。

**順序：** Console 一次只執行一個 request，await 回覆後才接受下一筆操作；不並行發出 set/read。使用 CallAsync 等待業務回覆，不用 NotifyAsync 表示寫入完成。

### EXP-05｜關閉、斷線與逾時

**學習問題：** 連線生命週期、在途工作的未知結果與安全停止。

| 情境 | 驗收 |
|---|---|
| Console 未啟動就 Connect | 顯示連線失敗，Unity 主執行緒不被同步等待卡住 |
| Token 錯誤 | 拒絕連線，不註冊可用的遠端控制 session |
| 手動 Disconnect | 停止目前 session，read/send 不使用失效連線 |
| 退出 Play Mode 或停用 Root | Console 得知斷線，舊 session 不再被重用 |
| Console exit / Ctrl+C | Unity 在取得斷線結果並更新後顯示 Disconnected；不遺留未觀測例外 |
| 再次 Enter Play Mode / Connect | 使用新連線與新的實驗實例；不重播上一個 session 的命令 |
| 暫停 Unity 超過呼叫 timeout | Console 回報 OutcomeUnknown，不直接重送；恢復後先重新讀取狀態確認 |

建議連續進出 Play Mode / 手動重連三輪；不新增自動重連、heartbeat或重試 framework。

目前 wire profile 的 timeout/cancellation 只停止本地等待，不撤銷遠端工作；不同 request 也沒有跨方法交易／順序保證。[S8] 檢查連線 lifetime token 能避免部分未開始工作，但不能聲稱中斷後所有已執行寫入都被回滾。

Unity 本身也不會在退出 Play Mode 時自動停止所有背景工作，因此要明確處理取消、釋放與 Task 例外觀测。[S7]

## 6. 最小應用契約

下列名稱與 DTO 是此學習 Project 的提案，不是共用 Module 已有的應用 API。

| 方法 | Request | Response | 寫入狀態 |
|---|---|---|---|
| lab.state.read | ReadStateRequest（空物件） | LabStateDto | 否 |
| lab.message.set | SetMessageRequest { Text } | LabStateDto | 是，正式 setter 完成後回覆 |

LabStateDto：InstanceId、Message、Revision、Frame、HandlerThreadId。全部為普通 C# 9 class/property，無 record struct、file-scoped namespace 或 Unity 型別。

Console 命令最少只做 read / send <text> / exit。sample.echo 可留作 smoke check；不再加入動態 method discovery、反射欄位修改或任意程式碼執行入口。

呼叫使用共用模組的 CallAsync，不在兩端重新手寫 JSON-RPC parser。現有 WebSocket profile 是 one positional payload：params:[payload]，不是把 payload 直接作為任意 named params；一般使用 module API 即可遵守。[S8]

## 7. 三條必須維持的邊界

1. Project 負責來源、組裝、場景、loopback endpoint與應用方法；實驗方法不塞回通訊 Module。
2. 狀態寫入與快照建立在 Unity主執行緒；網路等待不得用 .Wait() 或 .Result 阻塞該執行緒。handler 本轮保持短小同步狀態操作。
3. 接收請求、派送開始、狀態已套用、Console 收到回覆分別可觀測；以寫入回覆與後續 read 證明效果，不以 socket Connected 代替。

## 8. 本輪完成後才考慮的延伸

本輪成功標準到 EXP-05 即止。可選 EXP-06 是 Unity 主動發送 lab.state.changed，讓本地修改即時出現在 Console；事件發送失敗不回滾已套用狀態，不先做複雜訂閱或重播。

後續獨立實驗才加入 target.capture 與 input.submit，透過 input pipeline測試點擊。RPC文字寫入不作為該後續測試的替代路徑，也不先建立 Monkey Framework。

## 參考依據

- [S1] [Module.Communication.Unity3D/docs/smoke-test.md](https://github.com/crafty-racoon/Module.Communication.Unity3D/blob/main/docs/smoke-test.md)
- [S2] [既有外部 LoopbackServer/Program.cs](https://github.com/crafty-racoon/Module.Communication.Unity3D/blob/main/samples~/LoopbackServer/Program.cs)
- [S3] [既有 CommunicationSmokeProbe.cs](https://github.com/crafty-racoon/Module.Communication.Unity3D/blob/main/samples~/UnityProbe/CommunicationSmokeProbe.cs)
- [S4] [UnityRpcHost.cs](https://github.com/crafty-racoon/Module.Communication.Unity3D/blob/main/src/UnityRpcHost.cs)
- [S5] [學習 Project 的遠端 README baseline](https://github.com/angus945/Learn.PRC-InteractionAutomation.Unity/blob/main/README.md)
- [S6] [Unity 6.3 C# compiler and language version reference](https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html)
- [S7] [Unity 6.3 Awaitable completion and continuation](https://docs.unity3d.com/6000.3/Documentation/Manual/async-awaitable-continuations.html)
- [S8] [JSON-RPC WebSocket wire profile](https://github.com/crafty-racoon/Module.Communication.JsonRpc.WebSocket/blob/main/docs/wire-profile.md)

參考的是可讀取的遠端內容；本機尚未 push 的 asmdef 變更未在此驗證。
