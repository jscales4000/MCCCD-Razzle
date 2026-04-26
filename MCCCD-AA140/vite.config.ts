import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import { readFileSync, existsSync } from 'fs';

// Workaround: Rollup URL-encodes '#' as '%23' in module IDs when the project
// path contains '#'. This plugin decodes '%23' back to '#' so files can be read.
const decodePoundSign = {
  name: 'decode-pound-sign',
  load(id: string) {
    if (id.includes('%23')) {
      const realPath = id.replace(/%23/g, '#');
      if (existsSync(realPath)) {
        return readFileSync(realPath, 'utf8');
      }
    }
    return null;
  }
};

export default defineConfig({
  plugins: [
    decodePoundSign,
    svelte(),
    viteStaticCopy({
      targets: [
        {
          src: 'public/cr-com-lib.js',
          dest: '.'
        }
      ]
    })
  ],
  base: './',
  build: {
    outDir: 'dist',
    target: 'es2015'
  }
});
