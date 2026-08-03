"""Generated contract checks for bar-architecture; not executed by the Skill initializer."""

import unittest

from tools.modeling.bar_architecture_asset_contract import ASSETS, approved_contracts


class BarArchitectureAssetContractTests(unittest.TestCase):
    def test_every_stable_asset_has_an_approved_contract_and_required_anchors(self):
        contracts = approved_contracts()
        self.assertEqual({asset["asset_id"] for asset in ASSETS}, set(contracts))
        for asset in ASSETS:
            asset_id = asset["asset_id"]
            self.assertTrue(asset["required_anchors"], asset_id)
            self.assertEqual(
                tuple(asset["required_anchors"]),
                tuple(contracts[asset_id]["required_anchors"]),
                asset_id,
            )
            self.assertGreater(len(contracts[asset_id]), 0, asset_id)
        architecture = contracts["bar_architecture"]
        self.assertEqual((16.0, 10.0, 4.5), architecture["room_size_m"])
        self.assertTrue(architecture["visual_only"])
        self.assertEqual(
            ("room_shell", "south_main_entry", "south_east_window", "north_east_service_door"),
            architecture["required_nodes"],
        )


if __name__ == "__main__":
    unittest.main()
