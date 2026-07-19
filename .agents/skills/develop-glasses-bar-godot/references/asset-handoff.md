# Asset handoff contract

- Prefer GLB, meter scale, applied transforms, `+Y` up, and `-Z` forward.
- Require stable asset IDs and PBR materials.
- Use functional anchors named `Grip`, `Placement`, `Spout`, `FillOrigin`, and `Interaction` where relevant.
- Keep imported geometry below a hand-authored gameplay wrapper scene.
- Map one asset ID to placeholder and production paths through the asset manifest.
- Reuse geometry with an eye-world material override unless the silhouette changes.
- Validate scale, anchors, meshes, materials, textures, collision expectations, and manifest paths before acceptance.
- Preserve the graybox fallback until the replacement passes runtime and visual review.

