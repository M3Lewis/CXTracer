# Trellis Workflow Portable Files

这个目录保存了本轮 Trellis workflow 改造后的可移植文件，用于覆盖到其他已经初始化 Trellis 的项目。

## 目录结构

```text
trellis-workflow-portable-files/
├── README.md
├── FILE_MAP.md
└── files/
    ├── .trellis/
    ├── .agents/
    └── .codex/
```

`files/` 目录下的路径已经按目标项目的相对路径排列。把 `files/` 里的内容复制到另一个项目根目录时，对应路径就是覆盖位置。

## 覆盖前检查

在目标项目根目录先检查：

```powershell
git status --short
Test-Path .trellis\workflow.md
```

如果目标项目这些文件有未提交修改，先备份或提交。不要直接覆盖未确认的本地改动。

## 推荐覆盖方式

在目标项目根目录执行时，把 `<SOURCE>` 替换成这个项目里的 `trellis-workflow-portable-files\files` 绝对路径。

```powershell
$source = "<SOURCE>"
Copy-Item -LiteralPath "$source\.trellis\workflow.md" -Destination ".trellis\workflow.md" -Force
Copy-Item -LiteralPath "$source\.trellis\project-profile.md" -Destination ".trellis\project-profile.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-explore\SKILL.md" -Destination ".agents\skills\trellis-explore\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-before-dev\SKILL.md" -Destination ".agents\skills\trellis-before-dev\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-check\SKILL.md" -Destination ".agents\skills\trellis-check\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-continue\SKILL.md" -Destination ".agents\skills\trellis-continue\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-start\SKILL.md" -Destination ".agents\skills\trellis-start\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.agents\skills\trellis-update-spec\SKILL.md" -Destination ".agents\skills\trellis-update-spec\SKILL.md" -Force
Copy-Item -LiteralPath "$source\.codex\agents\trellis-check.toml" -Destination ".codex\agents\trellis-check.toml" -Force
Copy-Item -LiteralPath "$source\.codex\agents\trellis-implement.toml" -Destination ".codex\agents\trellis-implement.toml" -Force
```

如果目标项目没有某个目录，先创建对应目录，例如：

```powershell
New-Item -ItemType Directory -Force -Path ".agents\skills\trellis-explore"
New-Item -ItemType Directory -Force -Path ".codex\agents"
```

## 覆盖后验证

在目标项目根目录执行：

```powershell
python .\.trellis\scripts\get_context.py --mode phase --step 2.2 --platform codex
rg "Verification Matrix|trellis-explore|project-profile|implicit-rules" .trellis .agents .codex
git diff --check -- .trellis .agents .codex
```

期望结果：

- Phase 2.2 输出包含 project-specific validation 和 Verification Matrix。
- `rg` 能找到 `trellis-explore`、`.trellis/project-profile.md`、`implicit-rules.md` 相关规则。
- `git diff --check` 没有 whitespace error。

## 注意

- 这些文件是本地 Trellis 配置和 Codex 平台入口，不是 Trellis 上游源码。
- `.trellis/project-profile.md` 默认写的是 C# / .NET / Avalonia 项目。覆盖到非 Avalonia 项目后，应按目标项目技术栈修改该文件。
- `.codex/agents/*` 只影响 Codex。没有 Codex 配置的项目可以不复制 `.codex/` 下的文件。
- 本项目的 `.trellis/tasks/06-04-improve-trellis-workflow-verification/` 是任务记录，不建议覆盖到其他项目。
