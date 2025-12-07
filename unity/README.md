# GhostLayer Unity Client

AR client for GhostLayer — iOS and Meta Quest support.

## Requirements

- Unity 6000.0.x (LTS)
- AR Foundation 6.x
- Meta XR SDK (for Quest)
- ARKit XR Plugin (for iOS)

## Setup

1. Open project in Unity Hub
2. Import required packages via Package Manager:
   - AR Foundation
   - ARKit XR Plugin (iOS)
   - Meta XR SDK (Quest)
3. Configure API endpoint in `Assets/Scripts/ApiClient.cs`

## Build Targets

### iOS
- Set build target to iOS
- Enable ARKit in XR Plug-in Management
- Build and deploy via Xcode

### Meta Quest
- Set build target to Android
- Enable Meta XR in XR Plug-in Management
- Build APK and deploy via adb

## Project Structure

```
Assets/
├── Scenes/
│   ├── MapScene.unity      # Map view with ghost pins
│   └── ARScene.unity       # AR ghost interaction
├── Scripts/
│   ├── ApiClient.cs        # REST API client
│   ├── GhostConfig.cs      # Ghost data model
│   ├── GhostSpawner.cs     # AR ghost spawning
│   └── MapController.cs    # Map UI logic
├── Prefabs/
│   └── GhostPrefab.prefab  # AR ghost model
└── UI/
    └── GhostUI.prefab      # Interaction UI
```

## API Integration

The client connects to:
- Production: `https://ghosts.api.app.hurated.com`
- Local dev: `http://localhost:6000`

## Features

- Map view showing nearby ghosts
- Distance-based AR reveal
- Ghost interactions (message, riddle, coupon)
- Chat-based ghost creation
