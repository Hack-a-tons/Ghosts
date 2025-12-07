# TODO

## Phase 1: Backend Setup
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
  - [x] src/ai.js (Azure OpenAI client)
- [x] Database schema (migrations)
- [x] API endpoints
  - [x] POST /api/ghosts (create via AI)
  - [x] GET /api/ghosts (list nearby)
  - [x] GET /api/ghosts/:id
  - [x] POST /api/ghosts/:id/interact
- [x] Bash scripts
  - [x] deploy.sh
  - [x] psql.sh
  - [x] ai.sh (test AI)
- [x] Prompts
  - [x] prompts/ghost-creator.md

## Phase 2: Unity Client
- [x] Create Unity project structure
- [x] unity/README.md
- [x] .gitignore for Unity
- [ ] Map view with ghost pins
- [ ] Ghost detail view
- [ ] AR view with ghost prefab
- [ ] Chat UI for ghost creation
- [ ] API client

## Phase 3: Deployment
- [ ] Setup nginx configs on server
- [ ] SSL certificates
- [ ] Deploy backend
- [ ] Test API endpoints

## Phase 4: Polish
- [ ] Ghost animations
- [ ] Sound effects
- [ ] UI polish
- [ ] Demo script
