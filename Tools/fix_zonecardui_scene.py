# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / "Assets" / "Scenes" / "ZoneSelection.unity"


def spr(g):
    return "{fileID: 21300000, guid: %s, type: 3}" % g


def main():
    t = SCENE.read_text(encoding="utf-8")
    if "thumbnailImage:" in t and "900000202" in t:
        print("ZoneCardUI ya enlazado; sin cambios.")
        return

    old1 = r"""  cardCanvasGroup: {fileID: 291605883}
  title: "Zona 1 \u2013 Calabozos"
  description: "Produce almas y energ\xEDa pura.\nPrimer territorio del jugador."
  lockedHint: 
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  uiAudio: {fileID: 291605884}"""

    new1 = (
        r"""  cardCanvasGroup: {fileID: 291605883}
  thumbnailImage: {fileID: 900000202}
  lockSealImage: {fileID: 900000212}
  unlockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000004")
        + r"""
  lockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000005")
        + r"""
  zoneThumbnailSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000006")
        + r"""
  title: "Zona 1 \u2013 Calabozos"
  description: "Produce almas y energ\xEDa pura.\nPrimer territorio del jugador."
  lockedHint: 
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  unlockedImageTint: {r: 1, g: 1, b: 1, a: 1}
  lockedImageTint: {r: 0.78, g: 0.74, b: 0.86, a: 1}
  uiAudio: {fileID: 291605884}"""
    )

    old2 = r"""  cardCanvasGroup: {fileID: 1370683663}
  title: "Zona 2 \u2013 Ciudades"
  description: "Pr\xF3ximo frente de culto y tributo."
  lockedHint: Bloqueada. Requiere progreso en Zona 1.
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  uiAudio: {fileID: 1370683664}"""

    new2 = (
        r"""  cardCanvasGroup: {fileID: 1370683663}
  thumbnailImage: {fileID: 900000222}
  lockSealImage: {fileID: 900000232}
  unlockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000004")
        + r"""
  lockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000005")
        + r"""
  zoneThumbnailSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000007")
        + r"""
  title: "Zona 2 \u2013 Ciudades"
  description: "Pr\xF3ximo frente de culto y tributo."
  lockedHint: Bloqueada. Requiere progreso en Zona 1.
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  unlockedImageTint: {r: 1, g: 1, b: 1, a: 1}
  lockedImageTint: {r: 0.78, g: 0.74, b: 0.86, a: 1}
  uiAudio: {fileID: 1370683664}"""
    )

    old3 = r"""  cardCanvasGroup: {fileID: 903014747}
  title: "Zona 3 \u2013 Cuerpos Celestes"
  description: "El cielo tambi\xE9n debe arder."
  lockedHint: Bloqueada. Requiere progreso en Zona 2.
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  uiAudio: {fileID: 903014746}"""

    new3 = (
        r"""  cardCanvasGroup: {fileID: 903014747}
  thumbnailImage: {fileID: 900000242}
  lockSealImage: {fileID: 900000252}
  unlockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000004")
        + r"""
  lockedCardSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000005")
        + r"""
  zoneThumbnailSprite: """
        + spr("a1b2c3d4e5f6789012345678ab000008")
        + r"""
  title: "Zona 3 \u2013 Cuerpos Celestes"
  description: "El cielo tambi\xE9n debe arder."
  lockedHint: Bloqueada. Requiere progreso en Zona 2.
  unlockedTint: {r: 0.42, g: 0.36, b: 0.52, a: 0.95}
  lockedTint: {r: 0.18, g: 0.14, b: 0.22, a: 0.88}
  unlockedImageTint: {r: 1, g: 1, b: 1, a: 1}
  lockedImageTint: {r: 0.78, g: 0.74, b: 0.86, a: 1}
  uiAudio: {fileID: 903014746}"""
    )

    for name, old, new in (
        ("z1", old1, new1),
        ("z2", old2, new2),
        ("z3", old3, new3),
    ):
        if old not in t:
            raise SystemExit(f"No match {name}")
        t = t.replace(old, new, 1)

    SCENE.write_text(t, encoding="utf-8")
    print("ZoneCardUI OK")


if __name__ == "__main__":
    main()
