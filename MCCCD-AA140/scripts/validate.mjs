import { existsSync, readFileSync, readdirSync } from 'fs';

const requiredFiles = [
  'public/cr-com-lib.js',
  'public/ch5-components.js',
  'public/appui/manifest',
  'docs/SIGNAL-MAP.md',
  'src/lib/contract.ts'
];

const missing = requiredFiles.filter((file) => !existsSync(file));

if (missing.length > 0) {
  console.error('[validate] Missing required files:');
  for (const file of missing) {
    console.error(`- ${file}`);
  }
  process.exit(1);
}

const cceFiles = readdirSync('contracts').filter((f) => f.endsWith('.cce'));
if (cceFiles.length === 0) {
  console.error('[validate] contracts/ contains no .cce file. Add one based on the small-seed template.');
  process.exit(1);
}

const indexHtml = readFileSync('index.html', 'utf8');
if (!indexHtml.includes('cr-com-lib.js') || !indexHtml.includes('ch5-components.js')) {
  console.error('[validate] index.html is missing Crestron runtime script tags.');
  process.exit(1);
}

const contractTs = readFileSync('src/lib/contract.ts', 'utf8');
const roomNameMatch = contractTs.match(/export const ROOM_NAME = ['"]([^'"]+)['"]/);
if (!roomNameMatch) {
  console.error('[validate] src/lib/contract.ts is missing an exported ROOM_NAME constant.');
  process.exit(1);
}
const roomName = roomNameMatch[1];

const signalMap = readFileSync('docs/SIGNAL-MAP.md', 'utf8');
if (!signalMap.includes(roomName) && !signalMap.includes('<ROOM_NAME>')) {
  console.error(`[validate] docs/SIGNAL-MAP.md does not reference ROOM_NAME "${roomName}" (or the <ROOM_NAME> placeholder).`);
  process.exit(1);
}

console.log(`[validate] CH5 project structure looks correct (ROOM_NAME=${roomName}).`);
