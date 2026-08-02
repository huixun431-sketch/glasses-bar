import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


VALID_CONFIG = {
    "batch_id": "test-fixed-stations",
    "stage": "test",
    "assets": [
        {
            "asset_id": "test_kettle",
            "runtime_id": "kettle",
            "required_anchors": ["Placement", "Spout", "Interaction"],
            "interaction_kind": "fixed_station",
        },
        {
            "asset_id": "test_bin",
            "runtime_id": "waste_bin",
            "required_anchors": ["Placement", "Interaction"],
            "interaction_kind": "fixed_station",
        },
    ],
    "paths": {
        "candidate_root": "artifacts/test-fixed-stations",
        "formal_model_root": "assets/models",
        "wrapper_root": "scenes/assets/test-fixed-stations",
        "batch_record": "docs/assets/TEST_FIXED_STATIONS_ASSET_BATCH.md",
        "json_manifest": "assets/asset_manifest.json",
    },
    "checkpoints": {
        "silhouette": {"status": "pending", "evidence": []},
        "forward_plus": {"status": "pending", "evidence": []},
    },
}


PROJECT_ROOT = Path(__file__).resolve().parents[2]
SKILL_ROOT = PROJECT_ROOT / ".agents" / "skills" / "modeling-glasses-bar-assets"
SKILL_SCRIPTS = SKILL_ROOT / "scripts"
TEMPLATE_ROOT = SKILL_ROOT / "assets" / "templates"


def import_skill_script(module_name: str):
    script_path = SKILL_SCRIPTS / f"{module_name}.py"
    if not script_path.is_file():
        raise FileNotFoundError(script_path)

    spec = importlib.util.spec_from_file_location(module_name, script_path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ModelingSkillFrameworkTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.initializer = import_skill_script("init_asset_batch")
        cls.validator = import_skill_script("validate_asset_batch")

    def test_valid_fixed_station_batch_imports_scripts_and_validates(self):
        self.assertEqual(self.initializer.validate_config(VALID_CONFIG), [])
        self.assertEqual(self.validator.validate_config(VALID_CONFIG), [])

    def test_invalid_configuration_accumulates_independent_errors(self):
        invalid = json.loads(json.dumps(VALID_CONFIG))
        invalid["assets"][1]["asset_id"] = "test_kettle"
        invalid["assets"][1]["required_anchors"] = ["Bad Anchor!"]
        invalid["paths"]["candidate_root"] = "C:/outside"
        invalid["checkpoints"]["forward_plus"]["status"] = "done"

        errors = self.initializer.validate_config(invalid)

        self.assertTrue(any("duplicates" in error for error in errors))
        self.assertTrue(any("invalid anchor" in error for error in errors))
        self.assertTrue(any("candidate_root" in error and "absolute" in error for error in errors))
        self.assertTrue(any("forward_plus.status" in error for error in errors))

    def test_render_outputs_is_deterministic_and_replaces_all_template_tokens(self):
        first = self.initializer.render_outputs(VALID_CONFIG, TEMPLATE_ROOT)
        second = self.initializer.render_outputs(VALID_CONFIG, TEMPLATE_ROOT)

        self.assertEqual(first, second)
        self.assertEqual(len(first), 9)
        self.assertTrue(all("${" not in content for content in first.values()))
        self.assertTrue(any(path.name.endswith("AssetIntegrationTests.cs") for path in first))
        self.assertTrue(any(path.name.endswith("AssetVisualCapture.tscn") for path in first))

    def test_atomic_write_rejects_any_existing_destination_without_partial_output(self):
        rendered = self.initializer.render_outputs(VALID_CONFIG, TEMPLATE_ROOT)
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            conflict = root / next(iter(rendered))
            conflict.parent.mkdir(parents=True, exist_ok=True)
            conflict.write_text("owned", encoding="utf-8")

            with self.assertRaises(FileExistsError):
                self.initializer._write_atomically(root, rendered)

            self.assertEqual(conflict.read_text(encoding="utf-8"), "owned")
            self.assertEqual(
                [path for path in root.rglob("*") if path.is_file()],
                [conflict],
            )

    def test_design_phase_accepts_valid_config_without_generated_outputs(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            errors = self.validator.validate_batch(
                VALID_CONFIG,
                Path(temporary_directory),
                "design",
                tracked_files=[],
            )

        self.assertEqual(errors, [])

    def test_silhouette_phase_rejects_formal_outputs_and_missing_evidence(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            formal = root / "assets/models/test_kettle.glb"
            formal.parent.mkdir(parents=True, exist_ok=True)
            formal.write_bytes(b"glTF")

            errors = self.validator.validate_batch(
                VALID_CONFIG,
                root,
                "silhouette-review",
                tracked_files=[],
            )

        self.assertTrue(any(error.startswith("FORMAL_BEFORE_APPROVAL:") for error in errors))
        self.assertTrue(any(error.startswith("SILHOUETTE_EVIDENCE_MISSING:") for error in errors))
        self.assertTrue(any(error.startswith("SKELETON_MISSING:") for error in errors))

    def test_validator_rejects_tracked_review_artifacts_and_import_metadata(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            errors = self.validator.validate_batch(
                VALID_CONFIG,
                Path(temporary_directory),
                "silhouette-review",
                tracked_files=[
                    "artifacts/review/frame.png",
                    "artifacts/review/master.blend",
                    "assets/models/test_kettle.glb.import",
                ],
            )

        self.assertTrue(any(error.startswith("TRACKED_ARTIFACT:") for error in errors))
        self.assertTrue(any(error.startswith("TRACKED_SCREENSHOT:") for error in errors))
        self.assertTrue(any(error.startswith("TRACKED_BLEND:") for error in errors))
        self.assertTrue(any(error.startswith("TRACKED_IMPORT_METADATA:") for error in errors))


if __name__ == "__main__":
    unittest.main()
