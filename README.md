Veris is a 64-bit Windows 2D game engine built using C++ and C#. It uses SFML and the MSBuild build system. I am a lifelong gamer, and I built this to see if I could create my own game engine.

## Prerequisites

- Windows 10 or Windows 11, x64
- Visual Studio 2022 with the C++ and .NET desktop workloads
- MSVC v143 and a Windows SDK
- .NET 8 SDK

Note: Run the commands from a Visual Studio Developer PowerShell in the repository root.

## Building

The build process is managed by `Veris.sln` and the PowerShell scripts in `scripts/`.

### Build and Run

```powershell
powershell -File scripts/InitializeDependencies.ps1                  # Set up native dependencies
msbuild Veris.sln /restore /p:Configuration=Release /p:Platform=x64   # Build the engine and editor
./artifacts/bin/VerisEditor/Release/net8.0-windows/VerisEditor.exe    # Run the editor
msbuild Veris.sln /t:Clean /p:Configuration=Release /p:Platform=x64   # Clean Release build outputs
```

### Publishing

```powershell
powershell -File scripts/PublishEditor.ps1 -Configuration Release -OutputDirectory artifacts/publish/editor
./artifacts/publish/editor/VerisEditor.exe
```

Choose **Export game...** in the published editor to create a standalone Windows game.

## References

- SFML 3.0.1
- Box2D 2.4.1
- Lua 5.4.8
- Windows Presentation Foundation (WPF)

## License

No project license has been specified yet.
