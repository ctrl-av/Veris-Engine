# Beacon Run

From the repository root after building Debug x64:

```powershell
artifacts/bin/VerisRuntime/x64/Debug/VerisRuntime.exe Samples/BeaconRun/BeaconRun.veris
```

Open `BeaconRun.veris` in the editor and choose **Play project**, or export it with the published editor and run `Game.exe`.

- Enter starts the game. WASD or arrows move. Collect all three gold signals within 35 seconds while avoiding the moving red drones. Crates and arena walls block movement.
- The HUD tracks collected signals and remaining time. Recovering all signals opens the victory scene; drone contact or timeout opens the defeat scene. R restarts from either result screen. Escape closes standalone play.
- The game uses atlas animation, Box2D solid bodies and trigger sensors, Lua state and input actions, runtime prefab spawning, collection audio, viewport-aware camera fitting and deferred scene transitions.
- The scene/prefab documents, asset catalog, scripts and assets are editable project content. `scripts/GenerateSampleAssets.ps1` authors the geometric/typographic PNG and PCM assets; `python scripts/GenerateSample.py` rebuilds the scene documents, catalog and project descriptor.
- `Tests/InteropBehavior/SampleChecks.cs` runs actual game state through start, collection/victory, restart, drone defeat, restart and timeout. It uses native physics contacts rather than mocked events.
