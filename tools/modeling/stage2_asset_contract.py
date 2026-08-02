from dataclasses import dataclass
from types import MappingProxyType


@dataclass(frozen=True)
class AssetContract:
    asset_id: str
    envelope: tuple[float, float, float]  # width, height, depth in meters
    anchors: tuple[str, ...]
    hand: str
    material_group: str


STAGE2_ASSETS = MappingProxyType(
    {
        "traditional_filter": AssetContract(
            "traditional_filter",
            (0.36, 0.24, 0.30),
            ("Grip", "Placement", "Spout", "Interaction"),
            "left",
            "warm_brushed",
        ),
        "bean_scoop": AssetContract(
            "bean_scoop",
            (0.18, 0.08, 0.34),
            ("Grip", "Placement", "FillOrigin"),
            "right",
            "dark_satin",
        ),
        "ice_tongs": AssetContract(
            "ice_tongs",
            (0.10, 0.08, 0.46),
            ("Grip", "Placement", "Interaction"),
            "right",
            "dark_satin",
        ),
        "jigger_small": AssetContract(
            "jigger_small",
            (0.11, 0.15, 0.11),
            ("Grip", "Placement", "FillOrigin", "Spout"),
            "right",
            "bright_silver",
        ),
        "jigger_large": AssetContract(
            "jigger_large",
            (0.15, 0.21, 0.15),
            ("Grip", "Placement", "FillOrigin", "Spout"),
            "right",
            "bright_silver",
        ),
    }
)

MATERIAL_GROUPS = {"warm_brushed", "dark_satin", "bright_silver"}


def validate_contracts(contracts: dict[str, AssetContract]) -> list[str]:
    errors: list[str] = []
    for key, contract in contracts.items():
        if contract.asset_id != key:
            errors.append(f"{key}: asset_id must match mapping key")
        if any(dimension <= 0 for dimension in contract.envelope):
            errors.append(f"{key}: envelope dimensions must be positive meters")
        if "Placement" not in contract.anchors:
            errors.append(f"{key}: Placement anchor is required")
        if contract.hand not in {"left", "right"}:
            errors.append(f"{key}: hand must be left or right")
        if contract.material_group not in MATERIAL_GROUPS:
            errors.append(f"{key}: unknown material group {contract.material_group}")
    return errors


def review_manifest_assets(model_prefix: str) -> list[dict]:
    return [
        {
            "id": contract.asset_id,
            "path": f"{model_prefix}/{contract.asset_id}.glb",
            "placeholder": False,
            "required_anchors": list(contract.anchors),
        }
        for contract in STAGE2_ASSETS.values()
    ]
