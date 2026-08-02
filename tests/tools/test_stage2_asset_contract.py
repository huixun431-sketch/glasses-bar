import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools" / "modeling"))
from stage2_asset_contract import (
    AssetContract,
    STAGE2_ASSETS,
    review_manifest_assets,
    validate_contracts,
)


class Stage2AssetContractTests(unittest.TestCase):
    def test_approved_contracts_validate_and_emit_review_manifest_entries(self):
        self.assertEqual(validate_contracts(STAGE2_ASSETS), [])
        entries = review_manifest_assets("models")
        self.assertEqual([entry["id"] for entry in entries], list(STAGE2_ASSETS))
        self.assertTrue(all(entry["placeholder"] is False for entry in entries))
        self.assertEqual(
            entries[0],
            {
                "id": "traditional_filter",
                "path": "models/traditional_filter.glb",
                "placeholder": False,
                "required_anchors": ["Grip", "Placement", "Spout", "Interaction"],
            },
        )

    def test_validation_rejects_invalid_hand_envelope_anchor_and_material_group(self):
        broken = {
            "broken": AssetContract(
                "broken", (0.2, 0.0, 0.1), ("Grip",), "center", "painted_plastic"
            )
        }
        self.assertEqual(
            validate_contracts(broken),
            [
                "broken: envelope dimensions must be positive meters",
                "broken: Placement anchor is required",
                "broken: hand must be left or right",
                "broken: unknown material group painted_plastic",
            ],
        )
