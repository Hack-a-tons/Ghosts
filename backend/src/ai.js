import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const SYSTEM_PROMPT = fs.existsSync(path.join(__dirname, '../../prompts/ghost-creator.md'))
  ? fs.readFileSync(path.join(__dirname, '../../prompts/ghost-creator.md'), 'utf8')
  : `You are a Ghost Creator. Generate ghost JSON with: name, personality, location, visibility_radius_m, interaction.`;

export async function generateGhost(prompt, lat, lng) {
  const endpoint = process.env.AZURE_OPENAI_ENDPOINT;
  const key = process.env.AZURE_OPENAI_KEY;

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

  const fullPrompt = `${SYSTEM_PROMPT}\n\nCreate a ghost at lat=${lat}, lng=${lng}. User request: ${prompt}\n\nRespond with valid JSON only.`;
  
  const res = await fetch(`https://${endpoint}/api/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-API-Key': key },
    body: JSON.stringify({ prompt: fullPrompt })
  });

  if (!res.ok) throw new Error(`AI error: ${res.status}`);
  const data = await res.json();
  
  // Extract JSON from response
  let content = data.text || data.response || data.content || '';
  
  // Try to extract JSON from markdown code blocks
  const jsonMatch = content.match(/```(?:json)?\s*([\s\S]*?)```/) || content.match(/(\{[\s\S]*\})/);
  if (jsonMatch) content = jsonMatch[1];

  const ghost = JSON.parse(content.trim());
  ghost.location = ghost.location || { lat, lng };
  ghost.location.lat = ghost.location.lat || lat;
  ghost.location.lng = ghost.location.lng || lng;
  ghost.visibility_radius_m = ghost.visibility_radius_m || 100;
  return ghost;
}
