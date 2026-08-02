# 完整酒吧灰盒 Z3/H3 重建 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将被打回的完整酒吧灰盒重建为 16×10×4.5 m 房间、9.10 m 连续多边形前吧、Z3 功能分区、H3 高度、封闭储物和五湾空瓶架，并以运行时几何合同与 16 张 Forward+ 图片交付用户检查。

**Architecture:** `BarLayoutDefinition` 继续是唯一尺寸、稳定 ID、储物分配和局部摆位来源；新增多边形棱柱与储物宿主记录，`GrayboxArchitectureBuilder` 从同一轮廓生成视觉网格和确定性碰撞，`CabinetBuilder` 创建柜门／抽屉及其内容宿主，`GameplaySceneComposer` 只按分配记录实例化玩法对象。玩法仍由 `NeutralGameplay` 唯一持有，现实／眼镜世界只切换表现；GLB 不参与本轮重建。

**Tech Stack:** Godot 4.7.1 Forward+、C#/.NET、GDScript-free Godot integration scenes、PowerShell verification、Git。

## Global Constraints

- 权威规格：`docs/superpowers/specs/2026-08-03-bar-graybox-rework-design.md`。
- 房间净尺寸固定为 `16.00 m × 10.00 m × 4.50 m`；前吧东西总长固定为 `9.10 m`。
- 玩家工作台／客侧台面高度固定为 `1.12 m / 1.38 m`；玩家眼高保持 `1.83 m`；上柜下沿／顶固定为 `2.65 m / 3.95 m`。
- 前后吧南北向操作通道为 `1.55 m`；顾客主／次通路最小为 `1.40 m / 0.90 m`。
- 西侧只保留操作手册；东侧只保留弃物区和员工门；洗手水槽位于前吧东端且下方无任何柜体、抽屉、门板、背板、托盘、碰撞或旧湿区节点。
- 四个前吧收纳湾净宽各 `1.40 m`，五道分隔各 `0.06 m`，交接落台 `1.00 m`，水槽湾 `1.50 m`，两处连续切角各 `0.35 m`。
- 后吧五湾各 `1.70 m`，两端各 `0.30 m`；空瓶架层板顶标高 `1.50 m / 2.10 m`，背板上沿 `2.55 m`。
- 每日开始所有储物关闭；工具、杯具、咖啡原料、水壶和冰桶只有所属储物打开后才可交互；保持一次只打开一个储物正面。
- 保留 `front_drawer_2_upper`、既有工具／站点 ID、手册锚点、现实可交互／眼镜只观察和唯一玩法状态。
- 删除旧独立倒角节点、旧连续瓶架、`MergedShelf0/1`、`BackLiquor0…13`、东侧旧湿区及水槽下方旧结构；不得隐藏、零缩放或移出房间。
- 不发明正式配方、容量、平衡、顾客故事、瓶装原料或最终美术；不进入正式环境 GLB／Blender 阶段。
- `export_presets.cfg` 属于用户未提交改动，任何提交都必须显式排除；不推送、不发布。

---

## File Map

- `scripts/world/BarLayoutDefinition.cs`：权威尺度、多边形轮廓、五湾结构、储物分配、局部包络和验证。
- `scripts/world/BarPolygonGeometry.cs`：新增；把 XZ 轮廓挤出为 `ArrayMesh`，并把三角剖分结果转换为非重叠静态碰撞棱柱。
- `scripts/world/GrayboxArchitectureBuilder.cs`：从权威轮廓／湾记录生成建筑、台体、瓶架和灰盒表现，移除旧叠加件。
- `scripts/world/CabinetBuilder.cs`：创建储物正面与内容宿主，建立储物 ID 到宿主节点映射并保证复位关闭。
- `scripts/world/GameplaySceneComposer.cs`：按 `ItemId → StorageId → LocalPlacement` 建工具／原料，不再把物品散布在台面世界坐标。
- `scripts/gameplay/CabinetInteractable.cs`：公开开合信号与储物访问状态。
- `scripts/gameplay/ToolInteractable.cs`、`scripts/gameplay/StationInteractable.cs`：绑定所属储物并在关闭时拒绝交互。
- `tests/godot/BarProductionLayoutContractTests.cs`：新 Z3/H3 纯布局合同与旧节点禁止列表。
- `tests/godot/BarStorageIntegrationTests.cs`、`.tscn`：新增；验证关门不可取、开门可取、互锁、局部宿主和每日复位。
- `tests/godot/BarRuntimeGeometryTests.cs`、`.tscn`：新增；验证运行时网格包络、净空和多状态碰撞。
- `tests/godot/FlowIntegrationTests.cs`、`InputIntegrationTests.cs`：更新既有稳定 ID 与交互流程断言。
- `tests/godot/BarProductionVisualCapture.cs`：改为 16 个 1920×1080 审批视角。
- `tools/run_verification.ps1`：接入新增储物和运行时几何测试。
- `docs/CONTEXT_HANDOFF.md`、`docs/PROJECT_STATUS.md`、`docs/CHANGELOG.md`、`progress.md`：记录实现、验证、截图与尚待用户批准的灰盒门。

---

### Task 1: 锁定被打回基线会失败的新布局合同

**Files:**
- Modify: `tests/godot/BarProductionLayoutContractTests.cs`
- Modify: `scripts/world/BarLayoutDefinition.cs`

**Interfaces:**
- Consumes: `BarLayoutDefinition.Prototype` 与现有布局记录。
- Produces: 新常量、`BarPolygonPrismLayout`、`BarStorageLayout`、`BarItemStorageLayout`、`FrontBodyFootprint`、`FrontPlayerTopFootprint`、`FrontGuestTopFootprint`、`BottleRackBays`。

- [x] **Step 1: 先只改测试，锁定新尺度与禁止旧结构**

在 `Run()` 中用明确断言替换旧 12×9×3.5、5.60 m、旧倒角和旧瓶架断言：

```csharp
Require(layout.RoomClearSize.IsEqualApprox(new Vector3(16f, 4.5f, 10f)),
    "room is 16 by 10 by 4.5 metres");
Require(Mathf.IsEqualApprox(BarLayoutDefinition.FrontOutlineWidth, 9.10f) &&
        Mathf.IsEqualApprox(BarLayoutDefinition.PlayerWorktopHeight, 1.12f) &&
        Mathf.IsEqualApprox(BarLayoutDefinition.FrontBarTopHeight, 1.38f) &&
        Mathf.IsEqualApprox(BarLayoutDefinition.OperationAisleClearWidth, 1.55f) &&
        Mathf.IsEqualApprox(BarLayoutDefinition.PlayerEyeHeight, 1.83f),
    "approved Z3/H3 scale is locked");
Require(layout.FrontBarInnerChamfers.Count == 0 &&
        layout.PlayerWorktopChamfers.Count == 0 &&
        layout.LiquorBottles.Count == 0,
    "obsolete overlay chamfers and placeholder bottles are absent");
Require(layout.BottleRackBays.Count == 5 &&
        layout.BottleRackBays.All(bay => bay.Shelves.Count == 2),
    "five aligned two-level empty bottle-rack bays are locked");
var requiredStorageIds = new[]
{
    "front_drawer_1_upper", "front_drawer_1_lower",
    "front_drawer_2_upper", "front_drawer_2_lower",
    "front_drawer_3_upper", "front_drawer_3_lower",
    "front_drawer_4_upper", "front_drawer_4_lower",
    "rear_lower_cabinet_1", "rear_lower_cabinet_2",
    "rear_lower_cabinet_3", "rear_lower_cabinet_4", "rear_lower_cabinet_5"
};
Require(requiredStorageIds.All(id => layout.Storages.Any(storage => storage.Id == id)) &&
        layout.ItemStorageAssignments.All(item => layout.Storages.Any(storage => storage.Id == item.StorageId)),
    "all required storage and item assignments resolve");
```

- [x] **Step 2: 运行单一合同场景并观察红灯**

Run:

```powershell
dotnet build GlassesBar.csproj --configuration Debug --nologo
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarProductionLayoutContractTests.tscn
```

Expected: build 或场景失败，错误直接指出缺少新布局成员或旧常量仍为 12/9/3.5、5.60、0.96/1.20。

- [x] **Step 3: 在布局定义中加入精确记录并替换旧常量**

在文件顶部新增：

```csharp
public readonly record struct BarPolygonPrismLayout(
    string Name, IReadOnlyList<Vector2> Footprint, float BottomY, float TopY);

public readonly record struct BarStorageLayout(
    string Id, BarCabinetLayout Front, Vector3 HostPosition, Vector3 HostSize,
    bool MovesWithFront);

public readonly record struct BarItemStorageLayout(
    string ItemId, string StorageId, Vector3 LocalPlacement);

public sealed class BarBottleRackBayLayout
{
    public required string Id { get; init; }
    public required BarBoxLayout Back { get; init; }
    public required IReadOnlyList<BarBoxLayout> Shelves { get; init; }
}
```

把核心常量改为：

```csharp
public const float RoomWidth = 16f;
public const float RoomDepth = 10f;
public const float RoomHeight = 4.5f;
public const float FrontOutlineWidth = 9.10f;
public const float FrontBarTopHeight = 1.38f;
public const float PlayerWorktopHeight = 1.12f;
public const float RearShelfTopHeight = 1.12f;
public const float PlayerEyeHeight = 1.83f;
public const float OperationAisleClearWidth = 1.55f;
public const float MainCustomerRouteClearWidth = 1.40f;
public const float SecondaryCustomerRouteClearWidth = 0.90f;
public const float UpperCabinetBottomHeight = 2.65f;
public const float UpperCabinetTopHeight = 3.95f;
public const float BottleRackLowerShelfHeight = 1.50f;
public const float BottleRackUpperShelfHeight = 2.10f;
public const float BottleRackBackTopHeight = 2.55f;
```

用一个可复算 helper 派生四个 1.40 m 湾、五个 0.06 m 分隔、1.00 m 落台、1.50 m 水槽湾和两个 0.35 m 切角；`Validate()` 必须验证复算总长为 9.10 m、五后吧湾各 1.70 m、两端各 0.30 m，以及所有储物／分配 ID 唯一。

- [x] **Step 4: 运行合同场景并修到绿灯**

Run: 与 Step 2 相同。

Expected: `BAR_PRODUCTION_LAYOUT_CONTRACT_PASS`，退出码 0。

- [x] **Step 5: 提交布局合同**

```powershell
git add -- scripts/world/BarLayoutDefinition.cs tests/godot/BarProductionLayoutContractTests.cs
git commit -m "test: lock Z3 H3 production bar layout"
```

---

### Task 2: 由同一多边形生成前吧视觉与碰撞

**Files:**
- Create: `scripts/world/BarPolygonGeometry.cs`
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `tests/godot/BarRuntimeGeometryTests.cs`
- Create: `tests/godot/BarRuntimeGeometryTests.tscn`

**Interfaces:**
- Consumes: `BarPolygonPrismLayout` 与 `BarLayoutDefinition` 三个权威轮廓。
- Produces: `BarPolygonGeometry.CreateMesh(BarPolygonPrismLayout)`、`CreateConvexCollisionChildren(StaticBody3D, BarPolygonPrismLayout)`、运行时包络检查场景。

- [x] **Step 1: 写运行时失败测试**

测试实例化 `Main.tscn` 后断言旧节点不存在、新多边形节点存在，且视觉和碰撞都使用布局轮廓：

```csharp
Require(!reality.HasNode("FrontBarWestChamfer") &&
        !reality.HasNode("FrontBarEastChamfer") &&
        !neutral.HasNode("FrontBarWestChamferCollider") &&
        !neutral.HasNode("FrontBarEastChamferCollider"),
    "obsolete chamfer overlays are hard-deleted");
Require(reality.HasNode("FrontBarBody") &&
        reality.HasNode("PlayerWorktop") &&
        reality.HasNode("GuestCounterTop") &&
        neutral.HasNode("FrontBarBodyCollider"),
    "authoritative polygon nodes exist");
```

- [x] **Step 2: 运行新场景并观察红灯**

Run:

```powershell
dotnet build GlassesBar.csproj --configuration Debug --nologo
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarRuntimeGeometryTests.tscn
```

Expected: 缺少新多边形节点或仍存在旧倒角节点而失败。

- [x] **Step 3: 实现确定性棱柱网格与碰撞**

`BarPolygonGeometry` 使用 `Geometry2D.TriangulatePolygon()`；顶／底面由三角索引生成，侧面按轮廓相邻点生成两个三角形。碰撞对每个三角形创建一个 `ConvexPolygonShape3D`，六个顶点为三角形在 `BottomY` 和 `TopY` 的上下副本；三角棱柱只共享边界，不体积重叠。任何少于三点、零高度、三角剖分失败或顺序退化都抛出带 `layout.Name` 的 `InvalidOperationException`。

- [x] **Step 4: 用多边形节点替换叠加矩形和旋转方盒**

`BuildCollisions()` 只为 `FrontBodyFootprint`、`FrontPlayerTopFootprint`、`FrontGuestTopFootprint` 调用多边形 helper；`BuildGrayboxVisuals()` 在现实／眼镜层各创建一次对应 `ArrayMesh`。彻底删除对 `FrontBarBodySections`、`FrontBarInnerChamfers`、`PlayerWorktopSections`、`PlayerWorktopChamfers` 的迭代。

- [x] **Step 5: 运行布局和运行时几何场景**

Run:

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarProductionLayoutContractTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarRuntimeGeometryTests.tscn
```

Expected: 两个场景均 PASS；场景树不再包含旧倒角附加件。

- [x] **Step 6: 提交多边形台体**

```powershell
git add -- scripts/world/BarPolygonGeometry.cs scripts/world/GrayboxArchitectureBuilder.cs tests/godot/BarRuntimeGeometryTests.cs tests/godot/BarRuntimeGeometryTests.tscn
git commit -m "feat: rebuild bar from authoritative polygon"
```

---

### Task 3: 建立封闭储物宿主和物品门禁

**Files:**
- Modify: `scripts/world/CabinetBuilder.cs`
- Modify: `scripts/gameplay/CabinetInteractable.cs`
- Modify: `scripts/gameplay/ToolInteractable.cs`
- Modify: `scripts/gameplay/StationInteractable.cs`
- Modify: `scripts/world/GameplaySceneComposer.cs`
- Create: `tests/godot/BarStorageIntegrationTests.cs`
- Create: `tests/godot/BarStorageIntegrationTests.tscn`

**Interfaces:**
- Consumes: `BarStorageLayout`、`BarItemStorageLayout`、`CabinetInteractable.IsOpen`。
- Produces: `CabinetBuilder.StorageHosts`、`CabinetInteractable.OpenStateChanged(bool)`、`BindStorage(CabinetInteractable)` on tools/stations。

- [x] **Step 1: 写储物行为失败测试**

实例化主场景并启动一天，逐项验证：

```csharp
Require(layout.ItemStorageAssignments.Select(item => item.ItemId).Order().SequenceEqual(
        expectedStoredIds.Order()), "every approved tool and ingredient has one storage");
Require(!tool.CanInteract(context), "tool is inaccessible while its storage is closed");
front.SetOpen(true, false);
Require(tool.CanInteract(context), "tool becomes accessible after its storage opens");
other.SetOpen(true, false);
Require(!front.IsOpen && other.IsOpen, "only one storage front remains open");
```

测试还必须在 `RestartDay` 后断言所有 `cabinet_storage` 关闭、每项回到相同 `StorageId`，并确认 `front_drawer_2_upper/ice_bucket` 不变。

- [x] **Step 2: 运行储物测试并观察红灯**

Run:

```powershell
dotnet build GlassesBar.csproj --configuration Debug --nologo
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarStorageIntegrationTests.tscn
```

Expected: 工具仍直接位于台面／无储物绑定，测试失败。

- [x] **Step 3: 让 CabinetBuilder 返回稳定宿主**

为每个 `BarStorageLayout` 创建名为 `${StorageId}_host` 的 `Node3D`；抽屉宿主作为抽屉子节点并使用 `HostPosition - Front.Center` 局部位置，门柜宿主作为 `NeutralGameplay` 子节点使用绝对 `HostPosition`，避免随门扇旋转。新增：

```csharp
public IReadOnlyDictionary<string, Node3D> StorageHosts => _storageHosts;
public CabinetInteractable RequireFront(string storageId) =>
    _fronts.TryGetValue(storageId, out var front)
        ? front
        : throw new InvalidOperationException($"Unknown storage '{storageId}'.");
```

- [x] **Step 4: 为工具和站点加入储物门禁**

两类交互体新增 `_storageFront` 和 `BindStorage(CabinetInteractable front)`；`CanInteract` 首先拒绝 `_storageFront is { IsOpen: false }`，`GetUnavailablePrompt` 返回具体储物显示语义。`CabinetInteractable.SetOpen` 在立即设置和 tween 开始时发出 `OpenStateChanged`，但不复制物品玩法状态。

- [x] **Step 5: 按局部分配创建物品**

`GameplaySceneComposer.BuildStations` 和 `BuildTools` 对每项查找唯一分配；节点加入对应宿主，`Position = LocalPlacement`，再调用 `BindStorage(front)`。`customer`、`hand_wash_sink`、`waste_bin` 仍是固定站点，不进入储物分配；`coffee_beans`、`kettle`、九件工具和 `ice_bucket` 必须进入指定储物。`DrinkWorkstation.RegisterTool` 接收宿主闭合状态下的 `node.GlobalPosition` 作为每日复位点。

- [x] **Step 6: 运行储物、输入和流程测试**

Run:

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarStorageIntegrationTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/InputIntegrationTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/FlowIntegrationTests.tscn
```

Expected: 三个场景均 PASS，既有稳定 ID 与手工流程未改变。

- [x] **Step 7: 提交储物交互**

```powershell
git add -- scripts/world/CabinetBuilder.cs scripts/gameplay/CabinetInteractable.cs scripts/gameplay/ToolInteractable.cs scripts/gameplay/StationInteractable.cs scripts/world/GameplaySceneComposer.cs tests/godot/BarStorageIntegrationTests.cs tests/godot/BarStorageIntegrationTests.tscn tests/godot/InputIntegrationTests.cs tests/godot/FlowIntegrationTests.cs
git commit -m "feat: store bar tools behind closed cabinetry"
```

---

### Task 4: 收敛 Z3 固定功能并重建五湾空瓶架

**Files:**
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `scripts/world/BarLayoutDefinition.cs`
- Modify: `tests/godot/BarProductionLayoutContractTests.cs`
- Modify: `tests/godot/BarRuntimeGeometryTests.cs`

**Interfaces:**
- Consumes: 五个后吧湾、三类固定站点、前吧水槽开放包络。
- Produces: 五组对齐下柜／空瓶架／上柜表现；旧节点禁止列表。

- [x] **Step 1: 扩展红灯测试覆盖硬删除和净空**

禁止节点列表至少包括：

```csharp
string[] forbidden =
{
    "MergedBottleRackBack", "MergedShelf0", "MergedShelf1",
    "EastWetOuterSupport", "EastWetInnerSupport", "InspectionDoor",
    "FrontBarWestChamfer", "FrontBarEastChamfer"
};
Require(forbidden.All(name => !reality.FindChildren(name, string.Empty, true, false).Any()),
    "obsolete Z3 geometry is absent");
Require(Enumerable.Range(0, 14).All(i =>
        !reality.FindChildren($"BackLiquor{i}", string.Empty, true, false).Any()),
    "placeholder bottles are absent");
```

同时以包络相交函数断言 `SinkUnderClearVolume` 与所有静态碰撞形状不相交。

- [x] **Step 2: 运行布局／运行时测试并观察红灯**

Run: Task 2 Step 5 的两个场景。

Expected: 旧湿区、旧瓶架或水槽下结构仍在时失败。

- [x] **Step 3: 只生成批准的 Z3 固定功能**

在 `BuildCounterDetails` 中删除东湿区支撑、旧检查门和旧台面片；创建东端嵌入水槽开口、碗和龙头，但不创建水槽下任何 Mesh／Collision。西侧只创建 `ManualShelf` 与唯一 `OperationManual`。东侧回转区只创建 `EastWasteModule`、`waste_bin` 表现和 `EmployeeGate`。

- [x] **Step 4: 用五个独立湾替换连续瓶架**

删除 `BuildMergedBackRack`；新增 `BuildBottleRackBays`，逐湾创建独立 `Back`、`LowerShelf`、`UpperShelf`，节点名为 `BottleRackBay1…5`，每湾中心与对应 1.70 m 下柜和上柜一致。当前不创建任何瓶体 Mesh。

- [x] **Step 5: 运行布局、几何、冒烟测试**

Run:

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarProductionLayoutContractTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/BarRuntimeGeometryTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path . --quit-after 300 res://tests/godot/SmokeTests.tscn
```

Expected: 三个场景均 PASS；瓶架空、齐、无连续旧件。

- [x] **Step 6: 提交 Z3 固定结构**

```powershell
git add -- scripts/world/BarLayoutDefinition.cs scripts/world/GrayboxArchitectureBuilder.cs tests/godot/BarProductionLayoutContractTests.cs tests/godot/BarRuntimeGeometryTests.cs
git commit -m "feat: rebuild aligned Z3 bar fixtures"
```

---

### Task 5: 验证运行时包络与开合全过程

**Files:**
- Modify: `tests/godot/BarRuntimeGeometryTests.cs`
- Modify: `tools/run_verification.ps1`

**Interfaces:**
- Consumes: 实例化后的 `MeshInstance3D`、`CollisionShape3D`、储物宿主包络与开合 API。
- Produces: 运行时全局 AABB 诊断、0/25/50/75/100% 状态采样和全量回归门禁。

- [x] **Step 1: 写会暴露越界／重叠的运行时诊断**

加入 `TransformAabb(Aabb local, Transform3D global)`，枚举局部 AABB 八个角并合并；每个已分配物品的所有 `MeshInstance3D` 必须位于房间和所属 `HostSize` 包络内。失败信息必须是：

```csharp
throw new InvalidOperationException(
    $"ItemId={item.ItemId}; StorageId={item.StorageId}; " +
    $"Intersects={other.Name}; Sample={sample:0.##}");
```

对每个抽屉／门用 `SetOpen(false/true, false)` 取得闭合与全开变换，再对 `0f, .25f, .5f, .75f, 1f` 插值位置或旋转，逐帧验证不与相邻储物、台体和 `SinkUnderClearVolume` 相交。

- [x] **Step 2: 运行诊断并观察现有几何缺口**

Run: Task 2 的 `BarRuntimeGeometryTests.tscn`。

Expected: 若任何局部摆位、开门弧、抽屉行程或宿主尺寸非法，输出具体 ItemId／StorageId／相交对象／采样值并失败。

- [x] **Step 3: 只通过修改权威布局修复失败**

调整只发生在 `BarLayoutDefinition` 的轮廓、宿主包络或 `LocalPlacement`；不得在构建器中为单个对象增加隐藏偏移、缩放、裁剪或场景外坐标。每次修复后重跑布局、储物和几何三个场景。

- [x] **Step 4: 把新增场景接入一键验证**

在生产布局合同之后依次运行：

```powershell
& $godotPath --headless --path $root --quit-after 300 res://tests/godot/BarStorageIntegrationTests.tscn
Assert-LastExitCode 'Godot bar storage integration'
& $godotPath --headless --path $root --quit-after 300 res://tests/godot/BarRuntimeGeometryTests.tscn
Assert-LastExitCode 'Godot bar runtime geometry'
```

- [x] **Step 5: 运行完整验证**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

Expected: 资产验证、领域测试、Debug／Release、Godot 导入、布局、储物、运行时几何、冒烟、Stage 1/2、输入和流程全部退出码 0。

- [x] **Step 6: 提交运行时门禁**

```powershell
git add -- tests/godot/BarRuntimeGeometryTests.cs tools/run_verification.ps1
git commit -m "test: reject out-of-bounds bar geometry"
```

---

### Task 6: 生成并人工检查 16 张 Forward+ 审批图

**Files:**
- Modify: `tests/godot/BarProductionVisualCapture.cs`
- Modify: `tests/godot/BarProductionVisualCapture.tscn`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `progress.md`

**Interfaces:**
- Consumes: 已通过回归的新灰盒场景。
- Produces: ignored 的 `artifacts/visual_review_bar_graybox_z3_h3/01…16.png`、SHA-256、人工审查记录和用户验收门。

- [x] **Step 1: 把捕获列表改为批准的 16 视角**

视图名称固定为：

```text
01_overhead_9m10_span
02_player_eye_customer_view
03_west_manual_only
04_east_waste_and_gate
05_east_sink_open_underbay
06_west_chamfer_close
07_east_chamfer_close
08_all_front_storage_closed
09_tool_storage_open
10_ice_drawer_fully_open
11_five_bay_empty_rack_front
12_coffee_kettle_cabinets_open
13_customer_chairs_pulled
14_reality_lighting
15_glasses_lighting
16_runtime_aabb_overview
```

每帧使用固定 FOV／曝光并保存 1920×1080 PNG；开柜视图只打开该帧所需正面，下一帧先复位全部储物。

- [x] **Step 2: 运行 Forward+ Movie Maker 捕获**

Run:

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --resolution 1920x1080 --write-movie artifacts/visual_review_bar_graybox_z3_h3/capture.avi res://tests/godot/BarProductionVisualCapture.tscn
```

Expected: `BAR_PRODUCTION_VISUAL_CAPTURE_PASS`，16 个 PNG 全部存在且尺寸为 1920×1080。

- [x] **Step 3: 逐张视觉审查**

使用本地图片查看器逐张确认：9.10 m 横向比例、H3 高度、玩家眼高、两处拐角无三角堆砌／Z-fighting、西侧无手册外功能、东侧无弃物／门外功能、水槽下完全开放、全部工具／原料初始不在台面、打开柜体内物品不越界、五湾瓶架笔直对齐且为空、客区通路、现实／眼镜照明可读。任一失败回到对应 Task 修改并重新跑完整验证及全部 16 帧。

- [x] **Step 4: 记录哈希与未批准边界**

Run:

```powershell
Get-FileHash artifacts/visual_review_bar_graybox_z3_h3/*.png -Algorithm SHA256
```

把 16 个哈希、Godot 版本、GPU、分辨率、人工检查结论写入四份项目状态文档；明确“实现与内部验证完成，等待用户检查新灰盒”，不得写成用户已批准。

- [x] **Step 5: 最后再跑一次完整验证和差异检查**

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
git diff --check
git status --short
```

Expected: 验证退出码 0；无 whitespace error；`export_presets.cfg` 保持未暂存，其他预期文件可提交。

- [x] **Step 6: 提交截图工具与最终归档**

```powershell
git add -- tests/godot/BarProductionVisualCapture.cs tests/godot/BarProductionVisualCapture.tscn docs/CONTEXT_HANDOFF.md docs/PROJECT_STATUS.md docs/CHANGELOG.md progress.md
git commit -m "feat: deliver rebuilt Z3 H3 bar graybox"
```

完成后向用户展示最能判断问题的总览、东西拐角、水槽下净空、全关收纳和五湾空瓶架图片，并请求新灰盒批准。正式环境 GLB／Blender 任务仍保持未开始。
