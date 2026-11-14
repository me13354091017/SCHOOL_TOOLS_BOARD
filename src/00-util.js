/* ==========================================================================
 * 00-util.js — 基础工具库
 * InkBoard · SCHOOL_TOOLS_BOARD
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});

  const uid = (prefix = 'o') =>
    prefix + '_' + Date.now().toString(36) + Math.random().toString(36).slice(2, 8);

  const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);
  const lerp = (a, b, t) => a + (b - a) * t;
  const dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
  const now = () => performance.now();

  function debounce(fn, ms) {
    let t = 0;
    return function (...args) {
      clearTimeout(t);
      t = setTimeout(() => fn.apply(this, args), ms);
    };
  }

  /** 轻量事件总线 */
  class Emitter {
    constructor() { this._m = new Map(); }
    on(k, f) { (this._m.get(k) || this._m.set(k, []).get(k)).push(f); return () => this.off(k, f); }
    off(k, f) { const a = this._m.get(k); if (a) { const i = a.indexOf(f); if (i >= 0) a.splice(i, 1); } }
    emit(k, ...a) { const l = this._m.get(k); if (l) for (const f of l.slice()) { try { f(...a); } catch (e) { console.error(e); } } }
  }

  /** 文件 / 图片 */
  function download(blob, filename) {
    const url = typeof blob === 'string' && blob.startsWith('blob:') ? blob : URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename;
    document.body.appendChild(a); a.click(); a.remove();
    if (url.startsWith('blob:')) setTimeout(() => URL.revokeObjectURL(url), 4000);
  }

  function pickFile(accept, multiple = false) {
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file'; input.accept = accept; input.multiple = multiple;
      input.style.position = 'fixed'; input.style.left = '-2000px';
      document.body.appendChild(input);
      input.addEventListener('change', () => {
        const files = Array.from(input.files || []);
        input.remove();
        resolve(multiple ? files : files[0] || null);
      });
      input.click();
    });
  }

  function fileToDataURL(file) {
    return new Promise((resolve, reject) => {
      const r = new FileReader();
      r.onload = () => resolve(r.result);
      r.onerror = reject;
      r.readAsDataURL(file);
    });
  }

  function loadImage(src) {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = reject;
      img.src = src;
    });
  }

  function formatTime(sec) {
    sec = Math.max(0, Math.round(sec));
    const m = Math.floor(sec / 60), s = sec % 60;
    return String(m).padStart(2, '0') + ':' + String(s).padStart(2, '0');
  }

  /** 简单 IndexedDB 键值存储（带内存回退） */
  const Store = {
    _db: null, _mem: new Map(),
    async open() {
      if (this._db) return this._db;
      this._db = await new Promise((resolve) => {
        try {
          const req = indexedDB.open('inkboard', 1);
          req.onupgradeneeded = () => { const db = req.result; if (!db.objectStoreNames.contains('kv')) db.createObjectStore('kv'); };
          req.onsuccess = () => resolve(req.result);
          req.onerror = () => resolve(null);
        } catch (e) { resolve(null); }
      });
      return this._db;
    },
    async get(key) {
      const db = await this.open();
      if (!db) return this._mem.get(key);
      return new Promise((resolve) => {
        const tx = db.transaction('kv', 'readonly').objectStore('kv').get(key);
        tx.onsuccess = () => resolve(tx.result);
        tx.onerror = () => resolve(this._mem.get(key));
      });
    },
    async set(key, val) {
      this._mem.set(key, val);
      const db = await this.open();
      if (!db) return;
      return new Promise((resolve) => {
        const tx = db.transaction('kv', 'readwrite').objectStore('kv').put(val, key);
        tx.onsuccess = () => resolve(); tx.onerror = () => resolve();
      });
    },
    async del(key) {
      this._mem.delete(key);
      const db = await this.open();
      if (!db) return;
      return new Promise((resolve) => {
        const tx = db.transaction('kv', 'readwrite').objectStore('kv').delete(key);
        tx.onsuccess = () => resolve(); tx.onerror = () => resolve();
      });
    },
  };

  IB.util = { uid, clamp, lerp, dist, now, debounce, Emitter, download, pickFile, fileToDataURL, loadImage, formatTime, Store };
  IB.Emitter = Emitter;
})();
