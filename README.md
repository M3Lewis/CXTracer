# Codex Lens

Windows-first Codex CLI session reader.

目标：不接管、不启动、不写入 Codex CLI，只读读取本机 `~/.codex/sessions/**/*.jsonl`，把 Codex 的可读回复和命令执行轨迹分成左右两栏。

## 技术栈

- .NET 8
- Avalonia 12.0.4
- SukiUI 7.0.1
- CommunityToolkit.Mvvm 8.4.0

## 功能

- 扫描 `%USERPROFILE%\.codex\sessions\**\*.jsonl`
- UI 手动切换历史 session
- 选中 session 后实时 tail 新增 JSONL 行
- 左栏 Conversation：只显示 user / assistant / final answer
- 右栏 Execution：command / stdout / stderr / exit / diff / tool / error
- 搜索当前 session
- 过滤：All / Conversation / Commands / Errors / Diffs / Final / Tools / Raw
- 多 Codex 并行时默认 Pin selected，不自动抢当前 session
- 严格不修改 `.codex` 文件，不创建索引库，不上传任何数据

## 运行

```powershell
dotnet restore .\CodexLens.sln
dotnet run --project .\src\CodexLens\CodexLens.csproj
```

## 发布 Windows 单文件

```powershell
dotnet publish .\src\CodexLens\CodexLens.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

输出目录大致在：

```text
src\CodexLens\bin\Release\net8.0\win-x64\publish
```

## WSL / Zed 注意

如果你在 Zed 里使用的是 WSL shell，Codex session 可能不在 `C:\Users\你\.codex\sessions`，而是在类似：

```text
\\wsl.localhost\Ubuntu\home\你的Linux用户名\.codex\sessions
```

把这个 UNC 路径粘贴进顶部 Sessions root，然后点击 Refresh。

## 只读边界

应用只执行：

- Directory.EnumerateFiles
- FileSystemWatcher
- FileStream(FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)

应用不会：

- 启动 Codex CLI
- 向 Codex CLI stdin 写入内容
- 修改 `~/.codex` 下的任何文件
- 创建 transcript 索引
- 上传数据

## 已知限制

Codex transcript JSONL 不是公开稳定 API，所以解析器采用宽松启发式：识别常见字段如 `role`、`type`、`content`、`text`、`command`、`stdout`、`stderr`、`diff`、`patch`、`tool_call` 等。遇到识别不了的事件，会放进 Raw events。`reasoning` / `thinking` / `plan` 也默认不进入左栏，只有打开 Raw events 时才会看到。

如果 Avalonia 和 SukiUI 后续升级时发生 API 兼容问题，优先保持 Avalonia 与 SukiUI 的主版本匹配，并先跑 `dotnet restore` / `dotnet build` 验证。

## 测试样例

`samples/sample-rollout.jsonl` 可以复制到一个临时 sessions 子目录里做 UI 验证。
