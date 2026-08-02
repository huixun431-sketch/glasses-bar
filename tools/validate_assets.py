#!/usr/bin/env python3
"""Read-only GLB/manifest validator for Glasses Bar asset handoff."""

from __future__ import annotations

import argparse
import json
import struct
import sys
import tempfile
from pathlib import Path

MAGIC = b"glTF"
JSON_CHUNK = 0x4E4F534A


def read_glb(path: Path) -> dict:
    data = path.read_bytes()
    if len(data) < 20:
        raise ValueError("file is too small to be a GLB")
    magic, version, declared_length = struct.unpack_from("<4sII", data, 0)
    if magic != MAGIC:
        raise ValueError("invalid GLB magic")
    if version != 2:
        raise ValueError(f"unsupported GLB version {version}; expected 2")
    if declared_length != len(data):
        raise ValueError(f"declared length {declared_length} does not match file length {len(data)}")

    offset = 12
    while offset + 8 <= len(data):
        chunk_length, chunk_type = struct.unpack_from("<II", data, offset)
        offset += 8
        chunk = data[offset : offset + chunk_length]
        offset += chunk_length
        if chunk_type == JSON_CHUNK:
            return json.loads(chunk.rstrip(b" \x00").decode("utf-8"))
    raise ValueError("missing JSON chunk")


def validate_manifest(manifest_path: Path, allow_placeholders: bool) -> list[str]:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    messages: list[str] = []
    errors = 0
    seen: set[str] = set()

    if manifest.get("units") != "meters" or manifest.get("up_axis") != "+Y" or manifest.get("forward_axis") != "-Z":
        messages.append("ERROR manifest axes/units must be meters, +Y up, -Z forward")
        errors += 1

    for entry in manifest.get("assets", []):
        asset_id = entry.get("id", "")
        if not asset_id or asset_id in seen:
            messages.append(f"ERROR invalid or duplicate asset id: {asset_id!r}")
            errors += 1
            continue
        seen.add(asset_id)

        is_placeholder = bool(entry.get("placeholder", False))
        path = manifest_path.parent / entry.get("path", "")
        if is_placeholder:
            level = "INFO" if allow_placeholders else "ERROR"
            messages.append(f"{level} {asset_id}: graybox placeholder")
            errors += 0 if allow_placeholders else 1
            continue
        if not path.is_file():
            messages.append(f"ERROR {asset_id}: missing {path}")
            errors += 1
            continue

        try:
            gltf = read_glb(path)
        except Exception as exc:  # validator must report one asset without aborting the batch
            messages.append(f"ERROR {asset_id}: {exc}")
            errors += 1
            continue

        nodes = gltf.get("nodes", [])
        names = {node.get("name", "") for node in nodes}
        scenes = gltf.get("scenes", [])
        scene_index = gltf.get("scene", 0)
        root_indices = scenes[scene_index].get("nodes", []) if 0 <= scene_index < len(scenes) else []
        root_names = {
            nodes[index].get("name", "")
            for index in root_indices
            if 0 <= index < len(nodes)
        }
        required_root = entry.get("required_root", asset_id)
        if required_root not in root_names:
            messages.append(f"ERROR {asset_id}: scene root must be named {required_root}")
            errors += 1
        missing_anchors = sorted(set(entry.get("required_anchors", [])) - names)
        if missing_anchors:
            messages.append(f"ERROR {asset_id}: missing anchors {', '.join(missing_anchors)}")
            errors += 1
        missing_nodes = sorted(set(entry.get("required_nodes", [])) - names)
        if missing_nodes:
            messages.append(f"ERROR {asset_id}: missing required nodes {', '.join(missing_nodes)}")
            errors += 1

        identity_matrix = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]
        for index in root_indices:
            if not 0 <= index < len(nodes):
                continue
            node = nodes[index]
            if (node.get("translation", [0, 0, 0]) != [0, 0, 0] or
                    node.get("rotation", [0, 0, 0, 1]) != [0, 0, 0, 1] or
                    node.get("scale", [1, 1, 1]) != [1, 1, 1] or
                    node.get("matrix", identity_matrix) != identity_matrix):
                messages.append(
                    f"ERROR {asset_id}: non-identity transform on node {node.get('name', '<unnamed>')}"
                )
                errors += 1

        for node in nodes:
            if node.get("scale", [1, 1, 1]) != [1, 1, 1]:
                messages.append(f"ERROR {asset_id}: unapplied scale on node {node.get('name', '<unnamed>')}")
                errors += 1

        if not gltf.get("meshes"):
            messages.append(f"ERROR {asset_id}: no mesh")
            errors += 1
        if not gltf.get("materials"):
            messages.append(f"ERROR {asset_id}: no material")
            errors += 1
        if not any(line.startswith(f"ERROR {asset_id}:") for line in messages):
            messages.append(f"OK {asset_id}: GLB structure and anchors accepted")

    messages.append(f"SUMMARY assets={len(seen)} errors={errors}")
    return messages


def write_test_glb(
    path: Path,
    *,
    root_name: str,
    node_names: tuple[str, ...] = ("Grip", "Placement"),
    transform: dict | None = None,
) -> None:
    root_node = {"name": root_name, "mesh": 0}
    if transform:
        root_node.update(transform)
    payload = {
        "asset": {"version": "2.0"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [root_node, *({"name": name} for name in node_names)],
        "meshes": [{"primitives": []}],
        "materials": [{"name": "PBR"}],
    }
    chunk = json.dumps(payload, separators=(",", ":")).encode("utf-8")
    chunk += b" " * ((4 - len(chunk) % 4) % 4)
    total = 12 + 8 + len(chunk)
    path.write_bytes(struct.pack("<4sII", MAGIC, 2, total) + struct.pack("<II", len(chunk), JSON_CHUNK) + chunk)


def self_test() -> int:
    with tempfile.TemporaryDirectory(prefix="glasses_bar_asset_test_") as tmp:
        root = Path(tmp)
        write_test_glb(root / "good.glb", root_name="contract_root")
        write_test_glb(root / "wrong_root.glb", root_name="WrongRoot")
        write_test_glb(root / "missing_node.glb", root_name="missing_node_root", node_names=("Grip",))
        write_test_glb(root / "non_identity.glb", root_name="non_identity_root",
                       transform={"translation": [0.25, 0, 0]})
        good_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "good", "path": "good.glb", "placeholder": False,
                        "required_root": "contract_root",
                        "required_anchors": ["Grip"],
                        "required_nodes": ["Placement"]}],
        }
        wrong_root_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "wrong_root_asset", "path": "wrong_root.glb", "placeholder": False,
                        "required_root": "ExpectedRoot", "required_nodes": ["Grip"]}],
        }
        missing_node_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "missing_node_asset", "path": "missing_node.glb", "placeholder": False,
                        "required_root": "missing_node_root", "required_nodes": ["MissingNode"]}],
        }
        non_identity_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "non_identity_asset", "path": "non_identity.glb", "placeholder": False,
                        "required_root": "non_identity_root"}],
        }
        good_path = root / "good.json"
        wrong_root_path = root / "wrong_root.json"
        missing_node_path = root / "missing_node.json"
        non_identity_path = root / "non_identity.json"
        good_path.write_text(json.dumps(good_manifest), encoding="utf-8")
        wrong_root_path.write_text(json.dumps(wrong_root_manifest), encoding="utf-8")
        missing_node_path.write_text(json.dumps(missing_node_manifest), encoding="utf-8")
        non_identity_path.write_text(json.dumps(non_identity_manifest), encoding="utf-8")
        good_lines = validate_manifest(good_path, False)
        wrong_root_lines = validate_manifest(wrong_root_path, False)
        missing_node_lines = validate_manifest(missing_node_path, False)
        non_identity_lines = validate_manifest(non_identity_path, False)
        good_ok = good_lines[-1].endswith("errors=0")
        wrong_root_ok = any(
            line == "ERROR wrong_root_asset: scene root must be named ExpectedRoot"
            for line in wrong_root_lines
        )
        missing_node_ok = any(
            line == "ERROR missing_node_asset: missing required nodes MissingNode"
            for line in missing_node_lines
        )
        non_identity_ok = any(
            line == "ERROR non_identity_asset: non-identity transform on node non_identity_root"
            for line in non_identity_lines
        )
        print("SELFTEST good_manifest", "PASS" if good_ok else "FAIL")
        print("SELFTEST wrong_root_manifest", "PASS" if wrong_root_ok else "FAIL")
        print("SELFTEST missing_node_manifest", "PASS" if missing_node_ok else "FAIL")
        print("SELFTEST non_identity_manifest", "PASS" if non_identity_ok else "FAIL")
        return 0 if good_ok and wrong_root_ok and missing_node_ok and non_identity_ok else 1


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", nargs="?", type=Path)
    parser.add_argument("--allow-placeholders", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    if args.manifest is None:
        parser.error("manifest is required unless --self-test is used")
    lines = validate_manifest(args.manifest.resolve(), args.allow_placeholders)
    print("\n".join(lines))
    return 1 if lines[-1].split("errors=")[-1] != "0" else 0


if __name__ == "__main__":
    sys.exit(main())

