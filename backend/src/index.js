import express from 'express';
import { pool, initDb } from './db.js';
import { generateGhost } from './ai.js';

const app = express();
app.use(express.json());

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
    res.json({ id: result.rows[0].id, ...ghost });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: e.message });
  }
});

// Get nearby ghosts
app.get('/api/ghosts', async (req, res) => {
  try {
    const { lat, lng, radius = 1000 } = req.query;
    let query = 'SELECT * FROM ghosts ORDER BY created_at DESC LIMIT 100';
    let params = [];
    if (lat && lng) {
      // Haversine distance filter (approximate)
      query = `SELECT *, 
        (6371000 * acos(cos(radians($1)) * cos(radians(lat)) * cos(radians(lng) - radians($2)) + sin(radians($1)) * sin(radians(lat)))) as distance
        FROM ghosts
        WHERE (6371000 * acos(cos(radians($1)) * cos(radians(lat)) * cos(radians(lng) - radians($2)) + sin(radians($1)) * sin(radians(lat)))) < $3
        ORDER BY distance LIMIT 100`;
      params = [lat, lng, radius];
    }
    const result = await pool.query(query, params);
    res.json(result.rows.map(r => ({
      id: r.id,
      name: r.name,
      personality: r.personality,
      location: { lat: parseFloat(r.lat), lng: parseFloat(r.lng) },
      visibility_radius_m: r.visibility_radius_m,
      interaction: r.interaction,
      distance: r.distance ? Math.round(r.distance) : null
    })));
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
    await pool.query(
      'UPDATE ghosts SET interactions = interactions + 1 WHERE id = $1',
      [req.params.id]
    );
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
