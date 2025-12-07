# TODO.md — Step-by-step hackathon plan (hour-by-hour)

## Priorities & scope
**MUST (core demo within 24h):**
- Map with 2–3 ghost pins.
- Chat-based ghost creation -> GPT-5 -> validated JSON.
- Distance logic + AR reveal (simulate proximity option).
- Tap interactions: message, riddle -> show reward (coupon text).
- Polished 2-minute demo script.

**SHOULD (if time permits):**
- Business-sponsored ghost type (coupon redemption mock).
- Voice input for creator.
- Simple analytics (visit counter).

**DON'T (avoid now):**
- Payments / full marketplace
- Complex moderation
- Multiplayer sync

---

## Hour-by-hour plan (24-hour hackathon)
**Hour 0-1: Setup & team alignment**
- Fork/create repo.
- Create backend Node.js skeleton (Express).
- Create Unity project (AR Foundation template).
- Add basic README & TODO.

**Hour 1-3: Backend core + OpenAI integration**
- Implement `/api/create-ghost` endpoint (accept prompt, lat, lng).
- Implement Azure OpenAI call with the system prompt (sync).
- Implement basic JSON validation (schema-based).
- Store ghost JSON in simple SQLite or in-memory store.

**Hour 3-5: Map screen (Unity)**
- Integrate Mapbox SDK or simple 2D map UI.
- Implement map camera + show static ghost pins (use sample coords).
- Implement map pin tap -> open Ghost Preview UI.

**Hour 5-7: Fetch ghosts + distance logic**
- Unity fetches `/api/ghosts` for viewport.
- Calculate distance from player to ghost (Haversine).
- Show distance and "Enter AR" button disabled if far.
- Add "simulate proximity" button for testing.

**Hour 7-10: AR view & ghost prefab**
- Setup AR Foundation scene for iOS.
- Create simple ghost prefab (floating sprite or 3D model).
- Implement `GhostSpawner` to spawn prefab when entering AR.
- Add tap detection on prefab -> call interaction handler.

**Hour 10-12: Interaction types**
- Implement `message` interaction: show modal with text + OK.
- Implement `riddle_unlock`: show riddle input UI, validate answer, show reward.
- Implement `coupon` display: show coupon code.

**Hour 12-14: Conversational Creator UI**
- Implement a chat UI in Unity or embed a tiny web UI:
  - User types prompt + location.
  - Calls backend `/api/create-ghost`.
  - Backend returns JSON -> Unity refreshes map.
- Ensure server validates output.

**Hour 14-16: Polish & UX**
- Make map pins pretty, show distance labels.
- Add transitions: map -> AR.
- Add small audio + idle animation to ghost prefab.

**Hour 16-18: Business ghost mock & analytics**
- Add a "sponsored" flag to some ghosts.
- Increment visit counter server-side when AR opened.

**Hour 18-20: Test on device**
- Build and deploy to iOS device.
- Run through demo script.
- Fix crashes and performance issues.

**Hour 20-22: Final touches & recording**
- Prepare short demo recording or live demo flow.
- Prepare 5-slide pitch deck or slide notes.
- Create README and quick start instructions.

**Hour 22-24: Buffer & contingency**
- Handle last-minute integration issues.
- Prepare fallback demo (recorded video + local simulation).

---

## File & code checklist
- [ ] backend/index.js (Express + OpenAI)
- [ ] backend/validate.js (JSON schema)
- [ ] backend/dockerfile
- [ ] unity/Assets/Scripts/GhostConfig.cs
- [ ] unity/Assets/Scripts/GhostSpawner.cs
- [ ] unity/Scenes/MapScene.unity
- [ ] unity/Scenes/ARScene.unity
- [ ] prompts/system_prompt.txt
- [ ] docs/demo-script.md

---

## Testing & demo checklist
- [ ] Chat create -> ghost visible on map
- [ ] Tap map pin -> preview shows correct info
- [ ] Enter AR -> ghost spawns (or simulated)
- [ ] Tap ghost -> interaction runs
- [ ] Riddle -> correct answer unlocks reward
- [ ] Sponsored ghost shows coupon text

---

## Fallbacks & shortcuts (if time tight)
- Use simulated GPS toggles instead of live GPS.
- Use 2D "AR" mock (ghost overlay on camera texture) if AR Foundation integration fails.
- Hardcode 2-3 ghosts into Unity to demo interactions and chat creation separately.

---

## Helpful prompts for GPT-5 (creator chat examples)
- "Create a friendly ghost near [lat],[lng] that tells a fun historical fact and gives a 'FREE COFFEE' coupon when the user answers a riddle."
- "Make a promotional ghost for a bookstore at [lat],[lng] that gives users 15% off if they pick the correct book title from 3 options."
- "Create a small haunted-tour ghost sequence of 3 ghosts along this street. Keep each message <= 120 chars."

---

Good luck at the hackathon — tell me if you want a 5-slide pitch deck outline and the actual Node.js template & Unity C# snippets.