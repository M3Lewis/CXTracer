# CXTracer

Windows 优先的 Codex CLI 会话极速只读阅读器。

目标：**严格不接管、不启动、不修改** Codex CLI。通过后台扫描和监听本机 `~/.codex/sessions/**/*.jsonl`，将 Codex 会话中的可读回复和执行轨迹以优雅的左右两栏（Conversation & Execution）实时分栏呈现，提供流畅的审查体验。

---

## 技术栈

- **.NET 8** (采用最新 C# 语法与 Native AOT 支持)
- **Avalonia 12.0.4** (跨平台 UI 框架)
- **SukiUI 7.0.1** (基于橙色主题 `Orange` 与 `zh-CN` 区域设置的精美扁平化设计)
- **CommunityToolkit.Mvvm 8.4.0** (基于 C# Source Generator 的高效 MVVM 模型)

---

## 核心特性与实现机制

根据 `src` 代码的架构设计，本项目实现了以下核心功能与技术设计：

### 1. 左右双栏分栏 & 智能事件分类
Codex 的 JSONL 会话并非公开的稳定 API，因此 `CodexEventParser` 采用**扁平化展平（Flatten）**与**宽口径启发式算法**对 JSON 行进行字段扫描和正则分类。
* **左栏 (Conversation)**：展示 `User`、`Assistant`、`Final`。仅保留用户提示与 Codex 最终的文本回复。
* **右栏 (Execution)**：展示 `Command`、`CommandOutput`、`Diff`、`Tool`、`Error`。呈现指令执行、工具调用、文件变更补丁（Diff）以及错误信息。
* **底层折叠栏 (Raw events)**：推理（`Reasoning`/`Thought`）与更新计划（`Plan`）默认不进入左右栏，仅在底部的 `Raw events` 可折叠区域展示，以保持主界面整洁。

### 2. 双栏同步导航 (Synchronized Navigation)
* **按时间/行号对齐**：开启同步导航后，当用户在任一栏中上下导航（通过 UI 按钮或 Arrow 键）时，阅读器会基于事件的 `Timestamp` 或 `LineNumber` 自动定位并同步对齐另一栏的对应伴随事件（Companion Event），实现视觉上的阅读同步。
* **快捷键切换与定制**：支持捕获并绑定自定义快捷键（如 `Ctrl + Shift + S` 等），在 Settings 窗口中直接按下按键即可录制，并一键切换同步状态。
* **本地配置持久化**：用户配置（如同步导航开关和快捷键绑定）会自动序列化为 `settings.json`，持久化存储在 `%LOCALAPPDATA%\CXTracer\` 下，且完全兼容 **Native AOT** 编译（使用 Source Generator 构建 of `AppJsonContext` 避免反射）。

### 3. 高效实时 Live Tail 追加
* **文件变更监听**：使用 `FileSystemWatcher` 监测指定目录，一旦有 session 更新，通过 `SessionWatcher` 抛出防抖（Debounced）事件触发 UI 刷新。
* **流式增量读取**：`SessionReader` 在读取更新时，**不会重新加载整份文件**。它会记录上次读取的字节偏移量（`Offset`），直接 `Seek` 并在底层分配字节缓冲区读取新增的 UTF-8 数据。同时，它支持缓存不完整行的尾部（`Pending`），在下一次追加数据时无缝拼接，极大地降低了 I/O 开销。
* **会话锁定 (Pin Selected)**：如果存在多个 Codex 实例并行写入，支持在顶部勾选 "Pin selected"，当其他会话文件发生变更时，不会自动抢夺当前阅读器的选中焦点，始终锁定在当前查看的会话。

### 4. 会话详情预览与路径复制
* **丰富元数据**：Session 列表卡片会动态显示最近更新时间、提取的第一行 Prompt 作为 Title、从 `cwd` 推断的项目名称（Project Hint），并根据活跃程度标记为 `LIVE` / `Active` / `History` 状态。
* **路径 hover 与复制**：在主界面顶部，鼠标悬停在当前会话的路径上，可以通过 Tooltip 悬浮预览完整路径；点击/右击路径标签可直接将完整文件路径复制到系统剪贴板，并触发 SukiUI Toast 提示。

### 5. 消息详情弹窗浮层 (Message Detail Popup)
* **长日志快速阅读**：为了解决长日志（如 Assistant 完整回复、命令行长输出或复杂 Tool 荷载）在窄列分栏下阅读吃力的问题，CXTracer 支持**单击任意消息卡片**打开详情浮层。
* **暗色遮罩与 Raw JSON**：浮层采用半透明暗色遮罩背景，卡片主体采用对应角色的主题背景。底部配有可折叠的 **Raw JSON** 展开器，方便对原始 JSON 报文进行精确排查。按 `Esc` 键、点击空白区域或点击右上角关闭按钮即可退出浮层。

### 6. 严格的只读与安全边界
应用仅使用只读性质的系统 API，严格限制自身边界：
* 仅调用 `Directory.EnumerateFiles` 与 `FileSystemWatcher`。
* 读文件流时强制使用 `FileShare.ReadWrite | FileShare.Delete` 共享模式，避免影响 Codex 进程的写入与清理。
* **不修改** `~/.codex` 下的任何文件，**不创建**本地索引数据库，**不进行**任何形式的网络数据上传。

---

## 运行与编译

### 开发运行

```powershell
dotnet restore .\CXTracer.sln
dotnet run --project .\src\CXTracer\CXTracer.csproj
```

### 发布 Windows 单文件 (Native AOT)

项目配置了完整的 Native AOT 和 Trim 裁剪支持（配置于 `CXTracer.csproj`）。可通过以下命令发布不依赖 .NET 运行时的极小、启动即时、单 exe 运行文件：

```powershell
dotnet publish .\src\CXTracer\CXTracer.csproj -c Release -r win-x64 --self-contained true
```

编译输出目录位于：
```text
src\CXTracer\bin\Release\net8.0\win-x64\publish
```

---

## 搜索与过滤

* **双重独立检索**：
  * **会话检索（左侧）**：侧边栏上方的搜索框专职用于过滤 Session 列表，支持按会话标题、副标题或文件路径进行实时过滤。
  * **日志检索（右侧）**：右侧顶栏的 ComboBox 左侧新增了内容过滤框，专职针对当前会话中的日志事件内容进行关键字查找过滤。
* **分类视图切换**：提供 `All` / `Conversation` / `Commands` / `Errors` / `Diffs` / `Final` / `Tools` / `Raw` 快速过滤。

---

## WSL 与跨环境支持

如果你在 WSL (如 Ubuntu) 环境下运行 Codex，而本阅读器运行在 Windows 下，可将 WSL 的共享网络路径粘贴至顶部的 **Sessions root**。

例如：
```text
\\wsl.localhost\Ubuntu\home\username\.codex\sessions
```
点击 **Refresh** 后，应用将正常加载并进行文件监听及 Live 增量更新。

---

## 运行示例数据

可以使用 `samples/sample-rollout.jsonl` 作为参考测试数据，将其复制到 sessions 目录中来检验解析和 UI 分栏渲染效果。
