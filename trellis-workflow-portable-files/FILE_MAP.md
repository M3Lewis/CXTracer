# File Map

把 `trellis-workflow-portable-files/files/` 下的文件覆盖到目标项目根目录的同名相对路径。

| Portable file | Target path in another Trellis project | Purpose |
|---|---|---|
| `files/.trellis/workflow.md` | `.trellis/workflow.md` | Trellis workflow 主文件；包含 Phase 2、workflow-state breadcrumb、routing、Verification Matrix 和 `trellis-explore` 规则。 |
| `files/.trellis/project-profile.md` | `.trellis/project-profile.md` | 项目技术栈 profile；默认 C# / .NET / Avalonia 验证命令和测试分层。 |
| `files/.agents/skills/trellis-explore/SKILL.md` | `.agents/skills/trellis-explore/SKILL.md` | 新增 thinker skill；失败、歧义、用户纠正或重复调试时写 `research/implicit-rules.md`。 |
| `files/.agents/skills/trellis-before-dev/SKILL.md` | `.agents/skills/trellis-before-dev/SKILL.md` | 写代码前读取 task artifacts、project profile、implicit rules 和相关 specs。 |
| `files/.agents/skills/trellis-check/SKILL.md` | `.agents/skills/trellis-check/SKILL.md` | 质量检查规则；要求 stack-aware validation 和 Verification Matrix。 |
| `files/.agents/skills/trellis-continue/SKILL.md` | `.agents/skills/trellis-continue/SKILL.md` | 继续任务时把 check 失败/目标纠正/重复失败路由到 `trellis-explore`。 |
| `files/.agents/skills/trellis-start/SKILL.md` | `.agents/skills/trellis-start/SKILL.md` | 新会话 skill routing；区分事中 `trellis-explore` 和事后 `trellis-break-loop`。 |
| `files/.agents/skills/trellis-update-spec/SKILL.md` | `.agents/skills/trellis-update-spec/SKILL.md` | spec 沉淀规则；`implicit-rules.md` 只作为候选输入，必须过 curator gate。 |
| `files/.codex/agents/trellis-check.toml` | `.codex/agents/trellis-check.toml` | Codex check sub-agent 入口；要求 project profile、implicit rules 和 Verification Matrix。 |
| `files/.codex/agents/trellis-implement.toml` | `.codex/agents/trellis-implement.toml` | Codex implement sub-agent 入口；要求 test plan、合适测试和 project-specific validation。 |

## Not Included As Portable Overrides

| Local file | Reason |
|---|---|
| `.trellis/tasks/06-04-improve-trellis-workflow-verification/*` | 这是本项目本次改造的任务记录和验证证据，不应该覆盖到其他项目。 |
| `workflow-change.md` | 这是源需求说明，不是 Trellis runtime 文件。可以作为参考文档复制，但不需要覆盖目标项目。 |
| Native AOT 相关源码和任务文件 | 与本轮 Trellis workflow portable package 无关。 |

## Minimal Set

如果目标项目不是 Codex 项目，最小覆盖集是：

```text
.trellis/workflow.md
.trellis/project-profile.md
.agents/skills/trellis-explore/SKILL.md
.agents/skills/trellis-before-dev/SKILL.md
.agents/skills/trellis-check/SKILL.md
.agents/skills/trellis-continue/SKILL.md
.agents/skills/trellis-start/SKILL.md
.agents/skills/trellis-update-spec/SKILL.md
```

如果目标项目使用 Codex，再加：

```text
.codex/agents/trellis-check.toml
.codex/agents/trellis-implement.toml
```
