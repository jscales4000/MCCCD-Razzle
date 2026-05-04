/**
 * Vite build workaround for projects with '#' in the path.
 *
 * Rollup URL-encodes '#' as '%23' in module IDs, breaking relative import
 * resolution for all source files. This script copies the project sources to
 * a temp directory (no '#'), builds from there, then copies dist/ back.
 *
 * Post-build: rewrites the HTML for Crestron panel compatibility — module
 * script moved into <body>, no crossorigin attributes, runtime <script> tags
 * for cr-com-lib.js and ch5-components.js preserved at the top of <head>.
 */

import { cpSync, mkdirSync, rmSync, existsSync, readFileSync, writeFileSync } from 'fs';
import { execSync } from 'child_process';
import { join } from 'path';
import os from 'os';

const projectDir = process.cwd();
const pkg = JSON.parse(readFileSync(join(projectDir, 'package.json'), 'utf8'));
const projectName = pkg.name;
const tmpDir = join(os.tmpdir(), `${projectName}-ch5panel-tmp`);

const include = ['src', 'public', 'node_modules', 'index.html',
  'vite.config.ts', 'svelte.config.js', 'tsconfig.json', 'package.json', 'config.json'];

console.log(`[build] Temp dir: ${tmpDir}`);

if (existsSync(tmpDir)) rmSync(tmpDir, { recursive: true });
mkdirSync(tmpDir, { recursive: true });

for (const item of include) {
  const src = join(projectDir, item);
  if (existsSync(src)) {
    cpSync(src, join(tmpDir, item), { recursive: true });
  }
}

console.log('[build] Running vite build...');
const viteCommand = process.platform === 'win32'
  ? '.\\node_modules\\.bin\\vite.cmd build'
  : './node_modules/.bin/vite build';
execSync(viteCommand, { cwd: tmpDir, stdio: 'inherit' });

const distSrc = join(tmpDir, 'dist');
const distDest = join(projectDir, 'dist');
if (existsSync(distDest)) rmSync(distDest, { recursive: true });
cpSync(distSrc, distDest, { recursive: true });

rmSync(tmpDir, { recursive: true });

const indexPath = join(distDest, 'index.html');
const viteHtml = readFileSync(indexPath, 'utf8');

const jsMatch = viteHtml.match(/src="\.\/assets\/(index-[^"]+\.js)"/);
const cssMatch = viteHtml.match(/href="\.\/assets\/(index-[^"]+\.css)"/);
const jsFile = jsMatch ? jsMatch[1] : 'index.js';
const cssFile = cssMatch ? cssMatch[1] : 'index.css';

const htmlTitleMatch = viteHtml.match(/<title>([^<]+)<\/title>/);
const htmlTitle = htmlTitleMatch ? htmlTitleMatch[1] : projectName;

// CH5 Video Specialist requires:
//   - Scripts at END of body (not head) — DGE/TS Android Chromium otherwise
//     fails to upgrade ch5-video / ch5-background custom elements
//   - body/html transparent — handled in src/global.css
//   - <ch5-background> as a direct child of <body>, BEFORE the app mount —
//     its IntersectionObserver only flips isInitialized=true when visible
//     with non-zero dimensions, so it must exist in the DOM at first paint
//   - DO NOT set CSS background-color on ch5-background — the CSS paints over
//     the native compositor surface and blocks the video cutout. Use the
//     `backgroundcolor` attribute instead, which the native renderer honors.
const panelHtml = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, height=device-height, initial-scale=1.0, user-scalable=no" />
  <title>${htmlTitle}</title>
  <!-- ch5-theme.css MUST load before our app CSS so we can override the few
       generic rules (body font-size). It supplies the internal styles for
       <ch5-video> (display:inline-block, aspect-ratio classes, full-screen-icon
       positioning). Without it ch5-video renders at wrong dimensions and the
       fullscreen icon ships unstyled. -->
  <link rel="stylesheet" href="./ch5-theme.css">
  <link rel="stylesheet" href="./assets/${cssFile}">
</head>
<body>
  <!-- z-index:-1 sits ch5-background behind the Svelte UI without painting a
       CSS background (which would block the native cutout). The dark color is
       rendered by the native compositor via the backgroundcolor attribute. -->
  <ch5-background backgroundcolor="#0f172a" style="position:fixed;inset:0;z-index:-1;"></ch5-background>

  <!-- ch5-video lives at body level (proven 1beyond-multicam pattern). When
       Cameras.svelte mounts it positions this element to match a placeholder
       div via getBoundingClientRect + ResizeObserver, then sets display:block.
       Other pages leave display:none so the cutout is invisible.
       Sizing inside a Svelte component tree never reaches ch5-video reliably;
       hoisting it here gives the JS a stable host whose rect we control. -->
  <ch5-video
    id="cam-preview"
    sourcetype="Network"
    url="rtsp://admin:crestron@192.168.1.172:554/1.h264"
    userid="admin"
    password="crestron"
    size="custom"
    aspectratio="16:9"
    stretch="true"
    componentwasresized="true"
    receivestateplay="CamPlay"
    sendeventstate="CamState"
    sendeventerrorcode="CamError"
    sendeventerrormessage="CamErrorMsg"
    sendeventresolution="CamResolution"
    style="display:none;position:fixed;z-index:0;"
  ></ch5-video>

  <div id="app" style="position:relative;z-index:1;"></div>
${process.env.BUILD_DEBUG_OVERLAY === '1' ? `  <div id="debug" style="position:fixed;bottom:0;left:0;right:0;background:red;color:white;font-size:24px;padding:20px;z-index:99999;display:none;"></div>
  <script>
    window.onerror = function(msg, url, line, col, err) {
      var d = document.getElementById('debug');
      d.style.display = 'block';
      d.innerHTML = 'ERROR: ' + msg + '<br>File: ' + url + '<br>Line: ' + line + ':' + col;
      return false;
    };
  </script>
` : ''}  <script src="./cr-com-lib.js"></script>
  <script src="./ch5-components.js"></script>
  <script type="module" src="./assets/${jsFile}"></script>
</body>
</html>
`;

writeFileSync(indexPath, panelHtml);
console.log('[build] Replaced index.html with panel-compatible version');
console.log('[build] Done. Output in dist/');
