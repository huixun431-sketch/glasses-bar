# 变更记录

## 2026-07-19

- 修正眼镜世界移动规则：戴镜后人物仍可移动，但顾客、原材料与制作台交互继续禁用；输入集成测试新增实际位移和交互门控覆盖。
- 核对当前环境，确认 Godot 4.7.1-stable .NET 便携版位于 `D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\`，系统已安装 .NET 9 SDK 和 .NET 8 运行时。
- 从 Godot 官方 4.7.1-stable .NET 模板归档按 ZIP Range 提取并安装 Windows x86_64 Debug/Release 模板及控制台包装；逐项通过 CRC、尺寸和 `4.7.1.stable.mono` 版本校验。
- 首次导出发现 Godot 在缺少解决方案时仍返回退出码 0；新增 `GlassesBar.sln`，并将 `tests/*` 从分发资源中排除。此后导出验证同时检查文本日志中的 `ERROR`/异常，不再只依赖退出码。
- 分别生成独立的 Windows Debug/Release 分发目录，避免两个配置共用并覆盖同一个 .NET 数据目录；两个包均通过实际无头启动验证。
- 重新通过资产验证、Debug/Release 构建、领域测试 4/4、Godot 导入、主场景、冒烟、输入链路与流程集成测试。

## 2026-07-18

- 初始化 Godot 4.7.1 .NET/C# 项目。
- 添加现实/眼镜双表现世界、第一人称控制和近视后处理。
- 添加单杯冰美式教学流程、参数化液体、持续操作、丢弃重做、评价与日结。
- 添加灰盒资产、数据资源、资产清单与 GLB 验证工具。
- 添加项目级开发 Skill、技术文档和测试入口。
- 使用临时 .NET 8 SDK 与 Godot 4.7.1 .NET 便携版完成 Debug/Release 构建、编辑器导入、无头冒烟、输入链路和完整流程测试。
- 修复 Godot 测试程序集排除导致的假通过，并要求测试日志出现明确 PASS 标记。
- 使用 Forward+ Movie Maker 渲染并检查现实/眼镜两个表现层。
- 添加 Windows Desktop 导出预设；可分发 EXE 等待 Godot 导出模板。
- 添加带 P0-P3 优先级的主动上下文压缩与交接规范，并建立 `docs/CONTEXT_HANDOFF.md` 首读入口。
- 新增根目录 `progress.md`，整理已完成事项、关键决策与未完成待办，供新对话直接接手。
