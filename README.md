# Learn.PRC-InteractionAutomation.Unity

Unity JSON-RPC 與 InteractionAutomation 的學習 Host。
本次修正 C# 版本與 source/DLL 匯入邊界；尚未建立場景、端點或完整可執行專案。

## 固定來源

```text
Assets/CraftyRacoon/
├─ Module.Communication.JsonRpc/            # source assembly
├─ Module.Communication.JsonRpc.WebSocket/  # SDK source checkout; Unity 使用預編譯 DLL
├─ Module.Communication.Unity3D/            # source assembly
└─ Workspace.InteractionAutomation/        # 三個 source assemblies
```

| Submodule | 固定 revision |
|---|---|
| Module.Communication.JsonRpc | 88298902cfbd04c7a7675aa4e9db4cf63d9a2221 |
| Module.Communication.JsonRpc.WebSocket | ce0cb83c8c91dd393c6ef2eb3b55137b190ecf99 |
| Module.Communication.Unity3D | 468782c93b6dd34c256580192604f8e5ed71a931 |
| Workspace.InteractionAutomation | 8fbb1a5b9ee075cfd9729aaf87d03c34265e03ae |

Git tree 的 gitlink 是版本權威；此表只記錄本次更新，不是第二個 lock。
來源皆為 Git submodule，沒有 source copy、mirror 或 nested dependency checkout。

## 更新本機

先保留本機修改。在此 repository 根目錄執行：

```shell
git pull --ff-only
git submodule sync --recursive
git submodule update --init --recursive
git submodule status --recursive
```

不使用 --remote 追逐來源分支。若有本機 submodule 修改或未追蹤檔案阻擋更新，
先檢視、保存再處理；不要直接用 --force 或 git clean 丟棄工作。
來源 repo 的存取權限仍需個別具備。

## C# 與 assembly 邊界

Unity 6 的 source baseline 是 C# 9，不以修改 IDE 產生的 csproj 或 csc.rsp 升級 compiler。
InteractionAutomation 的 production namespace 已改成 block scope，並固定 LangVersion 9.0。
RPC core 原本就是 C# 9，現在有 canonical asmdef。Unity adapter 的 runtime、test 與 sample
已改成引用這個 source assembly，不再要求 Module.Communication.JsonRpc.dll。
因此不要另外匯入核心 DLL。核心 AssemblyInfo 在 SDK/Unity build 共用 0.1.0.0 版本。

三個 InteractionAutomation Module 各自有 src/asmdef，Targets/PhysicalInput 僅依賴 Coordinates。
.NET SDK/NUnit 專用 test/ 以 !UNITY_5_3_OR_NEWER 的 asmdef 排除，不會漏進 Assembly-CSharp；
dotnet test 仍可執行。真正的 Unity Shared/PlayMode tests 仍留在 Unity adapter 的測試 assembly。
既有 Unity adapter .meta GUID 未重建。

## WebSocket 還需要什麼

WebSocket production 也已改成 C# 9，但其 Unity 整合仍採用預編譯 netstandard2.1 transport。
其 src/test asmdef 明確排除 SDK 原始碼；排除不代表已經裝好、可執行 WebSocket。
Project 仍須提供 transport DLL 與經檢查的第三方 managed dependencies。不要用 net8.0 DLL，
也不要從輸出目錄再次匯入 RPC core DLL，或盲目複製 System.* / 重複 Newtonsoft.Json。
這些依賴尚未在本次提交中下載、編譯或匯入。

Directory.Build.props 由這個 Project 提供確定的 dependency roots，並使用 .NET SDK 8+
artifacts layout 將 bin/obj 放到 repo root 的 .artifacts，而不是 Assets/submodule 內。
既有本機 bin/obj 不會自動消失；需要先確認是產生物再移除，檢查器只報告、不刪除。

例如從 repo root 建置 transport（需要 .NET 8 或更新 SDK）：

```shell
dotnet build Assets/CraftyRacoon/Module.Communication.JsonRpc.WebSocket/src/Module.Communication.JsonRpc.WebSocket.csproj -c Release -f netstandard2.1
```

此命令只建置，不自動安裝 DLL、不改 Unity manifest、不啟動網路。

## 靜態檢查與限制

```shell
python scripts/validate-unity-imports.py
python scripts/test_import_policy.py
```

檢查器掃描明確 assembly 邊界、高版本語法、SDK test 排除與核心 DLL 重複匯入。
其 8 個 fixture 自測已執行通過；這不是 C# 編譯、Unity 匯入或 transport integration 測試。
作者環境沒有 .NET SDK/Unity Editor，未執行 restore/build/dotnet test/Unity PlayMode。

遠端 repo 目前仍未提供 ProjectSettings/ProjectVersion.txt、Packages/manifest.json 或 scene。
本次沒有猜測或覆寫使用者本機的 Unity 版本/專案設定，亦未加入 Playwright、Verification
或 Monkey Framework。完整 Host 組裝與 Unity/IL2CPP 相容性仍需實際驗證。
