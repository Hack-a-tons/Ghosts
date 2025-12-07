# TODO

## Phase 1: Backend Setup ✅
- [x] Create project structure
- [x] README.md with architecture
- [x] TODO.md with plan
- [x] Backend Node.js skeleton
  - [x] compose.yml with services
  - [x] .env.example
  - [x] Dockerfile
  - [x] package.json
  - [x] src/index.js (Express server)
  - [x] src/db.js (PostgreSQL connection)
  - [x] src/ai.js (Hurated AI client)
- [x] Database schema (migrations)
- [x] API endpoints
  - [x] POST /api/ghosts (create via AI)
  - [x] GET /api/ghosts (list nearby)
  - [x] GET /api/ghosts/:id
  - [x] POST /api/ghosts/:id/interact
  - [x] POST /api/players/position (player tracking)
  - [x] GET /api/players (list active players)
  - [x] GET /api/monitor/activity (activity log)
  - [x] GET /api/monitor/stats (statistics)
  - [x] GET /monitor (live map dashboard)
- [x] Bash scripts
  - [x] deploy.sh (root level)
  - [x] psql.sh
  - [x] ai.sh (test AI)
- [x] Prompts
  - [x] prompts/ghost-creator.md

## Phase 2: Unity Client 🔄 IN PROGRESS
- [x] Create Unity project structure (unity/My project/)
- [x] unity/README.md
- [x] .gitignore for Unity (updated for build artifacts)
- [x] Unity project created with Mobile AR Template
- [x] Installed packages:
  - [x] AR Foundation
  - [x] ARKit XR Plugin (iOS)
  - [x] ARCore XR Plugin (Android)
  - [x] Meta XR All-in-One SDK (Quest)
  - [x] XR Interaction Toolkit
- [x] Ghost scripts:
  - [x] GhostData.cs (data models)
  - [x] GhostAPI.cs (API client with player position reporting)
  - [x] GhostManager.cs (spawns/manages ghosts)
  - [x] GhostVisual.cs (rendering, animation, transparency)
  - [x] GhostInteractor.cs (proximity detection, riddles)
  - [x] LocationService.cs (GPS + debug mode for Quest)
  - [x] GhostShader.shader (ethereal effect)
- [x] UI scripts:
  - [x] RiddleUI.cs (riddle interaction panels)
  - [x] MiniMapUI.cs (overlay map with ghost markers)
- [x] Editor tools (GhostLayer menu):
  - [x] Create Ghost Prefab
  - [x] Setup Scene
  - [x] Create Riddle UI
  - [x] Create Quest Scene
  - [x] Check Build Settings
- [x] Debug tools:
  - [x] DebugLogger.cs (runtime logging)
  - [x] PassthroughEnabler.cs (Quest MR passthrough)
- [x] Ghost prefab with materials
- [x] QuestScene.unity (clean scene for Quest)
- [ ] **NEXT: Fix Quest passthrough (shows stars instead of real world)**
- [ ] Map view with ghost pins (full implementation)
- [ ] iOS AR testing
- [ ] Chat UI for ghost creation

## Phase 3: Deployment ✅
- [x] Setup nginx configs on server
- [x] SSL certificates (ghosts.api.app.hurated.com, ghost.api.app.hurated.com)
- [x] Deploy backend
- [x] Test API endpoints
- [x] Live monitor dashboard: https://ghosts.api.app.hurated.com/monitor

## Phase 4: Polish
- [x] Ghost floating animation
- [x] Ghost transparency/pulsing effect
- [ ] Sound effects
- [ ] UI polish
- [ ] Demo script

---

## Quick Start (Resume Development)

### Backend
```bash
# Deploy backend changes
./deploy.sh backend

# Check logs
ssh dbystruev@ghosts.api.app.hurated.com "cd Ghosts/backend && docker compose logs -f api"

# Test API
curl https://ghosts.api.app.hurated.com/health
curl https://ghosts.api.app.hurated.com/api/ghosts
```

### Unity (Quest)
1. Open Unity Hub → Open project: `unity/My project/`
2. Menu: `GhostLayer → Create Quest Scene` (creates clean scene with passthrough)
3. Menu: `GhostLayer → Check Build Settings` (verify only QuestScene is enabled)
4. `File → Build and Run` (builds to connected Quest)

### Unity (iOS)
1. `File → Build Profiles` → Select iOS → Switch Platform
2. Connect iPhone/iPad
3. `File → Build and Run`

### Unity (Editor Testing)
1. `File → Build Profiles` → Select macOS
2. Open `Assets/Scenes/QuestScene.unity`
3. Press Play

### Monitor Dashboard
- https://ghosts.api.app.hurated.com/monitor
- Shows live map with ghosts (👻) and players (green dots)
- Activity log updates every 2 seconds

---

## Known Issues

1. **Quest shows stars instead of passthrough**
   - Passthrough not enabling properly
   - Try: `Edit → Project Settings → Meta XR` → Enable passthrough
   - Try: `Edit → Project Settings → XR Plug-in Management → OpenXR → Meta Quest Features` → Enable Passthrough

2. **ARCore error on Quest**
   - ARCore is dimmed/can't disable in XR settings
   - Workaround: Use QuestScene which doesn't use AR Foundation

3. **Ghost shows "Ghost Name" instead of real name**
   - Fixed: Now shows GameObject name for test ghosts

---

## Project Structure
```
Ghosts/
├── backend/           # Node.js API (Docker)
│   ├── src/
│   │   ├── index.js   # Express server + monitor
│   │   ├── db.js      # PostgreSQL
│   │   └── ai.js      # Hurated AI client
│   └── compose.yml
├── unity/
│   └── My project/    # Unity 6 project
│       └── Assets/
│           ├── Scripts/       # C# scripts
│           ├── Prefabs/       # Ghost prefab
│           └── Scenes/        # QuestScene, SampleScene
├── prompts/           # AI prompts
├── deploy.sh          # Deployment script
└── README.md
```

## URLs
- API: https://ghosts.api.app.hurated.com
- Monitor: https://ghosts.api.app.hurated.com/monitor
- Client (future): https://ghost.api.app.hurated.com
