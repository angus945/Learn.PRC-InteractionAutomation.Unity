# Learn.PRC-InteractionAutomation.Unity

Unity JSON-RPC 與 InteractionAutomation 的學習 Host。

**目前只完成 dependency source 掛載，不是已組裝、可直接執行的 Unity 專案。**
本階段不建立場景、網路端點、Monkey Framework 或產品遠端方法。

## Source layout

```text
Assets/
└─ CraftyRacoon/
   ├─ Module.Communication.JsonRpc/             # submodule
   ├─ Module.Communication.JsonRpc.WebSocket/   # submodule
   ├─ Module.Communication.Unity3D/             # submodule
   └─ Workspace.InteractionAutomation/          # submodule
```

四個目錄都是 Git submodule，並非複製的原始碼。
Workspace.InteractionAutomation 內的 Coordinates、Targets、PhysicalInput
由同一個 workspace checkout 提供，不另外複製或重複掛載。
Playwright、Verification 與 gameplay integration 不在此階段加入。

## 初始化

新 clone：

```shell
git clone --recurse-submodules https://github.com/angus945/Learn.PRC-InteractionAutomation.Unity.git
```

已 clone 的工作樹，在 repository 根目錄執行：

```shell
git pull --ff-only
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
```

必須具備各來源 repository 的 GitHub 存取權限；本 repo 的可見性不授予子模組存取權。
初始化不修改來源 repository。

## Source / version ownership

- 此 Project 的 Git tree gitlink 是 exact revision 的權威；`.gitmodules` 描述來源位置。
- 初始化使用已提交的 commit，不使用 `git submodule update --remote` 追逐來源分支。
- 更新來源版本時，由 Project 明確更新並提交 gitlink。
- 每份 reusable source 只有一個 authoritative checkout，不建立 nested dependency checkout、copy 或 mirror。
- `Assets/CraftyRacoon.meta` 與四個掛載根目錄的 `.meta` 由此 Project 擁有；submodule 內的檔案由來源 repo 擁有。

## Unity 組裝仍待完成

本提交尚未建立 `Packages/manifest.json`、`ProjectSettings/ProjectVersion.txt` 或 scene，
也未執行 Unity 編譯、NuGet restore、DLL 發布或 runtime 驗證。

Module.Communication.Unity3D 目前的 asmdef 要求 Project 提供
`Module.Communication.JsonRpc.dll`。RPC core 與 WebSocket 應依各模組文件編譯為
.NET Standard 2.1 的 managed plugin；不能把 net8.0 DLL 當成 Unity plugin。
.NET 原始碼與測試檔直接掛進 Assets 不會自動完成 assembly 隔離，
本提交尚未配置這個隔離，也未匯入第三方 DLL。
因此必須先處理 Project 的 source/DLL 匯入邊界與 assembly references，
再進行 Unity 編譯；不能同時編譯同一個模組的 source 與 DLL。

Host 的 build/composition 由 Project 明確供應以下實際路徑：

| Property | 來源目錄（相對於 repo root） |
|---|---|
| `JsonRpcModuleRoot` | `Assets/CraftyRacoon/Module.Communication.JsonRpc` |
| `JsonRpcWebSocketModuleRoot` | `Assets/CraftyRacoon/Module.Communication.JsonRpc.WebSocket` |
| `InteractionAutomationWorkspaceRoot` | `Assets/CraftyRacoon/Workspace.InteractionAutomation` |

傳給 MSBuild 前由 Project 解析為絕對路徑；本提交不猜測 Unity 安裝位置，
也不新增會自行下載依賴或啟動通訊的流程。
