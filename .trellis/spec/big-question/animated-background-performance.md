# Animated Background Performance Cost

## Symptom

Idle CPU or GPU usage stays higher than expected, especially on dashboards or tools that should be mostly static.

## Root Cause

SukiUI background animation continuously redraws the visual tree. The official background documentation warns that enabling animation can materially increase CPU and GPU usage.

## Guidance

- use `Flat` background style by default for dense tools
- enable background animation only when it materially improves the experience
- avoid animated backgrounds on data-heavy or always-open screens

## Prevention

- treat animated backgrounds as an explicit design choice
- profile before enabling animation globally
- prefer static backgrounds for long-running productivity workflows
