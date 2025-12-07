import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const SYSTEM_PROMPT = fs.existsSync(path.join(__dirname, '../../prompts/ghost-creator.md'))
  ? fs.readFileSync(path.join(__dirname, '../../prompts/ghost-creator.md'), 'utf8')
  : `You are a Ghost Creator for an AR app. Generate ghost JSON with: name, personality, location, visibility_radius_m, interaction.`;

export async function generateGhost(prompt, lat, lng) {
  const endpoint = process.env.AZURE_OPENAI_ENDPOINT;
  const key = process.env.AZURE_OPENAI_KEY;
  const deployment = process.env.AZURE_OPENAI_DEPLOYMENT || 'gpt-4o';

  if (!endpoint || !key) {
    // Fallback for testing without AI
    return {
      name: 'Test Ghost',
      personality: 'Friendly',
      location: { lat, lng },
      visibility_radius_m: 100,
      interaction: { type: 'message', text: prompt }
    };
  }

  const url = `${endpoint}/openai/deployments/${deployment}/chat/completions?api-version=2024-02-15-preview`;
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'api-key': key },
    body: JSON.stringify({
      messages: [
        { role: 'system', content: SYSTEM_PROMPT },
        { role: 'user', content: `Create a ghost at lat=${lat}, lng=${lng}. User request: ${prompt}` }
      ],
      response_format: { type: 'json_object' }
    })
  });

  if (!res.ok) throw new Error(`AI error: ${res.status}`);
  const data = await res.json();
  const content = data.choices?.[0]?.message?.content;
  if (!content) throw new Error('No AI response');

  const ghost = JSON.parse(content);
  // Ensure location is set
  ghost.location = ghost.location || { lat, lng };
  ghost.location.lat = ghost.location.lat || lat;
  ghost.location.lng = ghost.location.lng || lng;
  ghost.visibility_radius_m = ghost.visibility_radius_m || 100;
  return ghost;
}
