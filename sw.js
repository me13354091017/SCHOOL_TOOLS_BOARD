/* Service Worker：离线可用（首次访问后断网也能上课） */
const VERSION = 'inkboard-v2';
const CORE = [
  './',
  './index.html',
  './manifest.json',
  './src/style.css',
  './src/style-ui-scale.css',
  './src/00-util.js',
  './src/10-geometry.js',
  './src/20-model.js',
  './src/30-render.js',
  './src/40-input.js',
  './src/50-tools.js',
  './src/60-ui.js',
  './src/65-ui-tools.js',
  './src/70-main.js',
  './icons/icon.svg',
];

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(VERSION).then((c) => c.addAll(CORE)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((k) => k !== VERSION).map((k) => caches.delete(k))).then(() => self.clients.claim()));
});

self.addEventListener('fetch', (e) => {
  const req = e.request;
  if (req.method !== 'GET') return;
  e.respondWith(
    caches.match(req).then((hit) => {
      if (hit) return hit;
      return fetch(req).then((res) => {
        const copy = res.clone();
        caches.open(VERSION).then((c) => c.put(req, copy)).catch(() => {});
        return res;
      }).catch(() => caches.match('./index.html'));
    })
  );
});
