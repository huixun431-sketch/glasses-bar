# Bar Layout Production Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the obsolete oversized bar graybox with the approved 12 m × 9 m playable layout, obtain explicit in-game scale approval, then build and integrate a complete modular Blender bar without changing the established gameplay contract.

**Architecture:** `BarLayoutDefinition` remains the authoritative metric and stable-ID contract. `NeutralGameplay` owns collision, stations, moving drawers/doors/manual state, and the single shared production counter/backbar visuals. `RealityWorld` and `GlassesWorld` receive separate instances of the same static GLB resources with different material variants; glasses-only wear overlays are non-colliding. `GrayboxLevelBuilder` always builds authoritative gameplay and collisions, tries to load production visuals, and falls back to graybox visuals if any required production module fails validation or loading.

**Tech Stack:** Godot 4.7.1 .NET/C#, .NET 8, Blender 4.5.5 LTS Python API, glTF 2.0/GLB, Python 3 asset validation, PowerShell verification, Forward+ Movie Maker visual capture.

## Global Constraints

- The approved source of truth is `docs/superpowers/specs/2026-08-02-bar-layout-production-model-design.md`. Do not reinterpret orientation, dimensions, materials, lighting, furniture, or deferred scope from older graybox code or screenshots.
- Use `C:\Users\lenovo\Desktop\美术设计案.docx` as the secondary art-direction reference for low-poly retro 3D, readable silhouettes, hand-painted base color, AO, and simplified PBR. It must not override the current approved layout, scale, material assignment, or interaction decisions.
- World axes are fixed: north `-Z`, south `+Z`, east `+X`, west `-X`. The player starts inside the U and faces south toward customers. The rear bar is north; the front bar is the south horizontal run; east and west side runs extend north.
- The implementation must pause twice for explicit user approval: after the complete playable graybox capture and after the production geometry/material review. Do not begin formal Blender geometry before the graybox approval.
- Preserve existing gameplay stable IDs, especially `front_drawer_2_upper`, `ice_bucket`, `hand_wash_sink`, `waste_bin`, and `bar_counter`. Do not rename asset IDs to match runtime station IDs.
- Keep one authoritative transform/state for every moving drawer, upper-cabinet door, employee gate, and manual. Do not create reality/glasses duplicates of moving gameplay state.
- The manual is modeled with `Grip`, `Placement`, cover, and page pivots and must be picked up before opening. Full-screen reading, drag interaction, page-turn gameplay, and return animation remain out of scope.
- Do not add customer AI, backstage geometry, recipes, balance values, customer stories, automatic delivery/snap, waste automation, storage gameplay, or floor/foot lighting.
- Cancel every bar-attached continuous footrail and fixed footboard. Keep only the front-face toe recess and each loose stool's independent foot ring.
- Imported GLBs are visual resources only. Collisions, lights, stable IDs, anchors, interaction nodes, and state ownership live in hand-authored Godot scenes/scripts.
- Preserve the user's unrelated `export_presets.cfg` modification. Do not stage, commit, or rewrite it.
- Each commit command below is a proposed boundary. Before running `git add` or `git commit`, obtain the user's explicit permission required by the repository instructions.
- Temporary `.blend` files and review renders belong under ignored `artifacts/`; commit reproducible Blender scripts, validated GLBs, Godot wrappers, tests, and documentation.

## File Structure Map

### Authoritative design and handoff

- Reference: `docs/superpowers/specs/2026-08-02-bar-layout-production-model-design.md`
- Create: `docs/assets/BAR_INTERIOR_ASSET_CONTRACT.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/CHANGELOG.md`
- Create or modify: `progress.md`

### Layout, gameplay, and visual composition

- Modify: `scripts/world/BarLayoutDefinition.cs`
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `scripts/world/GrayboxLevelBuilder.cs`
- Modify: `scripts/world/CabinetBuilder.cs`
- Modify: `scripts/world/GameplaySceneComposer.cs`
- Modify: `scripts/gameplay/CabinetInteractable.cs`
- Create: `scripts/world/BarEnvironmentVisualLoader.cs`
- Create: `scripts/world/BarGameplayVisualBinder.cs`
- Create: `scripts/world/BarMaterialVariantController.cs`
- Create: `scripts/world/BarLightRigController.cs`
- Modify: `scenes/Main.tscn`
- Create: `scenes/environment/BarInteriorProduction.tscn`
- Create: `scenes/environment/modules/BarArchitectureVisual.tscn`
- Create: `scenes/environment/modules/BarCounterVisual.tscn`
- Create: `scenes/environment/modules/BarBackbarVisual.tscn`
- Create: `scenes/environment/modules/BarFurnitureVisual.tscn`
- Create: `scenes/environment/modules/BarLightingVisual.tscn`
- Create: `scenes/environment/modules/BarWearOverlaysVisual.tscn`

### Blender production and exported resources

- Create: `tools/blender/bar_model_common.py`
- Create: `tools/blender/build_bar_master.py`
- Create: `tools/blender/export_bar_modules.py`
- Create: `tools/blender/render_bar_review.py`
- Create: `tools/blender/tests/test_bar_model_contract.py`
- Generate locally: `artifacts/blender/bar_interior_master.blend`
- Create: `models/bar_architecture.glb`
- Replace placeholder: `models/bar_counter.glb`
- Create: `models/bar_backbar.glb`
- Create: `models/bar_furniture.glb`
- Create: `models/bar_lighting.glb`
- Create: `models/bar_wear_overlays.glb`

### Asset validation, automated tests, and visual capture

- Modify: `assets/asset_manifest.json`
- Modify: `tools/validate_assets.py`
- Modify: `tools/run_verification.ps1`
- Create: `tests/godot/BarProductionLayoutContractTests.cs`
- Create: `tests/godot/BarProductionLayoutContractTests.tscn`
- Create: `tests/godot/BarProductionAssetIntegrationTests.cs`
- Create: `tests/godot/BarProductionAssetIntegrationTests.tscn`
- Create: `tests/godot/BarProductionVisualCapture.cs`
- Create: `tests/godot/BarProductionVisualCapture.tscn`
- Modify: `tests/godot/InputIntegrationTests.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`
- Replace: `tests/godot/BarLayoutVisualCapture.cs`
- Modify: `tests/godot/BarLayoutVisualCapture.tscn`

---

### Task 1: Encode the approved metric and stable-ID contract

**Files:**
- Create: `tests/godot/BarProductionLayoutContractTests.cs`
- Create: `tests/godot/BarProductionLayoutContractTests.tscn`
- Modify: `scripts/world/BarLayoutDefinition.cs`
- Modify: `tools/run_verification.ps1`

- [x] **Step 1: Write the failing layout contract scene**

Create a Godot test scene that calls `BarLayoutDefinition.Prototype.Validate()` and asserts all approved dimensions and counts without instantiating `Main.tscn`:

```csharp
Require(layout.RoomClearSize.IsEqualApprox(new Vector3(12f, 3.5f, 9f)), "room is 12 by 9 by 3.5 metres");
Require(layout.SouthWindows.Count == 1 &&
        layout.SouthWindows[0].Size.IsEqualApprox(new Vector3(3.2f, 1.55f, layout.WallThickness)) &&
        Mathf.IsEqualApprox(layout.SouthWindows[0].SillHeight, 0.75f),
    "south elevation has one east landscape window");
Require(Mathf.IsEqualApprox(layout.GuestCounterTopHeight, 1.20f) &&
        Mathf.IsEqualApprox(layout.PlayerWorktopHeight, 0.96f) &&
        Mathf.IsEqualApprox(layout.OperationAisleClearWidth, 1.40f) &&
        Mathf.IsEqualApprox(layout.DrawerOpenTravel, 0.38f),
    "gameplay scale uses the approved compromise values");
Require(layout.Cabinets.Count(c => c.Kind == CabinetPartKind.Drawer) == 8 &&
        layout.Cabinets.Single(c => c.ContainsIceBucket).Id == "front_drawer_2_upper",
    "front bar keeps eight drawers and the stable ice drawer ID");
Require(layout.FrontStools.Count == 6 && layout.LoungeTables.Count == 3 &&
        layout.LoungeChairs.Count == 12,
    "customer furniture counts are locked");
Require(layout.PendantFixtures.Count == 3 && layout.RearLinearFixtures.Count == 2 &&
        layout.CustomerSconces.Count == 4 && layout.CustomerFillLights.Count == 2,
    "all approved lighting groups are represented");
Require(layout.FrontFootrails.Count == 0,
    "the front bar has no attached footrail or fixed footboard");
```

Also assert north/south orientation, 5.60 m front outline, 0.80 m front depth split into 0.62 + 0.18 m, east wet side 0.80 m, west dry side 0.65 m, internal clear span 4.15 m, three front facade bays, 0.90 m handoff strip, 0.38 m upper cabinet depth, bottle-rack levels 1.34/1.68 m, upper cabinet lower edge 2.10 m, one 1.40 m south double door, and one approximately 0.90 m north-east service door.

- [x] **Step 2: Run the new test and confirm it fails for the old layout**

Run:

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path 'D:\眼镜酒馆开发' --quit-after 300 res://tests/godot/BarProductionLayoutContractTests.tscn
```

Expected: non-zero exit because the current layout is 14 m × 13.3 m, uses a 1.42 m front top, 2.00 m eye height, 2.31 m aisle, four stools, booths, and three windows.

- [x] **Step 3: Replace the old layout data with explicit approved records**

Extend `BarLayoutDefinition.cs` with immutable record types for openings, furniture, light fixtures, manual shelf geometry, and counter sections. Add explicit `OpenTravelDistance` to `BarCabinetLayout`:

```csharp
public readonly record struct BarCabinetLayout(
    string Id,
    CabinetPartKind Kind,
    Vector3 Center,
    Vector3 Size,
    bool HingeOnLeft,
    Vector3 OutwardDirection,
    float StorageDepth,
    float OpenTravelDistance,
    BarBoxLayout? Cavity,
    bool ContainsIceBucket);
```

Use a centered 12 m × 9 m room envelope (`X = -6..+6`, `Z = -4.5..+4.5`). Derive the west/north blueprint baseline from named constants rather than scattered literals: west bar clearance + half of the fixed 5.60 m outline determines the bar X axis; north rear-bar depth, 1.40 m clear aisle, and 0.80 m front section determine the U section along Z. The exact section dimensions take precedence over the approximate 3.20 m U-envelope note.

Encode the approved sink, waste opening, manual shelf, workboard, drawer, lower sliding-panel, bottle-rack, upper-door, doorway, window, table, chair, stool, and light values from the spec. Retain existing runtime station/tool IDs even when their coordinates move.

- [x] **Step 4: Strengthen `Validate()` against orientation and regression errors**

Add validation for unique IDs, positive sizes, the single-window rule, a south-facing player vector, north rear-bar/south front-bar ordering, 4.15 m internal cross-span, exact furniture/light counts, exactly one ice drawer, no drawers below the wet-side sink, no bar-attached footrail, and minimum pulled-out-chair route clearances represented by the layout acceptance envelopes.

- [x] **Step 5: Add the contract scene to the one-command verifier**

Append the test immediately after Godot import in `tools/run_verification.ps1` and require the token `BAR_PRODUCTION_LAYOUT_CONTRACT_PASS` before continuing.

- [x] **Step 6: Run the focused test and full non-visual regression**

Run the focused command from Step 2, then:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

Expected: layout contract and all existing test suites pass after old geometry assertions are updated in later tasks. If old scene assertions fail here, record the expected failures and do not weaken unrelated gameplay assertions.

- [x] **Step 7: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/BarLayoutDefinition.cs tests/godot/BarProductionLayoutContractTests.cs tests/godot/BarProductionLayoutContractTests.tscn tools/run_verification.ps1
git commit -m "test: lock approved production bar layout"
```

### Task 2: Rebuild the architectural graybox and player orientation

**Files:**
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `scripts/world/GrayboxLevelBuilder.cs`
- Modify: `scenes/Main.tscn`
- Modify: `tests/godot/InputIntegrationTests.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`

- [x] **Step 1: Write failing runtime assertions for shell, openings, and player pose**

Update integration assertions to require one south-east window, the inward-opening 0.70 + 0.70 m main door, the north-east service door opening north, no old booths/windows, a 1.78–1.88 m eye-height target, and a player forward vector with positive Z:

```csharp
var playerForward = player.GlobalBasis * Vector3.Forward;
Require(playerForward.Z > 0.99f, "player faces south toward customers");
Require(player.GlobalPosition.Z > layout.RearBarFrontZ &&
        player.GlobalPosition.Z < layout.FrontBarInnerEdgeZ,
    "player starts inside the U between rear and front bars");
Require(main.GetNode<Node3D>("RealityWorld").GetNode<Node3D>("SouthWindows").GetChildCount() == 1,
    "only the approved south-east landscape window exists");
```

- [x] **Step 2: Run the input and flow scenes and confirm the old geometry fails**

```powershell
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path 'D:\眼镜酒馆开发' --quit-after 300 res://tests/godot/InputIntegrationTests.tscn
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --headless --path 'D:\眼镜酒馆开发' --quit-after 300 res://tests/godot/FlowIntegrationTests.tscn
```

Expected: failures name the old player height/position and old wall/window nodes.

- [x] **Step 3: Separate authoritative collisions from replaceable graybox visuals**

Refactor the builder surface to:

```csharp
public void BuildCollisions();
public void BuildGrayboxVisuals();
```

`BuildCollisions()` creates the floor, segmented walls around actual openings, U-counter boundaries, rear-bar boundary, and door-leaf collision bodies under `NeutralGameplay`. `BuildGrayboxVisuals()` creates only replaceable meshes and labels under `RealityWorld`/`GlassesWorld`.

- [x] **Step 4: Build the approved shell and openings from the layout records**

Remove the 14 m × 13.3 m floor, four 5 m-high walls, three south windows, and booths. Build the 12 m × 9 m × 3.5 m shell, 1.05 m wainscot break, one south-east 3.20 m × 1.55 m window with 0.75 m sill, the inward-opening south double door, and the north-east service door opening north. Do not create backstage rooms behind the service door.

- [x] **Step 5: Coordinate the player body, head, and camera with the new work scale**

Keep a normal gameplay eye target inside 1.78–1.88 m, update `Main.tscn` body/head offsets together, position the player in the 1.40 m operating aisle, and preserve the 180° Y rotation that converts Godot forward `-Z` into world south `+Z`.

- [x] **Step 6: Re-run the focused tests**

Expected: the player starts inside the U, faces south, can move inside the aisle, cannot pass through the U boundary, and all opening counts and directions pass.

- [x] **Step 7: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/GrayboxArchitectureBuilder.cs scripts/world/GrayboxLevelBuilder.cs scenes/Main.tscn tests/godot/InputIntegrationTests.cs tests/godot/FlowIntegrationTests.cs
git commit -m "feat: rebuild approved bar architectural graybox"
```

### Task 3: Implement the U-bar interaction graybox at exact scale

**Files:**
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `scripts/world/CabinetBuilder.cs`
- Modify: `scripts/world/GameplaySceneComposer.cs`
- Modify: `scripts/gameplay/CabinetInteractable.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`

- [x] **Step 1: Replace old storage assertions with exact interaction-scale tests**

Assert 8 drawers with 0.38 m travel, 6 upper doors at approximately 85°, one closed half-height employee gate on the east side, sink under-clearance, north-adjacent open waste bin, central three-slot workboard, 0.90 m handoff strip, west manual shelf, and no automatic snapping or waste behavior.

- [x] **Step 2: Run the flow scene and confirm it fails before implementation**

Expected: the current `CabinetInteractable` reports at least 0.56 m drawer travel and old station coordinates.

- [x] **Step 3: Make drawer travel an explicit layout value**

Change the configuration surface to:

```csharp
public void Configure(
    string id,
    CabinetPartKind kind,
    Vector3 center,
    Vector3 size,
    bool hingeOnLeft,
    Vector3 outwardDirection,
    float storageDepth,
    float openTravelDistance)
```

For drawers, reject non-positive `openTravelDistance` and compute `_openPosition = center + _outwardDirection * openTravelDistance`. Keep 1.48 radians for approved approximately 85° doors. Update `CabinetBuilder` to pass each `BarCabinetLayout.OpenTravelDistance`.

- [x] **Step 4: Rebuild the U counter and stations**

Build the 5.60 m south front run with 0.80 m section depth, 0.62 m player surface, 0.18 m south overhang, 1.20 m guest top, 0.96 m player top, 0.06/0.04 m thicknesses, R8 guest outer edge approximation, 3 mm exposed chamfers, three approximately 1.87 m facade bays, 0.90 m center handoff, 0.08 m × 0.12 m toe recess, and no attached footrail.

Build the east 0.80 m wet side and west 0.65 m dry side, retain the 5.60 m exterior axis, offset the 4.15 m inner span 0.075 m west, use approximately 0.35 m 45° inner chamfers, and close the east employee gate. Place the 2.05 m three-slot workboard, 2–3 mm parking etches with approximately 40 mm gaps, and the eight short-travel drawers; retain `front_drawer_2_upper` as the ice bucket drawer.

Move the existing high-frequency tools as one cohesive cluster around the central workboard while preserving every stable tool ID and the wet-east/dry-west zoning. The shallow parking etches remain visual parking marks only: do not add snapping, auto-return, or delivery behavior.

- [x] **Step 5: Build the approved wet and manual-side fixtures**

East wet side: retain an undermount sink with an actual open bowl, horizontal XZ rim, unobstructed center clearance and a faucet rooted on the east rim. The faucet uses a vertical riser plus a short down-angled outlet into the bowl; reject the obsolete transverse pipe/vertical-frame geometry. Below it retain a solid drawer-free base that hides all plumbing, then the waste module and the `0.90 m` employee gate. Do not restore the deleted east-front lower cabinet or inspection door.

West dry side: keep only the manual shelf. The 2026-08-11 final review supersedes the earlier east-west candidate: rotate that rejected placement clockwise `90°`, reduce it from `0.80 m × 0.32 m` to `0.62 m × 0.28 m`, and align the long edge with the west-counter depth while retaining the inward reading slope and stop. Keep the manual as a separate pick-up item with `Grip`, `Placement`, cover, and page-pivot nodes; do not reintroduce obsolete tools or ingredients on the west return.

- [x] **Step 6: Rebuild the rear bar graybox**

Use a 5.60 m-wide, 0.50 m-deep, 0.96 m-high rear counter with wet east/dry west, three approximately 1.87 m lower modules with two overlapping inset noninteractive sliding panels each, two bottle-rack levels at 1.34 m and 1.68 m with 0.28 m depth, continuous back, thin uprights, and a 12 mm front lip. Add the 0.38 m-deep upper cabinet with 0.12 m setback, 2.10 m lower edge, approximately 3.10 m top, and three pairs/six independent 85° doors.

- [x] **Step 7: Run focused and full regression tests**

Expected: exact drawer travel, open-door direction, sink/waste/manual positions, stable IDs, old gameplay flow, reset semantics, and world-mode interaction gating all pass.

- [x] **Step 8: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/GrayboxArchitectureBuilder.cs scripts/world/CabinetBuilder.cs scripts/world/GameplaySceneComposer.cs scripts/gameplay/CabinetInteractable.cs tests/godot/FlowIntegrationTests.cs
git commit -m "feat: implement approved U bar interaction graybox"
```

### Task 4: Add the approved customer seating and complete graybox light rig

**Files:**
- Modify: `scripts/world/GrayboxArchitectureBuilder.cs`
- Modify: `scenes/Main.tscn`
- Modify: `tests/godot/BarProductionLayoutContractTests.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`

- [x] **Step 1: Add failing runtime count and clearance assertions**

Require six backless stools, three 0.80 m tables, twelve independent chairs, four wall sconces, two invisible customer fills, three front-bar pendants, and two rear linear lights. Assert that the three pulled-out chair envelopes preserve a 1.20 m main route and 0.90 m secondary routes without intersecting the entry swing or landscape window access envelope.

- [x] **Step 2: Run layout and flow tests and confirm the old furniture/light rig fails**

Expected: old booths, four stools, and generic `KeyLight`/`WarmLight` nodes violate the contract.

- [x] **Step 3: Build the loose customer furniture graybox**

Place six 0.78 m-high, approximately 0.40 m-diameter backless stools at approximately 0.65 m centers, two per front facade bay, with the center pair flanking the 0.90 m handoff. Model each stool's independent foot ring; create no bar-attached rail.

Place three 0.80 m-diameter, approximately 0.75 m-high central-pedestal tables in a loose north-south stagger in the east customer zone, with approximately 2.35 m center spacing and alternating 0.30 m east-west offset. Place four independent armless chairs per table using 0.46 m width, 0.50 m depth, 0.46 m seat height, and 0.86 m overall height. Use pulled-out positions for clearance validation.

- [x] **Step 4: Replace generic global lights with the approved fixture groups**

Build three pendants on the east-west front-bar axis, center one over the 0.90 m handoff, space adjacent pendants approximately 1.40 m, and keep the lowest visible point within 2.35–2.45 m. Build two concealed rear linear task lights.

Build two sconces on each east/west wall at center height approximately 2.15 m, entirely above the 1.05 m wainscot, with soft upward and downward spread. Build two low-energy invisible wide-angle ceiling fills for the customer zone, one south and one north. Do not place fixtures above the south window/main entry, on the floor, or at the bar foot zone. Reality light is warm; glasses light is cold/desaturated.

- [x] **Step 5: Run all focused tests**

Expected: counts, stable node groups, route envelopes, fixture exclusions, and world-specific light settings pass.

- [x] **Step 6: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/GrayboxArchitectureBuilder.cs scenes/Main.tscn tests/godot/BarProductionLayoutContractTests.cs tests/godot/FlowIntegrationTests.cs
git commit -m "feat: add customer seating and bar lighting graybox"
```

### Task 5: Capture and obtain approval for the complete playable graybox

**Files:**
- Replace: `tests/godot/BarLayoutVisualCapture.cs`
- Modify: `tests/godot/BarLayoutVisualCapture.tscn`
- Create: `tests/godot/BarProductionVisualCapture.cs`
- Create: `tests/godot/BarProductionVisualCapture.tscn`
- Modify: `docs/CONTEXT_HANDOFF.md`

- [x] **Step 1: Create deterministic review cameras and states**

Capture at least: overhead orientation, player eye facing south, west/north view of the U interior, east wet-side close-up, west manual shelf close-up, rear-bar open-door view, customer tables/chairs pulled out, south entry/window elevation, reality lighting, and glasses lighting. Hide menus/HUD, set deterministic exposure, and write the selected camera/state name into each filename.

- [x] **Step 2: Render with Forward+ Movie Maker, not headless-only geometry inspection**

```powershell
New-Item -ItemType Directory -Force artifacts/visual_review_bar_graybox | Out-Null
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path 'D:\眼镜酒馆开发' --write-movie 'artifacts/visual_review_bar_graybox/frame.png' --fixed-fps 1 --quit-after 12 res://tests/godot/BarProductionVisualCapture.tscn
```

- [x] **Step 3: Inspect every rendered frame**

Verify north rear bar, south front bar, player inside the U facing customers, entry/window placement, 1.40 m aisle, drawer reach, sink/waste/manual reach, pulled-out customer clearances, stool spacing, and all light groups. Record image hashes and any corrections in `docs/CONTEXT_HANDOFF.md`.

- [x] **Step 4: STOP — request explicit user approval of graybox scale and orientation**

Do not run Task 6 until the user approves the in-game graybox screenshots. Apply requested corrections in Tasks 1–5, rerun the contract/full suite, recapture, and ask again.

- [ ] **Step 5: Proposed commit boundary after approval and explicit commit permission**

```powershell
git add tests/godot/BarLayoutVisualCapture.cs tests/godot/BarLayoutVisualCapture.tscn tests/godot/BarProductionVisualCapture.cs tests/godot/BarProductionVisualCapture.tscn docs/CONTEXT_HANDOFF.md
git commit -m "test: approve production bar graybox scale"
```

### Task 6: Define and validate the modular GLB handoff contract

**Files:**
- Create: `docs/assets/BAR_INTERIOR_ASSET_CONTRACT.md`
- Modify: `assets/asset_manifest.json`
- Modify: `tools/validate_assets.py`
- Modify: `tools/run_verification.ps1`

- [x] **Step 1: Write failing validator self-tests for roots and required nodes**

Add a good fixture with the expected root and required nodes, a wrong-root fixture, a missing-required-node fixture, and a non-identity-transform fixture. Require errors to name the asset and missing contract item.

- [x] **Step 2: Run validator self-test and confirm failure**

```powershell
python tools/validate_assets.py --self-test
```

- [x] **Step 3: Extend the manifest schema without breaking existing assets**

Support optional `required_root` and `required_nodes` keys in addition to `required_anchors`. Register:

```json
{"id":"bar_architecture","path":"models/bar_architecture.glb","placeholder":true,"required_root":"bar_architecture","required_nodes":["room_shell","south_main_entry","south_east_window","north_east_service_door"]}
{"id":"bar_counter","path":"models/bar_counter.glb","placeholder":true,"required_root":"bar_counter","required_anchors":["Placement"],"required_nodes":["front_drawer_2_upper","east_sink","sink_base","waste_bin","employee_gate","manual_shelf","bar_structural_base","bar_carcass_monolith","bar_worktop_monolith"]}
{"id":"bar_backbar","path":"models/bar_backbar.glb","placeholder":true,"required_root":"bar_backbar","required_nodes":["rear_lower_cabinet_1_fixed","rear_lower_cabinet_1_moving","rear_lower_cabinet_2_fixed","rear_lower_cabinet_2_moving","rear_lower_cabinet_3_fixed","rear_lower_cabinet_3_moving","rear_lower_cabinet_4_fixed","rear_lower_cabinet_4_moving","rear_lower_cabinet_5_fixed","rear_lower_cabinet_5_moving","back_cabinet_1_left","back_cabinet_1_right","back_cabinet_2_left","back_cabinet_2_right","back_cabinet_3_left","back_cabinet_3_right","back_cabinet_4_left","back_cabinet_4_right","back_cabinet_5_left","back_cabinet_5_right","bottle_rack_bay_1","bottle_rack_bay_5"]}
{"id":"bar_furniture","path":"models/bar_furniture.glb","placeholder":true,"required_root":"bar_furniture","required_nodes":["stool_1","stool_6","lounge_table_1","lounge_table_3","lounge_chair_1","lounge_chair_12"]}
{"id":"bar_lighting","path":"models/bar_lighting.glb","placeholder":true,"required_root":"bar_lighting","required_nodes":["pendant_1","pendant_3","lounge_pendant_1","lounge_pendant_2","lounge_pendant_3","rear_linear_1","rear_linear_2","east_sconce_1","east_sconce_2","west_sconce_1","west_sconce_2"]}
{"id":"bar_wear_overlays","path":"models/bar_wear_overlays.glb","placeholder":true,"required_root":"bar_wear_overlays","required_nodes":["wear_overlay_root"]}
```

- [x] **Step 4: Document module ownership and every detachable pivot**

In `BAR_INTERIOR_ASSET_CONTRACT.md`, list meters/+Y/-Z, root names, stable node names, `Placement`/`Grip` anchors, applied transforms, material slots, detachable drawers/doors/manual parts, non-colliding overlays, and the rule that real lights/collisions live in Godot wrappers.

- [x] **Step 5: Run validator and full regression**

```powershell
python tools/validate_assets.py --self-test
python tools/validate_assets.py assets/asset_manifest.json --allow-placeholders
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

- [x] **Step 6: Commit boundary authorized by the user's continue-next-plan instruction**

```powershell
git add docs/assets/BAR_INTERIOR_ASSET_CONTRACT.md assets/asset_manifest.json tools/validate_assets.py tools/run_verification.ps1
git commit -m "feat: define production bar asset contract"
```

### Task 7: Create the deterministic Blender master and architecture module

**Files:**
- Create: `tools/blender/bar_model_common.py`
- Create: `tools/blender/build_bar_master.py`
- Create: `tools/blender/export_bar_modules.py`
- Create: `tools/blender/render_bar_review.py`
- Create: `tools/blender/tests/test_bar_model_contract.py`
- Generate locally: `artifacts/blender/bar_interior_master.blend`
- Generate candidate locally: `artifacts/blender_candidates/bar_architecture.glb`

- [x] **Step 1: Write failing Blender contract tests**

Use background Blender Python to assert scene unit scale 1.0 meters, +Y up/-Z forward export convention, named collections for all six modules, identity transforms at export roots, exact room/opening bounds, and no duplicate stable names.

- [x] **Step 2: Run the contract test and confirm missing scripts/collections fail**

```powershell
& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --factory-startup --python tools/blender/tests/test_bar_model_contract.py
```

- [x] **Step 3: Implement shared low-poly modeling helpers**

Add deterministic helpers for bevelled boxes, wall segments, cylindrical bases, material slots, empty anchors, collection isolation, transform application, triangle counting, GLB export, and review-camera setup. All dimensions come from one Python metric table copied verbatim from the approved Godot layout contract and cross-checked by the test; do not hand-type independent coordinate variants inside individual module builders.

- [x] **Step 4: Build the master scene and architecture geometry**

Create the approved 16 m × 10 m × 4.5 m shell, segmented south entry/window wall, north service door, deep-red walnut 1.05 m wainscot geometry, upper plaster faces, 0.18 m-wide north-south warm-oak floor boards, frames, handles, and trim. Keep south window and both doors as distinct named objects. Do not add backstage geometry.

- [x] **Step 5: Export and validate only the silhouette candidate**

```powershell
& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background --factory-startup --python tools/blender/build_bar_master.py -- --output artifacts/blender/bar_interior_master.blend
& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background artifacts/blender/bar_interior_master.blend --python tools/blender/export_bar_modules.py -- --module bar_architecture --mode silhouette --output artifacts/blender_candidates/bar_architecture.glb
```

Keep the silhouette GLB and review images ignored under `artifacts/`. Do not add a formal GLB, wrapper, or manifest switch before checkpoint-1 approval.

- [x] **Step 6: Render clay views and request architecture silhouette approval**

Render north/south/east/west interior elevations and an overhead view to `artifacts/blender_review/architecture/`. Verify openings, wainscot break, floor direction, and player-scale reference mannequin. Apply user corrections before continuing.

Status 2026-08-03: user approved checkpoint 1 after the south-door AREA-light projection was removed and all six neutral views were regenerated and manually inspected. Evidence: `docs/assets/BAR_ARCHITECTURE_SILHOUETTE_REVIEW_20260803.md`.

- [ ] **Step 7: Proposed commit boundary after approval and explicit permission**

```powershell
git add tools/blender/bar_model_common.py tools/blender/build_bar_master.py tools/blender/export_bar_modules.py tools/blender/render_bar_review.py tools/blender/tests/test_bar_model_contract.py models/bar_architecture.glb assets/asset_manifest.json
git commit -m "feat: build modular bar architecture asset"
```

### Task 8: Model the continuous C counter, wet/dry fixtures, manual shelf, and rear bar

**Files:**
- Modify: `tools/blender/build_bar_master.py`
- Modify: `tools/blender/tests/test_bar_model_contract.py`
- Create or replace: `models/bar_counter.glb`
- Create: `models/bar_backbar.glb`
- Modify: `assets/asset_manifest.json`

- [x] **Step 1: Add failing geometry and pivot assertions**

Assert all current approved Z3/H3 dimensions; a single continuous C-shaped structural base, carcass and worktop spanning front/west/rear; inward-only 45° chamfers; a 0.90 m east employee-gate cut; eight detachable drawer roots with 0.38 m travel; the exact stable ice drawer ID; ten upper-door hinge pivots with 85° collision-free sample poses; five lower sliding-door pairs; a retained solid sink base with no drawers or visible plumbing; waste opening; manual shelf slope/stops; three-slot workboard; five rear bays; two rack levels; hollow drawer clearance; and absence of a front footrail object. Reject obsolete separate front/rear worktop meshes and every dark-green cabinet material.

- [x] **Step 2: Run Blender contract tests and confirm failure**

- [x] **Step 3: Model `bar_counter` from the approved metric table**

Carve the front, west return and rear counter from one continuous C-shaped structural base, one carcass mesh and one worktop mesh. Apply the 45° chamfers only to the worker-side inner corners, then cut only the 0.90 m east employee gate. Retain the raised customer top and its approved 0.30 m customer-side extension, central handoff, workboard/etched parking marks, eight drawer faces/trays, sink/faucet above a solid drawer-free base, open waste aperture, employee gate and dedicated sloped manual shelf. Keep drawers, gate and manual parts separate under stable named roots. Do not expose plumbing or restore the deleted east-front lower cabinet. Use the existing `bar_counter` asset ID and `Placement` anchor.

- [x] **Step 4: Model `bar_backbar` from the approved metric table**

Build the approved rear counter as five regular bays: five hollow lower cabinets with sliding-door pairs, a continuous-backed five-bay/two-level bottle rack with thin non-overlapping uprights and 12 mm lips, set-back hollow upper cabinets, and ten independent upper doors with correct hinge pivots. Move each lower moving leaf slightly behind its fixed leaf so the closed seam does not read as a protruding edge. Preserve bottle-storage capacity while removing obsolete overlapping rack geometry.

- [x] **Step 5: Export, validate, and inspect open states**

```powershell
& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background artifacts/blender/bar_interior_master.blend --python tools/blender/export_bar_modules.py -- --module bar_counter --output models/bar_counter.glb
& 'D:\Applications\Blender 4.5 LTS\blender.exe' --background artifacts/blender/bar_interior_master.blend --python tools/blender/export_bar_modules.py -- --module bar_backbar --output models/bar_backbar.glb
python tools/validate_assets.py assets/asset_manifest.json --allow-placeholders
```

Keep both manifest entries as placeholders until the formal candidates pass Forward+ checkpoint 2. Render closed/open drawers, all ten hollow upper cabinets, hollow lower sliding cabinets, retained sink base without drawers/pipes, waste/manual reach, the east gate and a player-eye view. Request user approval before production integration.

- [ ] **Step 6: Proposed commit boundary after approval and explicit permission**

```powershell
git add tools/blender/build_bar_master.py tools/blender/tests/test_bar_model_contract.py models/bar_counter.glb models/bar_backbar.glb assets/asset_manifest.json
git commit -m "feat: model production U bar and backbar"
```

### Task 9: Model customer furniture, visible fixtures, and glasses-only wear overlays

**Files:**
- Modify: `tools/blender/build_bar_master.py`
- Modify: `tools/blender/tests/test_bar_model_contract.py`
- Create: `models/bar_furniture.glb`
- Create: `models/bar_lighting.glb`
- Create: `models/bar_wear_overlays.glb`
- Modify: `assets/asset_manifest.json`

- [x] **Step 1: Add failing count, dimension, and exclusion tests**

Assert six stools with individual foot rings, three tables, twelve armless chairs each facing its assigned table center, six pendant fixtures (three front-bar and three table-centered lounge pendants), two rear linear housings, four sconces, no customer-zone floor light fixture, and only sparse non-colliding wear meshes.

- [x] **Step 2: Run Blender contract tests and confirm failure**

- [x] **Step 3: Model furniture as independent named objects**

Use warm-oak round stool seats, dark-silver legs and independent rings; warm-oak 0.80 m table tops with dark-silver central pedestals; warm-oak chair seats/broad backs with dark-silver frames and no upholstery. Rotate all twelve chairs toward their assigned table centers. Preserve individual transforms for all 21 loose pieces so Godot clearance/debug tools can identify them.

- [x] **Step 4: Model visible lighting fixtures only**

Model three separable front-bar pendant assemblies plus one pendant centered over each of the three lounge tables, two concealed rear-linear housings, and four simplified low-poly retro sconces. All visible shades use the explicitly labelled `frosted_translucent_shade` material with translucent alpha, transmission and high roughness. The Godot wrapper supplies actual downward SpotLight3D illumination strong enough to light every tabletop without producing ceiling blobs. Invisible ceiling fills remain Godot lights and do not become GLB geometry.

- [x] **Step 5: Model sparse glasses-only wear overlays**

Add named non-colliding chips, rubbed edges, and stain masks as shallow overlay meshes. Keep them sparse, avoid new silhouette-changing damage, and do not duplicate the architecture/counter meshes.

- [x] **Step 6: Export, validate, and request furniture/fixture approval**

Export all three modules while keeping their manifest entries `placeholder: true`, and render table-facing chairs, bar stools, all sconces/pendants, illuminated tabletops, and overlays on/off. Correct route or readability issues before continuing; only Task 11 behavior integration plus checkpoint 2 may authorize placeholder removal.

- [ ] **Step 7: Proposed commit boundary after approval and explicit permission**

```powershell
git add tools/blender/build_bar_master.py tools/blender/tests/test_bar_model_contract.py models/bar_furniture.glb models/bar_lighting.glb models/bar_wear_overlays.glb assets/asset_manifest.json
git commit -m "feat: add bar furniture lighting and wear assets"
```

### Task 10: Apply the approved retro-modern material system

**Files:**
- Modify: `tools/blender/bar_model_common.py`
- Modify: `tools/blender/build_bar_master.py`
- Modify: `tools/blender/render_bar_review.py`
- Replace generated GLBs: `models/bar_architecture.glb`
- Replace generated GLBs: `models/bar_counter.glb`
- Replace generated GLBs: `models/bar_backbar.glb`
- Replace generated GLBs: `models/bar_furniture.glb`
- Replace generated GLBs: `models/bar_lighting.glb`
- Replace generated GLBs: `models/bar_wear_overlays.glb`

- [x] **Step 1: Add failing material-slot contract assertions**

Require named slots for deep-brown cabinet fronts/carcasses, dark walnut tops/racks/wainscot, warm oak floor/tables/chairs/stools, warm-gray mineral plaster, copper/brushed brass/dark silver, and simplified glass. Reject every deep-green material name and assignment.

- [x] **Step 2: Run Blender contract tests and confirm material failures**

- [x] **Step 3: Implement reusable low-poly PBR materials**

Use hand-painted base-color variation and baked/procedural AO suitable for long-distance readability. Keep broad color masses low-saturation, roughness readable, metals controlled, and glass simple. Do not introduce photoreal micro-detail that conflicts with the art document's low-poly retro 3D direction.

- [x] **Step 4: Re-export every module and rerun candidate validation**

Run the Blender contract test, export all six GLBs, validate each generated sidecar strictly, then run the project manifest with `--allow-placeholders`. All six environment entries must remain `placeholder: true` until behavior integration, actual dual-world Forward+ review, and explicit checkpoint-2 approval are complete.

- [x] **Step 5: Render a fixed pre-integration material comparison and STOP for user approval**

Render the same cameras in neutral clay, reality warm palette, and cold/desaturated glasses preview palette with overlays on. Present architecture, bar, rear rack, seating, and fixtures at close and room scales. This is the Task 10 geometry/material pre-review, not the modeling Skill's final checkpoint 2: do not start Godot replacement until the user approves this review, and do not record checkpoint 2 until Task 11 behavior integration proves shared state, fallback, and actual reality/glasses runtime presentation.

- [ ] **Step 6: Proposed commit boundary after approval and explicit permission**

```powershell
git add tools/blender/bar_model_common.py tools/blender/build_bar_master.py tools/blender/render_bar_review.py models/bar_architecture.glb models/bar_counter.glb models/bar_backbar.glb models/bar_furniture.glb models/bar_lighting.glb models/bar_wear_overlays.glb
git commit -m "art: finish retro modern bar material pass"
```

Status 2026-08-11 rework: Tasks 8–10 were rebuilt after review. The current candidate uses one continuous C-shaped bar base/carcass/worktop with inward chamfers and only an east-gate cut; a solid drawer-free sink base with hidden plumbing; an actually open sink bowl with a horizontal rim and short down-angled east-rim faucet; a compact west manual shelf rotated clockwise `90°` from the rejected placement and reduced to `0.62 m × 0.28 m`; dark-brown hollow cabinets; recessed lower sliding leaves; table-facing chairs; and three added lounge pendants with real downward Godot lights and labelled frosted translucent shades. Six GLBs/sidecars and 27 inspected `1920×1080` Forward+ images now cover eight matched cameras in neutral/reality/glasses plus drawer/lower-cabinet/upper-cabinet open states. All six environment entries remain placeholders. This remains Task 10 pre-integration review, not modeling Skill checkpoint 2; Task 11 must not begin until the user approves this comparison.

### Task 11: Add hand-authored Godot wrappers and guarded graybox fallback

**Files:**
- Create: `scripts/world/BarEnvironmentVisualLoader.cs`
- Create: `scripts/world/BarGameplayVisualBinder.cs`
- Create: `scripts/world/BarMaterialVariantController.cs`
- Create: `scripts/world/BarLightRigController.cs`
- Create: `scenes/environment/BarInteriorProduction.tscn`
- Create: `scenes/environment/modules/BarArchitectureVisual.tscn`
- Create: `scenes/environment/modules/BarCounterVisual.tscn`
- Create: `scenes/environment/modules/BarBackbarVisual.tscn`
- Create: `scenes/environment/modules/BarFurnitureVisual.tscn`
- Create: `scenes/environment/modules/BarLightingVisual.tscn`
- Create: `scenes/environment/modules/BarWearOverlaysVisual.tscn`
- Modify: `scripts/world/GrayboxLevelBuilder.cs`
- Modify: `scripts/gameplay/CabinetInteractable.cs`
- Create: `tests/godot/BarProductionAssetIntegrationTests.cs`
- Create: `tests/godot/BarProductionAssetIntegrationTests.tscn`

- [ ] **Step 1: Write failing production-load, fallback, and ownership tests**

Test successful load with all six modules, forced missing-module fallback, one copy of each moving gameplay stable ID, no collisions under imported visual roots, and no real lights embedded in imported GLBs. Require `BAR_PRODUCTION_ASSET_INTEGRATION_PASS`.

- [ ] **Step 2: Run the integration scene and confirm it fails before wrappers exist**

- [ ] **Step 3: Implement a loader with an all-or-graybox result**

Use this explicit surface:

```csharp
public sealed class BarEnvironmentVisualLoader
{
    public bool TryInstantiate(
        Node3D neutralGameplay,
        Node3D realityWorld,
        Node3D glassesWorld,
        out BarEnvironmentVisualSet visualSet);
}
```

The loader first verifies every PackedScene/resource and required node. If any module fails, free every partially created production instance, return `false`, and let the caller build complete graybox visuals. Never mix half-production and half-graybox room modules.

- [ ] **Step 4: Keep static and moving modules in the correct owners**

Instance architecture/furniture/visible-light geometry twice from the same GLB resources, once under `RealityWorld` and once under `GlassesWorld`. Instance counter/backbar only once under `NeutralGameplay`. Instance wear overlays once under `NeutralGameplay`, with collision disabled and visibility controlled by world mode.

- [ ] **Step 5: Bind detachable imported visuals to authoritative interactables**

`BarGameplayVisualBinder` resolves stable child names, reparents each imported drawer/door/manual visual under its existing authoritative gameplay node, resets local transform to the contract pose, and calls a new `CabinetInteractable.SetProductionVisual(Node3D visual)` that hides only the generated graybox panel/tray/handle. It must not create or own open/closed state.

- [ ] **Step 6: Put real Godot lights and collisions in wrappers**

Create static collision shapes from `BarLayoutDefinition`; create pendants, rear linear lights, four sconces, and two invisible customer fills in `BarLightRigController`. Fixture GLBs are meshes only. Keep the existing world toggle owner and avoid duplicate directional/global lights.

- [ ] **Step 7: Wire guarded loading into the composition root**

```csharp
architecture.BuildCollisions();
if (!new BarEnvironmentVisualLoader().TryInstantiate(neutral, reality, glasses, out _))
    architecture.BuildGrayboxVisuals();
```

Build gameplay stations/cabinets/tools regardless of visual result.

- [ ] **Step 8: Run production and forced-fallback tests**

Run asset integration, smoke, input, and flow tests once with all assets and once with the loader test hook forcing fallback. Both modes must preserve stable paths and gameplay behavior.

- [ ] **Step 9: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/BarEnvironmentVisualLoader.cs scripts/world/BarGameplayVisualBinder.cs scripts/world/BarMaterialVariantController.cs scripts/world/BarLightRigController.cs scripts/world/GrayboxLevelBuilder.cs scripts/gameplay/CabinetInteractable.cs scenes/environment tests/godot/BarProductionAssetIntegrationTests.cs tests/godot/BarProductionAssetIntegrationTests.tscn
git commit -m "feat: integrate production bar with graybox fallback"
```

### Task 12: Implement dual-world material/light switching without duplicated state

**Files:**
- Modify: `scripts/world/BarMaterialVariantController.cs`
- Modify: `scripts/world/BarLightRigController.cs`
- Modify: `scripts/world/BarGameplayVisualBinder.cs`
- Modify: `tests/godot/BarProductionAssetIntegrationTests.cs`
- Modify: `tests/godot/FlowIntegrationTests.cs`

- [ ] **Step 1: Add failing world-toggle assertions**

Assert that toggling glasses preserves drawer/door/manual transforms, swaps the shared counter/backbar materials without replacing nodes, hides reality static instances, shows glasses static instances, toggles only the wear overlay, changes customer/bar light color-energy settings, and leaves every imported overlay outside collision and interaction groups.

- [ ] **Step 2: Run the integration test and confirm material/state failures**

- [ ] **Step 3: Implement material variant application**

Duplicate imported `StandardMaterial3D` resources per instance before editing. Preserve texture assignments; apply warm reality colors and cold/desaturated glasses colors by named material slot. `BarMaterialVariantController.ApplyMode(WorldMode mode)` updates the shared neutral counter/backbar and overlay visibility only; static world copies remain controlled by `WorldLayerController`.

- [ ] **Step 4: Implement light variant application**

`BarLightRigController.ApplyMode(WorldMode mode)` switches temperature/color, energy, and shadow settings for the same logical fixture groups. It must not add or remove lights during a toggle and must not make the invisible fills visible as geometry.

- [ ] **Step 5: Run full regression twice**

Run `tools/run_verification.ps1` with production assets, then invoke the forced-fallback integration scene. Require unchanged gameplay phase, held tools, cabinet state, world mode, and reset behavior.

- [ ] **Step 6: Proposed commit boundary after explicit permission**

```powershell
git add scripts/world/BarMaterialVariantController.cs scripts/world/BarLightRigController.cs scripts/world/BarGameplayVisualBinder.cs tests/godot/BarProductionAssetIntegrationTests.cs tests/godot/FlowIntegrationTests.cs
git commit -m "feat: switch bar presentation across glasses worlds"
```

### Task 13: Perform final Forward+ visual QA, regression, and handoff

**Files:**
- Modify: `tests/godot/BarProductionVisualCapture.cs`
- Modify: `tests/godot/BarProductionVisualCapture.tscn`
- Modify: `tools/run_verification.ps1`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `docs/CONTEXT_HANDOFF.md`
- Create or modify: `progress.md`

- [ ] **Step 1: Add final capture states for production and fallback**

Capture the same approved camera set in reality production, glasses production, and forced graybox fallback. Add open drawer, open upper door, manual picked up, chairs pulled out, entry/window, and player-eye frames. Use deterministic exposure and no HUD/menu obstruction.

- [ ] **Step 2: Run the complete automated verification**

```powershell
python tools/validate_assets.py --self-test
python tools/validate_assets.py assets/asset_manifest.json
powershell -ExecutionPolicy Bypass -File tools/run_verification.ps1
```

Expected: strict asset validation, Debug/Release builds, domain tests, layout contract, asset integration, smoke, input, and flow tests all pass with explicit PASS tokens and no warnings introduced by this work.

- [ ] **Step 3: Render and inspect final Forward+ evidence**

```powershell
New-Item -ItemType Directory -Force artifacts/visual_review_bar_production | Out-Null
& 'D:\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path 'D:\眼镜酒馆开发' --write-movie 'artifacts/visual_review_bar_production/frame.png' --fixed-fps 1 --quit-after 18 res://tests/godot/BarProductionVisualCapture.tscn
```

Inspect every frame for orientation, silhouette, clipped geometry, door/drawer pivots, manual pickup, sink/waste clearances, table/chair routes, stool alignment, fixture placement, material legibility, light balance, glasses overlay restraint, and absence of bar-attached footrails. Record filenames and SHA-256 hashes.

- [ ] **Step 4: Fix every visual defect and repeat automated plus visual verification**

Do not declare completion from parameters alone. Any geometry/material/light correction requires rerunning the affected Blender test/export/validator and the final Godot capture.

- [ ] **Step 5: Update project records**

Update `PROJECT_STATUS.md`, `CHANGELOG.md`, `CONTEXT_HANDOFF.md`, and root `progress.md` with: completed items, approved decisions, exact verification results, visual evidence paths/hashes, retained graybox fallback, deferred manual animations/reading, deferred customer AI/backstage, and any remaining work.

- [ ] **Step 6: Request final user acceptance**

Present the final production and glasses screenshots beside the approved graybox reference. Clearly distinguish completed, deferred, and unapproved scope.

- [ ] **Step 7: Proposed final commit after user acceptance and explicit commit permission**

```powershell
git add tests/godot/BarProductionVisualCapture.cs tests/godot/BarProductionVisualCapture.tscn tools/run_verification.ps1 docs/PROJECT_STATUS.md docs/CHANGELOG.md docs/CONTEXT_HANDOFF.md progress.md
git add assets/asset_manifest.json models scenes/environment scripts/world scripts/gameplay/CabinetInteractable.cs tools/blender tools/validate_assets.py tests/godot/BarProductionLayoutContractTests.cs tests/godot/BarProductionLayoutContractTests.tscn tests/godot/BarProductionAssetIntegrationTests.cs tests/godot/BarProductionAssetIntegrationTests.tscn tests/godot/BarLayoutVisualCapture.cs tests/godot/BarLayoutVisualCapture.tscn scenes/Main.tscn
git status --short
git commit -m "feat: replace graybox with production bar interior"
```

Before committing, inspect `git status --short` and confirm `export_presets.cfg` is not staged.

## Completion Criteria

- The in-game graybox and final production model both use the approved orientation and interaction scale, with explicit user approval at both gates.
- Strict asset validation passes for all six environment GLBs; all transforms, roots, required nodes, and anchors satisfy the handoff contract.
- Production visuals replace the graybox only when the complete module set loads; forced fallback remains fully playable.
- Reality/glasses switching changes presentation without duplicating or resetting gameplay state.
- All automated suites pass and actual Forward+ screenshots have been inspected.
- Documentation and `progress.md` contain completed work, key decisions, verification evidence, and deferred items.
- No unrelated user modification is staged or committed.
