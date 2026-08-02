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
SKILL_SCRIPTS = PROJECT_ROOT / ".agents" / "skills" / "modeling-glasses-bar-assets" / "scripts"


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
    def test_valid_fixed_station_batch_imports_scripts_and_validates(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            config_path = Path(temporary_directory) / "batch.json"
            config_path.write_text(json.dumps(VALID_CONFIG), encoding="utf-8")

            init_asset_batch = import_skill_script("init_asset_batch")
            validate_asset_batch = import_skill_script("validate_asset_batch")

            self.assertTrue(config_path.is_file())
            self.assertTrue(hasattr(init_asset_batch, "main"))
            self.assertEqual(validate_asset_batch.validate_config(VALID_CONFIG), [])
