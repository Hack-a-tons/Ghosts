# GhostLayer — AR Ghost Creator (Hackathon README)

**Project:** GhostLayer — conversational, location-anchored AR ghosts  
**Hackathon:** SensAI (XR + AI)  
**Author:** Denis Bystruev (iOS / Unity / Node.js)  
**Date:** December 5, 2025 (hackathon demo)

---

## Elevator pitch (30s)
You don't program ghosts — you *talk* them into existence. Users create location-anchored interactive AR ghosts via a short conversational UI powered by GPT-5. Ghosts are visible on the map from afar and become AR experiences when players are physically near. Businesses and creators can publish promo ghosts (discounts, coupons, story beats).

---

## Goals for the hackathon (minimum viable demo)
- Map view showing 2–3 ghosts.
- Distance-based AR reveal: map → proximity → AR.
- Tap to interact: show message or riddle, unlock a simple reward (coupon text).
- Conversational ghost creation UI (text chat) using Azure OpenAI -> outputs **structured JSON**.
- Unity reads JSON and spawns ghost with properties (name, personality, radius, interaction).

---

## Architecture (short)
```
[Unity iOS App (AR Foundation)] <--> [Node.js API (Docker/nginx)] <--> [Azure OpenAI GPT-5]
                                         |
                                         --> [DB (SQLite / PostgreSQL)]
```

- Unity: Map screen, Ghost Detail, AR View, Chat/Create Ghost UI (embedded WebView or native UI).
- Backend: single Node.js service that proxies prompts to Azure OpenAI, validates JSON, stores ghost configs.
- DB: simple store for ghost JSON; can use SQLite for hackathon.

---

## Ghost JSON schema (example)
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
  },
  "ar_behavior": {
    "float": true,
    "loop_animation": "idle_ghost",
    "sound": "whisper"
  }
}
```

---

## Azure GPT-5 system prompt (recommended)
**Important:** Always sanitize and validate GPT output before storing or publishing.

```
You are "Ghost Behavior Generator" for an AR app.
You MUST respond with **only valid JSON** matching the schema below. No explanation.
Supported interaction types: message, riddle_unlock, timed_drop, coupon.
Schema:
{
  "name": string,
  "personality": string,
  "location": { "lat": number, "lng": number },
  "visibility_radius_m": number,
  "interaction": { "type": string, ... },
  "ar_behavior": { "float": boolean, "loop_animation": string, "sound": string }
}
If any field is missing or invalid, ask one clarifying question only.
Max name length: 40 characters.
Max visibility_radius_m: 500 (meters).
```

---

## Backend endpoints (hackathon)
- `POST /api/create-ghost` — body: `{ "prompt": "text", "lat": number, "lng": number }`  
  - Server: construct system+user prompt, call Azure OpenAI, validate JSON, store and return ghost id.
- `GET /api/ghosts?lat=&lng=&radius=` — returns ghosts near given coords.
- `GET /api/ghost/:id` — returns ghost JSON.
- `POST /api/validate-json` — (dev) validate sample JSON against schema.

Security: Protect OpenAI key on server. Rate limit chat creation.

---

## Unity integration notes
- Use **AR Foundation** + ARKit for iOS.
- Use a map SDK: Mapbox Unity SDK (or simplified 2D map fallback).
- Ghost Spawner flow:
  1. Fetch ghosts from API for current map viewport.
  2. Show map pins with distance.
  3. If distance < visibility_radius_m: enable "Enter AR" button.
  4. On Enter AR: spawn AR prefab at a "spatial anchor" near user location (for hackathon, approximate with camera-relative placement).
  5. Bind ghost JSON to prefab: name, animations, interaction logic.
- Interaction types to support in demo: `message`, `riddle_unlock`, `coupon`.

Suggested simple C# approach:
- `GhostConfig` class maps schema.
- `GhostSpawner` fetches config and instantiates `GhostPrefab`.
- `GhostPrefab` has `OnTap()` that runs interaction (shows UI modal).

---

## Demo script (2-minute flow)
1. Launch app — map shows three ghost pins (public + sponsored).
2. Tap a ghost on the map — preview card opens (name, distance).
3. Walk close (or simulate via "simulate proximity" button) — "Enter AR" becomes active.
4. Enter AR — see ghost model floating; tap → ghost speaks text / shows a riddle.
5. Show the conversational creator: type "Create a friendly ghost near this café that gives a 10% coupon for coffee." -> GPT returns JSON -> ghost appears on map immediately.
6. Finish: show business dashboard mock (analytics count: 3 visits).

---

## Judge Q&A (anticipated)
- **How is content moderated?** Only validated JSON types published; no free-form public text; whitelisted interaction types.
- **How do businesses pay?** Demo shows "sponsored" tag; pricing and redemption would be future work.
- **What about privacy?** No PII stored; location used only for proximity matching.

---

## Dev environment quickstart (local)
1. Backend
```bash
# inside /backend
docker build -t ghostlayer-api .
docker run --env OPENAI_API_KEY=... -p 3000:3000 ghostlayer-api
```
2. Unity
- Open Unity project (AR Foundation) -> set iOS build, ARKit, input API base URL -> run in editor (simulate GPS) or on-device.

---

## Useful resources
- Unity AR Foundation docs
- Mapbox Unity SDK
- Azure OpenAI API docs
- Sample Unity <-> REST examples

---

## Files included in repo
- `README.md` (this)
- `TODO.md` (step-by-step plan)
- `backend/` (Node.js template)
- `unity/` (Unity project skeleton)
- `prompts/` (system/user prompt examples)