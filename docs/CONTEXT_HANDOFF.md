# 上下文交接摘要

更新日期：2026-08-12

本文件是新对话和上下文压缩后的首读入口，只保留当前决策、验证状态、阻塞和下一动作。历史过程见 `progress.md`、`docs/CHANGELOG.md` 与对应设计／资产记录。

## P0｜当前权威状态

- 正式酒吧环境批次已经完成并获用户检查点 2 批准。六项环境资产 `bar_architecture`、`bar_counter`、`bar_backbar`、`bar_furniture`、`bar_lighting`、`bar_wear_overlays` 在 JSON 与 Godot Resource 清单中均为非占位。
- 默认 `Main` 使用完整正式环境。`BarEnvironmentVisualLoader` 以事务方式加载六模块；任一模块失败时清理部分实例并整体回退完整可玩灰盒，不混用半套正式／半套灰盒。
- GLB 只负责视觉。碰撞、14 盏逻辑灯、稳定 ID、柜门／抽屉状态和现实／眼镜共享玩法状态继续由 Godot 权威层拥有。
- 用户已批准现实暖色与眼镜冷色的最终灯光候选。24 张 `1920×1080` Vulkan Forward+ 生产集成图已人工检查；证据位于 ignored `artifacts/bar-interior-production-integration/`。
- 公开 GitHub 仓库使用 `main`，已补充 README 和标准 MIT License；版权行是 `2026 Glasses Bar contributors`。
- 仓库级 `.gitattributes` 将 `.glb` 声明为二进制、将 `.glb.import` 固定为 LF，已消除 15 份无内容差异的导入元数据工作树噪声。
- 用户本地 `export_presets.cfg` 修改必须继续保留，不得暂存或提交。

## P1｜最近验证证据

- `tools/run_verification.ps1` 最近一次退出码为 `0`。
- 资产验证：`assets=21 errors=0`，六项环境 GLB 全部 `OK`。
- C#：Debug/Release 均为 `0` 警告、`0` 错误；领域测试 `28/28`。
- Godot：布局、储物、运行时几何、正式表现、生产资产／强制回退、冒烟、Stage 1/2、输入和完整流程场景全部 PASS。
- 建模批次严格完成门禁：`phase=complete errors=0`。

## P1｜当前未完成范围

- 六项独立资产仍为明确灰盒：`ice_bucket`、`bar_sink`、`cutting_board`、`water_kettle`、`coffee_beans`、`waste_bin`。
- 正式冰美式配方、容量、容差、概率／恢复曲线、评价、奖励与成本仍需用户／策划批准；现有相关数值必须继续视为开发占位。
- 仍需基于当前正式酒吧生成新的测试包并进行真实键鼠连续游玩验收；旧 2026-07-23 灰盒包不代表当前空间和灯光。
- 正式存档产品层、手册拿取／阅读动画、顾客 AI、后台区域、多顾客／随机订单／经营经济等属于后续范围，尚未授权为当前任务。

## P0/P1 边界

- 不自行发明正式配方、平衡、顾客故事或最终美术。
- 不直接修改导入 GLB；所有正式资产继续使用稳定 ID 与手写 Godot 包装。
- 现实／眼镜世界只承载表现，权威玩法状态不得复制到两个世界。
- 视觉改动必须运行真实 Forward+ 并检查实际画面。
- 当前 P0 收尾已完成；下一开发任务必须由用户从剩余资产、正式玩法内容或其他明确范围中选择。

## 接手入口

- 当前状态：`docs/PROJECT_STATUS.md`
- 路线图：`docs/ROADMAP.md`
- 变更记录：`docs/CHANGELOG.md`
- 详细进展：`progress.md`
- 技术设计：`docs/TECHNICAL_DESIGN.md`
- 正式酒吧批次：`docs/assets/BAR_ARCHITECTURE_BATCH_RECORD.md`
- 一键验证：`tools/run_verification.ps1`
