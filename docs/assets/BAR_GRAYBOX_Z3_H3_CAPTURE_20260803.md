# Z3/H3 完整酒吧灰盒视觉证据（2026-08-03）

状态：实现与内部验证完成，等待用户检查；不代表用户已批准灰盒，也不授权正式环境 GLB／Blender 阶段。

## 捕获环境

- Godot `4.7.1.stable.mono`，Vulkan Forward+。
- GPU：NVIDIA GeForce RTX 5070 Laptop GPU。
- 输出：`artifacts/visual_review_bar_graybox_z3_h3/`，16 张 PNG，捕获脚本逐帧强制 `1920×1080`。
- `01–13` 与 `16` 使用仅存在于捕获场景的中性技术补光；`14–15` 不使用技术补光，保留实际现实／眼镜世界灯光。
- 已逐张人工检查总跨度、玩家眼高、Z3 东西侧收敛、水槽下净空、两处连续切角、全关／打开收纳、五湾空瓶架、拉出客椅、双世界灯光和运行时诊断总览。

## SHA-256

```text
01_overhead_9m10_span.png              f007c6634958cf943eff6f02dfa34d1109638c8ca9e52dff3b4b6393a99e7672
02_player_eye_customer_view.png        4d244ad4d4fbb4b57a875dd9ddadc245f4f7d39d0e6b54b52f698bf9c6b17926
03_west_manual_only.png                d4e7aabd2638ef9024d717e8ec0195480b34f84d9621790a0c26cbaf8a19f312
04_east_waste_and_gate.png             074df7861b44a52ff3cdc29f8d60c64c92753c13708492e39b227dff8220bf3e
05_east_sink_open_underbay.png         b8de23d2e6e031e2364aec585ac5de7a9b4ce66415b9f13d32b55fbd75eb69cc
06_west_chamfer_close.png              58ce21d119977567f98a02ede44601cb665b4a5f8725582f297b58fab48affc6
07_east_chamfer_close.png              543f18eb13325955bfb0fecc18c95c3f4bdd080ece0edba01e7856632dc122f8
08_all_front_storage_closed.png        6582ece1b2faef3c0820702ade0a909499932f5f085c34616a7021a76bef8862
09_tool_storage_open.png               431f787db012182bfb35e56e148d32034f539fb03aecee046a4f0cfdd2c05713
10_ice_drawer_fully_open.png           0bdd2e8cd5e66e246a3db574463606e09d70daacc0a90d7c492c5701032efffe
11_five_bay_empty_rack_front.png       02894966050d78d99e7f282d7e714a87a214d2c55cf39c485e2c2fd9bcb8e894
12_coffee_kettle_cabinets_open.png     970d19638b13972db31b396d879b4ba3812f0adca3c1e9ecdab9a8896df8bb7e
13_customer_chairs_pulled.png          bc09885b104ddeb7c6a0c49b942c83774ae964061f9655a534e609ca93896225
14_reality_lighting.png                5e509e8f5de81e37b42114c60fccb3774602acf0410498443d9f0eb73a285ac5
15_glasses_lighting.png                906fedd4b4728da3004a63b1f34fb29e70c10c0dc84de34c8e6d631a112ed2a7
16_runtime_aabb_overview.png           a5d99533542d4cf513ed60abc58dc34a72cb15067922d27d95e8481f0e77e18a
```

## 验证结论

- 捕获输出：`BAR_PRODUCTION_VISUAL_CAPTURE_PASS`。
- 最终一键回归：资产 16/0、领域测试 28/28、Debug/Release 0 警告/0 错误；布局、储物、运行时几何、冒烟、Stage 1/2、输入与流程场景全部 PASS。
- 当前门禁：等待用户检查这些图；用户批准前不开始正式环境 GLB／Blender 几何。
