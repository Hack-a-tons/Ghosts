# GhostLayer

Location-anchored AR ghosts created via conversational AI — see them on map, meet them in AR

## Overview

**Creator** can:
- See the map
- Add ghosts
- Choose ghost location
- Choose ghost scenario

**Player** can:
- See ghosts on the map
- Come to them in AR
- See them (semi-transparent, floating)
- Talk to them
- Solve riddles for rewards

## Architecture

```
[Unity/Meta XR App] <--> [Node.js API] <--> [Azure OpenAI]
                              |
                              --> [PostgreSQL]
```

## Project Structure

```
├── backend/          # Node.js API (Docker)
│   ├── src/
│   │   ├── index.js  # Express server + live monitor
│   │   ├── db.js     # PostgreSQL connection
│   │   └── ai.js     # Hurated AI client
│   └── compose.yml
├── unity/            # Unity project (iOS/Meta Quest)
│   └── My project/
│       └── Assets/
│           ├── Scripts/   # Ghost system scripts
│           ├── Prefabs/   # Ghost prefab
│           └── Scenes/    # QuestScene, SampleScene
├── prompts/          # AI system prompts
└── deploy.sh         # Deployment script
```

## Deployment

- API: `https://ghosts.api.app.hurated.com`
- Monitor: `https://ghosts.api.app.hurated.com/monitor`
- Client: `https://ghost.api.app.hurated.com`

## Quick Start

### Backend
```bash
cd backend
cp .env.example .env  # Edit with your credentials
docker compose up -d
```

### Unity
1. Open `unity/My project/` in Unity Hub
2. Menu: `GhostLayer → Create Quest Scene`
3. `File → Build and Run`

See `unity/README.md` for detailed setup.

## Ghost JSON Schema

```json
{
  "name": "Cafe Warden",
  "personality": "Mysterious but playful",
  "location": { "lat": 37.7841, "lng": -122.4075 },
  "visibility_radius_m": 150,
  "interaction": {
    "type": "riddle_unlock",
    "riddle": "What drinks without a mouth?",
    "correct_answer": "coffee",
    "reward": { "type": "coupon", "value": "10% OFF" }
  }
}
```

## API Endpoints

### Ghosts
- `POST /api/ghosts` — Create ghost via AI prompt
- `GET /api/ghosts?lat=&lng=&radius=` — Get nearby ghosts
- `GET /api/ghosts/:id` — Get ghost by ID
- `POST /api/ghosts/:id/interact` — Record interaction

### Players
- `POST /api/players/position` — Report player position
- `GET /api/players` — Get active players

### Monitoring
- `GET /monitor` — Live map dashboard (HTML)
- `GET /api/monitor/stats` — Statistics
- `GET /api/monitor/activity` — Activity log

## Unity Scripts

| Script | Purpose |
|--------|---------|
| `GhostData.cs` | Data models matching API schema |
| `GhostAPI.cs` | HTTP client, player position reporting |
| `GhostManager.cs` | Spawns/manages ghost objects |
| `GhostVisual.cs` | Rendering, floating animation, transparency |
| `GhostInteractor.cs` | Proximity detection, riddle handling |
| `LocationService.cs` | GPS wrapper + debug mode for Quest |
| `RiddleUI.cs` | Riddle/reward UI panels |
| `MiniMapUI.cs` | Overlay map with markers |

## Editor Tools (GhostLayer Menu)

- **Create Ghost Prefab** — Generates ghost with materials
- **Setup Scene** — Adds manager components
- **Create Riddle UI** — Creates UI panels
- **Create Quest Scene** — Clean scene for Meta Quest
- **Check Build Settings** — Verify build configuration

## Platforms

- **Meta Quest 3/3S** — VR/MR with passthrough
- **iOS** — AR with ARKit
- **Android** — AR with ARCore
- **macOS** — Editor testing

## License

MIT
