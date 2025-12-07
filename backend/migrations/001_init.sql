CREATE TABLE IF NOT EXISTS ghosts (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    personality TEXT,
    lat DECIMAL(10, 7) NOT NULL,
    lng DECIMAL(10, 7) NOT NULL,
    visibility_radius_m INTEGER DEFAULT 100,
    interaction JSONB,
    interactions INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ghosts_location ON ghosts (lat, lng);
