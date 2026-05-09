# Unity Windows Setup Commands

This file records the Windows commands used to install the Unity development stack for this project.

## 1. Install Unity Editor

```powershell
winget install --id Unity.Unity.6000 --source winget --accept-package-agreements --accept-source-agreements --silent
```

## 2. Install Unity Hub

Preferred command:

```powershell
winget install --id Unity.UnityHub --source winget --accept-package-agreements --accept-source-agreements --silent
```

If the `winget` package fails with a hash mismatch, use Unity's official installer directly:

```powershell
$ProgressPreference='SilentlyContinue'
Invoke-WebRequest -Uri 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe' -OutFile "$env:TEMP\UnityHubSetup-x64.exe"
Start-Process -FilePath "$env:TEMP\UnityHubSetup-x64.exe" -ArgumentList '/S' -Wait -WindowStyle Hidden
```

## 3. Install Windows SDK

```powershell
winget install --id Microsoft.WindowsSDK.10.0.22000 --source winget --accept-package-agreements --accept-source-agreements --silent
```

## 4. Install Visual Studio Community with Unity Workload

```powershell
winget install --id Microsoft.VisualStudio.2022.Community --source winget --accept-package-agreements --accept-source-agreements --override "--wait --quiet --add Microsoft.VisualStudio.Workload.ManagedGame --includeRecommended --norestart"
```

## 5. Verify Installs

```powershell
winget list --id Unity.UnityHub
winget list --id Unity.Unity.6000
winget list --id Microsoft.VisualStudio.2022.Community
```

## 6. Verify Executable Paths

```powershell
Test-Path 'C:\Program Files\Unity Hub\Unity Hub.exe'
Test-Path 'C:\Program Files\Unity 6000.4.5f1\Editor\Unity.exe'
Test-Path 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe'
```

## Notes

- Target platform here is Windows desktop.
- `Unity 6000` is Unity 6 LTS.
- Windows desktop build support is included with the Windows editor install.
- If Visual Studio was previously broken or partial, a clean reinstall may be simpler than repairing in place.
