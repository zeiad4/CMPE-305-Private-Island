# Private Island

Unity 6 URP starter scene for a stylized procedural island.

## Prerequisites

Install the required Windows tooling:

```powershell
winget install --id Unity.UnityHub --source winget --accept-package-agreements --accept-source-agreements --silent
winget install --id Unity.Unity.6000 --source winget --accept-package-agreements --accept-source-agreements --silent
winget install --id Microsoft.WindowsSDK.10.0.22000 --source winget --accept-package-agreements --accept-source-agreements --silent
winget install --id Microsoft.VisualStudio.2022.Community --source winget --accept-package-agreements --accept-source-agreements --override "--wait --quiet --add Microsoft.VisualStudio.Workload.ManagedGame --includeRecommended --norestart"
```

If the Unity Hub `winget` package fails with a hash mismatch, install it directly:

```powershell
$ProgressPreference='SilentlyContinue'
Invoke-WebRequest -Uri 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe' -OutFile "$env:TEMP\UnityHubSetup-x64.exe"
Start-Process -FilePath "$env:TEMP\UnityHubSetup-x64.exe" -ArgumentList '/S' -Wait -WindowStyle Hidden
```

Verify the main installs:

```powershell
winget list --id Unity.UnityHub
winget list --id Unity.Unity.6000
winget list --id Microsoft.VisualStudio.2022.Community
```

## Unity Packages

This project uses Unity `6000.4.5f1`. Package dependencies are declared in `Packages/manifest.json`, and Unity Package Manager restores them automatically when you open the project. There are no separate `npm`, `pip`, or NuGet install steps for gameplay libraries.

Packages currently declared:

- `com.unity.ai.navigation`
- `com.unity.collab-proxy`
- `com.unity.ide.rider`
- `com.unity.ide.visualstudio`
- `com.unity.render-pipelines.universal`
- `com.unity.test-framework`
- `com.unity.timeline`
- `com.unity.ugui`
- `com.unity.visualscripting`

## What's in here

- A scene bootstrap that builds the island terrain, water, props, lighting, and post-processing from a single root object.
- A first-person camera preset so the island explorer is controlled from the character's point of view in play mode.
- Basic Unity project hygiene with a practical `.gitignore`.

## Open the project

1. Open the project in Unity `6000.4.5f1`.
2. Load `Assets/Scenes/SampleScene.unity`.
3. Select `Island Bootstrap` to tune terrain size, sea level, vegetation count, and seed.
4. Press Play to explore the island in first person.

If you need to clone the project first:

```powershell
git clone <your-repo-url>
cd private-island
```

## Controls

- `W`, `A`, `S`, `D` or arrow keys move the island explorer.
- Move the mouse to look around.
- Press `Esc` to release the cursor, then left click in the Game view to capture it again.

## Structure

- `Assets/Scripts/Environment/IslandSceneBootstrap.cs`
- `Assets/Scripts/Environment/IslandMeshBuilder.cs`
- `Assets/Scripts/Camera/IslandFirstPersonCamera.cs`
