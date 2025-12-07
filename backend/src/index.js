import express from 'express';
import { pool, initDb } from './db.js';
import { generateGhost } from './ai.js';

const app = express();
app.use(express.json());

// In-memory activity log (last 100 events)
const activityLog = [];
const MAX_LOG = 100;

function logActivity(type, data) {
  const event = { type, data, timestamp: new Date().toISOString() };
  activityLog.unshift(event);
  if (activityLog.length > MAX_LOG) activityLog.pop();
  console.log(`[ACTIVITY] ${type}:`, JSON.stringify(data));
}

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

// === MONITORING ENDPOINTS ===

// Get activity log
app.get('/api/monitor/activity', (req, res) => {
  const limit = Math.min(parseInt(req.query.limit) || 50, MAX_LOG);
  res.json(activityLog.slice(0, limit));
});

// Get stats
app.get('/api/monitor/stats', async (req, res) => {
  try {
    const ghostCount = await pool.query('SELECT COUNT(*) FROM ghosts');
    const totalInteractions = await pool.query('SELECT COALESCE(SUM(interactions), 0) as total FROM ghosts');
    const recentGhosts = await pool.query('SELECT COUNT(*) FROM ghosts WHERE created_at > NOW() - INTERVAL \'24 hours\'');
    
    res.json({
      total_ghosts: parseInt(ghostCount.rows[0].count),
      total_interactions: parseInt(totalInteractions.rows[0].total),
      ghosts_last_24h: parseInt(recentGhosts.rows[0].count),
      activity_log_size: activityLog.length
    });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// Live monitor page (simple HTML)
app.get('/monitor', (req, res) => {
  res.send(`<!DOCTYPE html>
<html><head><title>GhostLayer Monitor</title>
<style>
  body { font-family: monospace; background: #1a1a2e; color: #0f0; padding: 20px; }
  h1 { color: #fff; }
  .stats { display: flex; gap: 20px; margin-bottom: 20px; }
  .stat { background: #16213e; padding: 15px; border-radius: 8px; }
  .stat-value { font-size: 24px; color: #0ff; }
  #log { background: #0d0d0d; padding: 10px; height: 400px; overflow-y: auto; border-radius: 8px; }
  .event { padding: 5px; border-bottom: 1px solid #333; }
  .event-type { color: #ff0; }
  .event-time { color: #666; font-size: 12px; }
  .ghost_appear { border-left: 3px solid #0f0; }
  .ghost_disappear { border-left: 3px solid #f00; }
  .ghost_interact { border-left: 3px solid #0ff; }
  .ghost_created { border-left: 3px solid #f0f; }
</style>
</head><body>
<h1>👻 GhostLayer Monitor</h1>
<div class="stats">
  <div class="stat"><div>Total Ghosts</div><div class="stat-value" id="total">-</div></div>
  <div class="stat"><div>Interactions</div><div class="stat-value" id="interactions">-</div></div>
  <div class="stat"><div>Last 24h</div><div class="stat-value" id="recent">-</div></div>
</div>
<h2>Activity Log</h2>
<div id="log"></div>
<script>
async function refresh() {
  const stats = await fetch('/api/monitor/stats').then(r => r.json());
  document.getElementById('total').textContent = stats.total_ghosts;
  document.getElementById('interactions').textContent = stats.total_interactions;
  document.getElementById('recent').textContent = stats.ghosts_last_24h;
  
  const activity = await fetch('/api/monitor/activity').then(r => r.json());
  document.getElementById('log').innerHTML = activity.map(e => 
    '<div class="event ' + e.type + '">' +
    '<span class="event-type">[' + e.type + ']</span> ' +
    JSON.stringify(e.data) +
    '<div class="event-time">' + e.timestamp + '</div></div>'
  ).join('');
}
refresh();
setInterval(refresh, 3000);
</script>
</body></html>`);
});

// === GHOST ENDPOINTS ===

// Create ghost via AI
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

// Get nearby ghosts
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
        client_id, 
        ghosts: ghosts.map(g => ({ id: g.id, name: g.name, distance: g.distance }))
      });
    }
    
    res.json(ghosts);
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

// Get ghost by ID
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

// Record interaction
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
      client_id
    });
    
    res.json({ success: true });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

// Client reports ghost left view
app.post('/api/ghosts/:id/disappear', async (req, res) => {
  try {
    const { client_id } = req.body;
    const ghost = await pool.query('SELECT name FROM ghosts WHERE id = $1', [req.params.id]);
    logActivity('ghost_disappear', { 
      ghost_id: parseInt(req.params.id),
      ghost_name: ghost.rows[0]?.name,
      client_id
    });
    res.json({ success: true });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

const PORT = process.env.PORT || 6000;
initDb().then(() => {
  app.listen(PORT, () => console.log(`API running on port ${PORT}`));
});
