import express from 'express';
import { pool, initDb } from './db.js';
import { generateGhost } from './ai.js';

const app = express();
app.use(express.json());

// In-memory stores
const activityLog = [];
const MAX_LOG = 100;
const playerPositions = new Map(); // client_id -> {lat, lng, updated}

function logActivity(type, data) {
  const event = { type, data, timestamp: new Date().toISOString() };
  activityLog.unshift(event);
  if (activityLog.length > MAX_LOG) activityLog.pop();
  console.log(`[ACTIVITY] ${type}:`, JSON.stringify(data));
}

// Clean old player positions (older than 30s)
setInterval(() => {
  const now = Date.now();
  for (const [id, pos] of playerPositions) {
    if (now - pos.updated > 30000) playerPositions.delete(id);
  }
}, 10000);

// CORS
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Headers', 'Content-Type');
  res.header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  if (req.method === 'OPTIONS') return res.sendStatus(200);
  next();
});

// Health check
app.get('/health', (req, res) => res.json({ status: 'ok' }));

// === PLAYER TRACKING ===

app.post('/api/players/position', (req, res) => {
  const { client_id, lat, lng } = req.body;
  if (!client_id || lat == null || lng == null) {
    return res.status(400).json({ error: 'client_id, lat, lng required' });
  }
  playerPositions.set(client_id, { lat, lng, updated: Date.now() });
  res.json({ success: true });
});

app.get('/api/players', (req, res) => {
  const players = [];
  for (const [id, pos] of playerPositions) {
    players.push({ client_id: id, lat: pos.lat, lng: pos.lng });
  }
  res.json(players);
});

// === MONITORING ===

app.get('/api/monitor/activity', (req, res) => {
  const limit = Math.min(parseInt(req.query.limit) || 50, MAX_LOG);
  res.json(activityLog.slice(0, limit));
});

app.get('/api/monitor/stats', async (req, res) => {
  try {
    const ghostCount = await pool.query('SELECT COUNT(*) FROM ghosts');
    const totalInteractions = await pool.query('SELECT COALESCE(SUM(interactions), 0) as total FROM ghosts');
    res.json({
      total_ghosts: parseInt(ghostCount.rows[0].count),
      total_interactions: parseInt(totalInteractions.rows[0].total),
      active_players: playerPositions.size
    });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// Live map monitor page
app.get('/monitor', (req, res) => {
  res.send(`<!DOCTYPE html>
<html><head><title>GhostLayer Monitor</title>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: -apple-system, sans-serif; background: #1a1a2e; color: #fff; }
  .container { display: flex; height: 100vh; }
  #map { flex: 1; }
  .sidebar { width: 350px; padding: 15px; overflow-y: auto; background: #16213e; }
  h1 { font-size: 18px; margin-bottom: 15px; }
  .stats { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 10px; margin-bottom: 15px; }
  .stat { background: #0f3460; padding: 10px; border-radius: 8px; text-align: center; }
  .stat-value { font-size: 24px; color: #0ff; }
  .stat-label { font-size: 11px; color: #888; }
  h2 { font-size: 14px; margin: 15px 0 10px; color: #888; }
  #log { font-size: 12px; font-family: monospace; }
  .event { padding: 8px; margin-bottom: 5px; background: #0d0d0d; border-radius: 4px; border-left: 3px solid #333; }
  .ghost_created { border-left-color: #f0f; }
  .ghost_interact { border-left-color: #0ff; }
  .ghost_appear { border-left-color: #0f0; }
  .event-type { color: #ff0; font-weight: bold; }
  .event-time { color: #666; font-size: 10px; }
  .ghost-marker { background: #0ff; border: 2px solid #fff; border-radius: 50%; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; font-size: 12px; }
  .player-marker { background: #0f0; border: 2px solid #fff; border-radius: 50%; width: 16px; height: 16px; }
</style>
</head><body>
<div class="container">
  <div id="map"></div>
  <div class="sidebar">
    <h1>👻 GhostLayer Monitor</h1>
    <div class="stats">
      <div class="stat"><div class="stat-value" id="ghosts">-</div><div class="stat-label">Ghosts</div></div>
      <div class="stat"><div class="stat-value" id="players">-</div><div class="stat-label">Players</div></div>
      <div class="stat"><div class="stat-value" id="interactions">-</div><div class="stat-label">Interactions</div></div>
    </div>
    <h2>Activity Log</h2>
    <div id="log"></div>
  </div>
</div>
<script>
const map = L.map('map').setView([37.7749, -122.4194], 14);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  attribution: '© OpenStreetMap'
}).addTo(map);

const ghostIcon = L.divIcon({ className: 'ghost-marker', html: '👻', iconSize: [24, 24] });
const playerIcon = L.divIcon({ className: 'player-marker', iconSize: [16, 16] });

let ghostMarkers = {};
let playerMarkers = {};
let firstLoad = true;

async function refresh() {
  // Stats
  const stats = await fetch('/api/monitor/stats').then(r => r.json());
  document.getElementById('ghosts').textContent = stats.total_ghosts;
  document.getElementById('players').textContent = stats.active_players;
  document.getElementById('interactions').textContent = stats.total_interactions;
  
  // Ghosts
  const ghosts = await fetch('/api/ghosts').then(r => r.json());
  const ghostIds = new Set();
  ghosts.forEach(g => {
    ghostIds.add(g.id);
    if (!ghostMarkers[g.id]) {
      ghostMarkers[g.id] = L.marker([g.location.lat, g.location.lng], { icon: ghostIcon })
        .bindPopup('<b>' + g.name + '</b><br>' + g.personality)
        .addTo(map);
    } else {
      ghostMarkers[g.id].setLatLng([g.location.lat, g.location.lng]);
    }
  });
  // Remove old
  Object.keys(ghostMarkers).forEach(id => {
    if (!ghostIds.has(parseInt(id))) {
      map.removeLayer(ghostMarkers[id]);
      delete ghostMarkers[id];
    }
  });
  
  // Players
  const players = await fetch('/api/players').then(r => r.json());
  const playerIds = new Set();
  players.forEach(p => {
    playerIds.add(p.client_id);
    if (!playerMarkers[p.client_id]) {
      playerMarkers[p.client_id] = L.marker([p.lat, p.lng], { icon: playerIcon })
        .bindPopup('Player: ' + p.client_id.slice(0,8))
        .addTo(map);
    } else {
      playerMarkers[p.client_id].setLatLng([p.lat, p.lng]);
    }
  });
  Object.keys(playerMarkers).forEach(id => {
    if (!playerIds.has(id)) {
      map.removeLayer(playerMarkers[id]);
      delete playerMarkers[id];
    }
  });
  
  // Center on first ghost if first load
  if (firstLoad && ghosts.length > 0) {
    map.setView([ghosts[0].location.lat, ghosts[0].location.lng], 15);
    firstLoad = false;
  }
  
  // Activity log
  const activity = await fetch('/api/monitor/activity?limit=20').then(r => r.json());
  document.getElementById('log').innerHTML = activity.map(e => 
    '<div class="event ' + e.type + '">' +
    '<span class="event-type">[' + e.type + ']</span> ' +
    JSON.stringify(e.data).slice(0, 100) +
    '<div class="event-time">' + new Date(e.timestamp).toLocaleTimeString() + '</div></div>'
  ).join('');
}

refresh();
setInterval(refresh, 2000);
</script>
</body></html>`);
});

// === GHOST ENDPOINTS ===

app.post('/api/ghosts', async (req, res) => {
  try {
    const { prompt, lat, lng } = req.body;
    if (!prompt || lat == null || lng == null) {
      return res.status(400).json({ error: 'prompt, lat, lng required' });
    }
    const ghost = await generateGhost(prompt, lat, lng);
    const result = await pool.query(
      `INSERT INTO ghosts (name, personality, lat, lng, visibility_radius_m, interaction, created_at)
       VALUES ($1, $2, $3, $4, $5, $6, NOW()) RETURNING id`,
      [ghost.name, ghost.personality, ghost.location.lat, ghost.location.lng, 
       ghost.visibility_radius_m, JSON.stringify(ghost.interaction)]
    );
    const ghostData = { id: result.rows[0].id, ...ghost };
    logActivity('ghost_created', { id: ghostData.id, name: ghost.name, lat, lng });
    res.json(ghostData);
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

app.get('/api/ghosts', async (req, res) => {
  try {
    const { lat, lng, radius = 1000, client_id } = req.query;
    let query = 'SELECT * FROM ghosts ORDER BY created_at DESC LIMIT 100';
    let params = [];
    if (lat && lng) {
      query = `SELECT *, 
        (6371000 * acos(cos(radians($1)) * cos(radians(lat)) * cos(radians(lng) - radians($2)) + sin(radians($1)) * sin(radians(lat)))) as distance
        FROM ghosts
        WHERE (6371000 * acos(cos(radians($1)) * cos(radians(lat)) * cos(radians(lng) - radians($2)) + sin(radians($1)) * sin(radians(lat)))) < $3
        ORDER BY distance LIMIT 100`;
      params = [lat, lng, radius];
    }
    const result = await pool.query(query, params);
    const ghosts = result.rows.map(r => ({
      id: r.id,
      name: r.name,
      personality: r.personality,
      location: { lat: parseFloat(r.lat), lng: parseFloat(r.lng) },
      visibility_radius_m: r.visibility_radius_m,
      interaction: r.interaction,
      distance: r.distance ? Math.round(r.distance) : null
    }));
    
    if (client_id && ghosts.length > 0) {
      logActivity('ghost_appear', { 
        client_id: client_id.slice(0, 8),
        count: ghosts.length
      });
    }
    
    res.json(ghosts);
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

app.get('/api/ghosts/:id', async (req, res) => {
  try {
    const result = await pool.query('SELECT * FROM ghosts WHERE id = $1', [req.params.id]);
    if (!result.rows.length) return res.status(404).json({ error: 'not found' });
    const r = result.rows[0];
    res.json({
      id: r.id,
      name: r.name,
      personality: r.personality,
      location: { lat: parseFloat(r.lat), lng: parseFloat(r.lng) },
      visibility_radius_m: r.visibility_radius_m,
      interaction: r.interaction
    });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

app.post('/api/ghosts/:id/interact', async (req, res) => {
  try {
    const { action, client_id } = req.body;
    await pool.query(
      'UPDATE ghosts SET interactions = interactions + 1 WHERE id = $1',
      [req.params.id]
    );
    
    const ghost = await pool.query('SELECT name FROM ghosts WHERE id = $1', [req.params.id]);
    logActivity('ghost_interact', { 
      ghost_id: parseInt(req.params.id),
      ghost_name: ghost.rows[0]?.name,
      action: action || 'interact',
      client_id: client_id?.slice(0, 8)
    });
    
    res.json({ success: true });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

const PORT = process.env.PORT || 6000;
initDb().then(() => {
  app.listen(PORT, () => console.log(`API running on port ${PORT}`));
});
