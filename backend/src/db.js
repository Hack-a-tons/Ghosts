import pg from 'pg';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export const pool = new pg.Pool({
  host: process.env.DB_HOST || 'localhost',
  port: process.env.DB_PORT || 5432,
  user: process.env.DB_USER || 'ghosts',
  password: process.env.DB_PASSWORD || 'ghosts',
  database: process.env.DB_NAME || 'ghosts'
});

export async function initDb() {
  const migrationPath = path.join(__dirname, '../migrations/001_init.sql');
  if (fs.existsSync(migrationPath)) {
    const sql = fs.readFileSync(migrationPath, 'utf8');
    await pool.query(sql);
    console.log('Database initialized');
  }
}
