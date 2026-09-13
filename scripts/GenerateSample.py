from pathlib import Path
import hashlib
import json
import uuid

ROOT = Path(__file__).resolve().parent.parent / "Samples" / "BeaconRun"
for directory in ("Content/Scenes", "Content/Prefabs", "GameCode", ".Veris"):
    (ROOT / directory).mkdir(parents=True, exist_ok=True)


def write(path, document):
    (ROOT / path).write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")


def asset_id(path):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "veris:beacon-run:" + path))


def sprite(texture, origin=(.5, .5), tint=0xFFFFFFFF, layer=0, rectangle=None):
    path = "Textures/" + texture
    return dict(texture=path, assetId=asset_id(path), origin=origin, tint=tint, layer=layer, visible=True, rectangle=rectangle)


def body(kind=0, sensor=False, half=(.5, .5)):
    return dict(type=kind, shape=0, halfSize=half, radius=.5, offset=[0, 0], velocity=[0, 0], angularVelocity=0,
                gravityScale=0, density=1, friction=0, restitution=0, fixedRotation=True, bullet=False,
                sensor=sensor, category=1, mask=65535, group=0)


def entity(entities, name, position=(0, 0), scale=(1, 1), visual=None, physics=None, script=None, parent=0):
    record = dict(id=len(entities) + 1, name=name, parent=parent, transform=dict(position=position, scale=scale, rotation=0), sprite=visual)
    if physics is not None:
        record["physics"] = physics
    if script:
        record["script"] = dict(path=script, enabled=True)
    entities.append(record)
    return record


for screen, title, script in (("title", "Title screen", "menu.lua"), ("win", "Victory screen", "end.lua"), ("lose", "Defeat screen", "end.lua")):
    entities = []
    entity(entities, title, (480, 320), visual=sprite(screen + ".png", (480, 320)), script=script)
    write("Content/Scenes/" + screen + ".verisscene", dict(formatVersion=1, entities=entities))

entities = []
entity(entities, "Floor", (480, 320), (960, 640), sprite("white.png", tint=0x223047FF))
entity(entities, "Level controller", script="level.lua")
for name, position, scale in (("North wall", (480, 92), (960, 24)), ("South wall", (480, 628), (960, 24)),
                              ("West wall", (12, 360), (24, 536)), ("East wall", (948, 360), (24, 536)),
                              ("Crate A", (340, 260), (100, 100)), ("Crate B", (630, 390), (120, 90))):
    entity(entities, name, position, scale, sprite("white.png", tint=0x64748BFF), body())
player = entity(entities, "Player", (100, 520), visual=sprite("player.png", (16, 16), layer=3, rectangle=[0, 0, 32, 32]), physics=body(2, half=(12, 12)), script="player.lua")
player["animation"] = dict(frames=[dict(rectangle=[0, 0, 32, 32], seconds=.12), dict(rectangle=[32, 0, 32, 32], seconds=.12)], loop=True, playing=False)
for name, position in (("Drone A", (480, 180)), ("Drone B", (480, 500))):
    entity(entities, name, position, (32, 32), sprite("white.png", tint=0xEF4444FF, layer=2), body(1, True))
entity(entities, "HUD", (480, 40), visual=sprite("hud.png", (480, 40), layer=10))
for score in range(4):
    entity(entities, "Score " + str(score), (860, 38), visual=sprite("score.png", (50, 20), tint=0xFFFFFFFF if score == 0 else 0xFFFFFF00, layer=11, rectangle=[score * 100, 0, 100, 40]))
entity(entities, "Timer track", (650, 62), (300, 6), sprite("white.png", tint=0x475569FF, layer=11))
entity(entities, "Timer marker", (800, 62), (8, 14), sprite("white.png", tint=0x38BDF8FF, layer=12))
sound = entity(entities, "Signal sound")
sound["audio"] = dict(clip="Audio/signal.wav", assetId=asset_id("Audio/signal.wav"), volume=70, loop=False, autoPlay=False)
write("Content/Scenes/arena.verisscene", dict(formatVersion=1, entities=entities))
coins = []
entity(coins, "Signal", scale=(24, 24), visual=sprite("white.png", tint=0xFACC15FF, layer=2), physics=body(0, True))
entity(coins, "Signal halo", scale=(1.6, 1.6), visual=sprite("white.png", tint=0xFACC1530, layer=1), parent=1)
write("Content/Prefabs/signal.verisprefab", dict(formatVersion=1, root=1, scene=dict(formatVersion=1, entities=coins)))
assets = []
for path in sorted((ROOT / "Content").rglob("*")):
    if path.suffix not in (".png", ".wav"):
        continue
    relative = path.relative_to(ROOT / "Content").as_posix()
    assets.append(dict(id=asset_id(relative), path=relative, kind="audio" if path.suffix == ".wav" else "texture", sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
write(".Veris/assets.json", dict(formatVersion=1, assets=assets))
write(".Veris/input-actions.json", dict(formatVersion=1, actions={"Quit": ["Key:Escape"], "Start": ["Key:Enter"], "Restart": ["Key:R"], "MoveUp": ["Key:W", "Key:Up"], "MoveDown": ["Key:S", "Key:Down"], "MoveLeft": ["Key:A", "Key:Left"], "MoveRight": ["Key:D", "Key:Right"]}))
write("BeaconRun.veris", dict(formatVersion=1, projectId="0ac9e72e-6bb9-43a2-9dc9-586c513ed913", name="Beacon Run", templateId="top-down", contentDirectory="Content", gameCodeDirectory="GameCode", startupScene="Scenes/title.verisscene", windowWidth=960, windowHeight=640, windowMode="windowed", windowResizable=True, verticalSync=True, gravityX=0, gravityY=0, masterVolume=60))
