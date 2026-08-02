# Z3/H3 吧台细节修订视觉证据（2026-08-03）

状态：四项用户反馈已实现并通过内部视觉检查；用户已授权完成后直接进入既定生产建模下一阶段。

## 捕获环境与范围

- Godot `4.7.1.stable.mono`，Vulkan Forward+，NVIDIA GeForce RTX 5070 Laptop GPU。
- 输出：`artifacts/visual_review_bar_graybox_z3_h3_detail_fix/`，20 张 PNG；PNG 头与捕获脚本均确认全部为 `1920×1080`。
- `01–16` 复核既有全景、玩家眼高、Z3 分区、收纳、瓶架、客区与双世界灯光；`17–20` 专门复核客侧外挑、抽屉／柜架净空、后下柜推拉门和水槽裸露管线。
- 已逐张人工检查：未见房间外溢出、旧柜体残留、抽屉与静态柜架穿插、推拉门侵入相邻柜或水槽下封闭柜体。推拉门开放侧的原料可见；客侧台面保持连续。
- Movie Maker 的附带 AVI 写入报告 MJPEG 文件打开错误，但脚本直接保存的 20 张 PNG 完整生成并输出 `BAR_PRODUCTION_VISUAL_CAPTURE_PASS`；本记录只把 PNG 作为证据。

## SHA-256

```text
01_overhead_9m10_span.png              e00a103cbf655625af36a7c9d6529f2584a40b2cac836f2d8773a7222d2fe9d9
02_player_eye_customer_view.png        8c572aff22b13a2e0c29e7dc6ec02cb759bace3721e02d29637b1192d82fbf62
03_west_manual_only.png                241259bf9707aef0a581f5ce2aac6689622a87f711e345460f6e3c054420de94
04_east_waste_and_gate.png             e7526a388a6ab799690965ec5b64c15c7304779b96dd477a13bce99912f3a598
05_east_sink_open_underbay.png         af9788fde1310df84cd50ee4c820ed48a9b55bca1ab44ac069d39bb0af0f37af
06_west_chamfer_close.png              d113837dfdad17c1682a5c44d468f9548a078da489c6a2b2a2336ce39db2ee26
07_east_chamfer_close.png              e1aa22cde7e59d285287c469eb6b211daf698fdc10931c4d1e52262984b309b7
08_all_front_storage_closed.png        9606781c4a74296de9510a946a1794f16560c546f269a7269678e15cc3f657ef
09_tool_storage_open.png               bb337c4283b02e8aa6cf78c9b51210921a59849e9a7fb9562d2fd590b025fcb2
10_ice_drawer_fully_open.png           55001506e9cf841fc6949ca23bbfb381aeba6bd25790acc6a8b520b69059c822
11_five_bay_empty_rack_front.png       51f217601c17c80e897dfbf5724955ee46c39085fce3993fa3130e5bdbd416f5
12_coffee_kettle_cabinets_open.png     b573a6a3def8d0c0cf9bd658ba994cd43ce0ade70bf27a2d8db2cde449733c7a
13_customer_chairs_pulled.png          f7314aa92b37c45d1c85524c515219bbd52cf29153e8fc46a127f06c368edf57
14_reality_lighting.png                a435f6ae307c41866832fb306d209b0fec37318530a41a66a9be2a40c86b02e8
15_glasses_lighting.png                abe25f4f1784609e1b747cab7a867a9751100425ea75c00a95b20e69279ce05a
16_runtime_aabb_overview.png           f1414df6e43d6a726226337222702d10fdfa06fe7f29dc685fb3b96435aa3c40
17_guest_counter_extension_close.png   a608736d62facfd26100624ac9e444135fded5a182d9e27cd08ca8cbb9ae9cb2
18_front_drawer_carcass_clearance.png  360b3f2ff09c9f0878bfa96b84b11fefa2eced522fa084ad2572b0a193104dd1
19_rear_sliding_door_open.png          b0cbd4baf41e63b26ce9a929eee306eeaebca45fff4d68bcf5015eecf013b59c
20_sink_exposed_plumbing_close.png     6c6ffde9638660fe26b082e95744b4b2a39c6ec821070bccfdabe50b27045b91
```

## 自动验证结论

- 资产 `16/0`；领域测试 `28/28`；Debug/Release `0` 警告、`0` 错误。
- `BAR_PRODUCTION_LAYOUT_CONTRACT_PASS`、`BAR_STORAGE_INTEGRATION_PASS`、`BAR_RUNTIME_GEOMETRY_PASS`、`SMOKE_TESTS_PASS`、`STAGE1_ASSET_INTEGRATION_PASS`、`STAGE2_ASSET_INTEGRATION_PASS`、`INPUT_INTEGRATION_PASS`、`FLOW_INTEGRATION_PASS` 全部通过。
- 下一动作：执行生产模型计划 Task 6，再按建模 Skill 推进 Task 7 至中性建筑轮廓强制审阅点。
