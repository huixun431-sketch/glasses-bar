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
        missing_anchors = sorted(set(entry.get("required_anchors", [])) - names)
        if missing_anchors:
            messages.append(f"ERROR {asset_id}: missing anchors {', '.join(missing_anchors)}")
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


def write_test_glb(path: Path, *, good: bool) -> None:
    payload = {
        "asset": {"version": "2.0"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": ([{"name": "Root", "mesh": 0}, {"name": "Grip"}, {"name": "Placement"}] if good
                  else [{"name": "Root", "mesh": 0, "scale": [2, 2, 2]}]),
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
        write_test_glb(root / "good.glb", good=True)
        write_test_glb(root / "bad.glb", good=False)
        good_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "good", "path": "good.glb", "placeholder": False,
                        "required_anchors": ["Grip", "Placement"]}],
        }
        bad_manifest = {
            "units": "meters", "up_axis": "+Y", "forward_axis": "-Z",
            "assets": [{"id": "bad", "path": "bad.glb", "placeholder": False,
                        "required_anchors": ["Grip"]}],
        }
        good_path = root / "good.json"
        bad_path = root / "bad.json"
        good_path.write_text(json.dumps(good_manifest), encoding="utf-8")
        bad_path.write_text(json.dumps(bad_manifest), encoding="utf-8")
        good_lines = validate_manifest(good_path, False)
        bad_lines = validate_manifest(bad_path, False)
        good_ok = good_lines[-1].endswith("errors=0")
        bad_ok = not bad_lines[-1].endswith("errors=0")
        print("SELFTEST good_manifest", "PASS" if good_ok else "FAIL")
        print("SELFTEST bad_manifest", "PASS" if bad_ok else "FAIL")
        return 0 if good_ok and bad_ok else 1


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

