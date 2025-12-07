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
- See them
- Talk to them
- Take action

## Architecture

```
[Unity/Meta XR App] <--> [Node.js API] <--> [Azure OpenAI]
                              |
                              --> [PostgreSQL]
```

## Project Structure

```
├── backend/          # Node.js API (Docker)
├── unity/            # Unity project (iOS/Meta Quest)
└── prompts/          # AI system prompts
```

## Deployment

- API: `https://ghosts.api.app.hurated.com`
- Client: `https://ghost.api.app.hurated.com`

## Quick Start

### Backend
```bash
cd backend
cp .env.example .env  # Edit with your credentials
docker compose up -d
```

### Unity
See `unity/README.md`

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

- `POST /api/ghosts` — Create ghost via AI prompt
- `GET /api/ghosts?lat=&lng=&radius=` — Get nearby ghosts
- `GET /api/ghosts/:id` — Get ghost by ID
- `POST /api/ghosts/:id/interact` — Record interaction

## License

MIT