# 上下文交接摘要

更新日期：2026-08-12

本文件是新对话和上下文压缩后的首读入口，只保留当前决策、验证状态、阻塞和下一动作。历史过程见 `progress.md`、`docs/CHANGELOG.md` 与对应设计／资产记录。

## P0｜当前用户决策与权威边界

- 第一阶段正式内容为 9 杯：冰美式、金汤力、古典鸡尾酒、莫吉托、玛格丽特、莫斯科骡子、代基里、马天尼、威士忌酸。
- 九杯配方正文是库存与用量的最高优先级来源；库存缺项按正文补齐，重复资产去重，威士忌酸不增加正文未写的橙皮装饰。
- 三种双头量酒器容量按小／大端锁定为 `10/20 ml`、`15/30 ml`、`25/50 ml`。
- 数量不明的“少量”“未注明”保持原文；滴、勺不擅自换算为毫升或克。
- 9 杯均是正式目录内容，但可玩状态必须区分：冰美式 `Partial`，其余 8 杯 `CatalogOnly`。不得把目录登记误报为九杯工序完成。
- 用户要求后续任务完成并验证后再集中交付，不再逐项询问实现细节方案；仍不得自行发明容差、概率／恢复曲线、奖励、成本、顾客内容或最终美术。
- 用户本地 `export_presets.cfg` 修改必须继续保留，不得暂存或提交。

## P1｜本轮实现状态

- `data/ingredients/stage1_ingredients.tres` 登记 26 个成分形态，去重后对应 23 个采购来源族。
- `data/recipes/stage1_recipe_catalog.tres` 登记 9 杯正式配方；配方正文投料与当前成品杯评价目标分离，避免转化原料被误判为最终杯中成分。
- 冰美式稳定 ID 已从 `prototype_iced_americano` 切换为 `iced_americano`；schema 1 旧快照在反序列化时兼容迁移。
- 咖啡豆来源每勺 9g，两勺合计 18g；小号量酒器大端五次加水合计 100ml。直接液体倒入按载具实际量转移，不再固定输出模板量。
- 当前冰夹／整冰块只作为 150g 碎冰系统的运行桥接；正式碎冰铲与碎冰资源尚未接入，不能宣称冰美式完整正式流程已验收。
- `docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md` 第 8 节登记 42 个稳定资产 ID／40 个模型族：5 个正式复用、4 个现有灰盒待替换、33 个新增。另有 `bar_sink`、`waste_bin` 两项基础站点灰盒。
- 本轮没有生成模型、导出、截图，也没有修改 `assets/asset_manifest.json` 或任何 placeholder 状态。
- 本轮最终一键回归退出码 `0`：资产 `21/0`，Debug／Release `0` 警告／`0` 错误，领域测试 `30/30`，正式目录与全部 Godot 集成场景 PASS。

## P1｜既有正式环境与资产状态

- 正式酒吧六模块已经完成并获用户检查点 2 批准；默认 `Main` 事务式加载整套正式环境，任一模块失败时整体回退完整灰盒。
- 阶段 1／2 九项工具资产已接入。第一阶段配方可直接复用 `highball_glass`、三种 `jigger_*`、`ice_tongs`；`mortar`／`pestle` 可用于莫吉托捣压。
- GLB 只负责视觉；碰撞、灯光、稳定 ID、柜体状态和现实／眼镜共享玩法状态继续由 Godot 权威层拥有。
- 公开仓库使用 MIT License；`.gitattributes` 固定 GLB／`.glb.import` 属性并保持工作树无导入噪声。

## P1｜仍未完成

- 其余 8 杯的全部可玩工序，以及冰美式正式碎冰、磨豆、萃取设备和最终流程验收。
- 建模计划 3A–3F 的轮廓、GLB、Godot 包装、交互和 Forward+ 两道检查点。
- 正式容差、概率／恢复曲线、卫生影响、评价、奖励、成本、顾客内容和最终美术。
- 基于当前正式酒吧生成测试包并进行真实键鼠连续游玩验收。
- 正式磁盘存档产品层、手册动画、顾客 AI、多顾客／随机订单／经营经济等后续功能。

## 接手入口

- 当前状态：`docs/PROJECT_STATUS.md`
- 路线图：`docs/ROADMAP.md`
- 正式配方设计：`docs/superpowers/specs/2026-08-12-stage1-formal-recipes-and-assets-design.md`
- 实施计划：`docs/superpowers/plans/2026-08-12-stage1-formal-recipes-and-assets.md`
- 建模计划：`docs/CORE_INTERACTION_ASSET_MODELING_PLAN.md`
- 详细进展：`progress.md`
- 一键验证：`tools/run_verification.ps1`
