// City Walk web preview. Scene, game rules and a scripted playthrough.
// Numbers (grid, speeds, camera) come from the Unity starter so the logic ports back to C#.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import * as SkeletonUtils from 'three/addons/utils/SkeletonUtils.js';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { MeshoptSimplifier } from './node_modules/meshoptimizer/meshopt_simplifier.js';

const W = 1920, H = 1080, FPS = 30, DT = 1 / FPS;
const BLOCK = 24, ROAD = 10, P = BLOCK + ROAD, NB = 5;
const WALK = 3.2, BRISK = 5.5, ACCEL = 10, TURN = 540 * Math.PI / 180;
const $ = id => document.getElementById(id);
const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));
const lerp = (a, b, t) => a + (b - a) * t;
const smooth = (a, b, x) => { const t = clamp((x - a) / (b - a)); return t * t * (3 - 2 * t); };
const wrapA = a => Math.atan2(Math.sin(a), Math.cos(a));
function rng32(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
const rng = rng32(7);

// ---------- renderer ----------
const QS = new URLSearchParams(location.search), RS = +(QS.get('rs') || 0.66667);
const renderer = new THREE.WebGLRenderer({ canvas: $('c'), antialias: QS.get('aa') !== '0', preserveDrawingBuffer: QS.has('pdb') });
renderer.setPixelRatio(1); renderer.setSize(Math.round(W * RS), Math.round(H * RS), false);
renderer.shadowMap.enabled = !QS.has('noshadow'); renderer.shadowMap.type = QS.get('sh') === 'soft' ? THREE.PCFSoftShadowMap : THREE.PCFShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.05;
const scene = new THREE.Scene();
{
  const cv = document.createElement('canvas'); cv.width = 4; cv.height = 256; const g = cv.getContext('2d');
  const gr = g.createLinearGradient(0, 0, 0, 256); gr.addColorStop(0, '#4f86c6'); gr.addColorStop(0.45, '#9cc4ea'); gr.addColorStop(1, '#cfe2f3');
  g.fillStyle = gr; g.fillRect(0, 0, 4, 256);
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; scene.background = t;
}
scene.fog = new THREE.Fog(0xb9d3ec, 70, 230);
const camera = new THREE.PerspectiveCamera(55, W / H, 0.1, 400);
scene.add(new THREE.HemisphereLight(0xd6e4f7, 0x6a6258, 2.1));
const sun = new THREE.DirectionalLight(0xffedd0, 2.5);
const SUN_DIR = new THREE.Vector3(-0.42, 0.80, 0.43).normalize();
sun.castShadow = true; { const ms = +(QS.get('sm') || 1024); sun.shadow.mapSize.set(ms, ms); }
Object.assign(sun.shadow.camera, { left: -60, right: 60, top: 60, bottom: -60, near: 1, far: 260 });
sun.shadow.bias = -0.0005; sun.shadow.normalBias = 0.05;
scene.add(sun, sun.target);

// ---------- city ----------
const boxes = []; // camera collision volumes {x0,x1,z0,z1,h}
const lam = c => new THREE.MeshLambertMaterial({ color: c });
function addBox(w, h, d, x, y, z, mat, cast = false, recv = false) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat); m.position.set(x, y, z);
  m.castShadow = cast; m.receiveShadow = recv; m.matrixAutoUpdate = false; m.updateMatrix(); scene.add(m); return m;
}
function windowTexture() {
  const cv = document.createElement('canvas'); cv.width = cv.height = 512; const g = cv.getContext('2d'); const r = rng32(99);
  g.fillStyle = '#ffffff'; g.fillRect(0, 0, 512, 512);
  for (let i = 0; i < 8; i++) for (let j = 0; j < 8; j++) {
    g.fillStyle = r() < 0.24 ? '#f3d58c' : (r() < 0.5 ? '#2b323b' : '#353d47');
    g.fillRect(i * 64 + 15, j * 64 + 12, 34, 40);
  }
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 4; return t;
}
const winTex = windowTexture();
const BCOL = [0xc9b79c, 0x9aa3ad, 0xd8c7a8, 0x7f8892, 0xb89678, 0xddd6c8];
const cityGeos = [], paveGeos = [];
function cityBox(w, h, d, x, y, z, wallCol, roofCol, windows) {
  const g = new THREE.BoxGeometry(w, h, d), uv = g.attributes.uv, col = new Float32Array(24 * 3);
  const ou = Math.floor(rng() * 8) / 8, ov = Math.floor(rng() * 8) / 8;
  for (let f = 0; f < 6; f++) { // 4 verts per face: +x -x +y -y +z -z
    const top = f === 2 || f === 3, wide = (f < 2 ? d : w), nu = Math.max(1, Math.round(wide / 1.7)) / 8, nv = Math.max(1, Math.round(h / 2.4)) / 8, c = top ? roofCol : wallCol;
    for (let k = 0; k < 4; k++) { const i = f * 4 + k;
      if (top || !windows) uv.setXY(i, 0.004, 0.004); else uv.setXY(i, uv.getX(i) * nu + ou, uv.getY(i) * nv + ov);
      col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b; }
  }
  g.setAttribute('color', new THREE.BufferAttribute(col, 3)); g.translate(x, y, z); cityGeos.push(g);
}
function building(x, z, w, h, d, ci, tint) {
  const wc = new THREE.Color(tint || BCOL[ci]), rc = wc.clone().multiplyScalar(0.7);
  cityBox(w, h, d, x, 0.15 + h / 2, z, wc, rc, true);
  cityBox(w * 0.9, 0.8, d * 0.9, x, 0.15 + h + 0.4, z, rc, rc, false);
  boxes.push({ x0: x - w / 2, x1: x + w / 2, z0: z - d / 2, z1: z + d / 2, h: h + 1 });
}
const blockC = (i, j) => [(i - 2) * P, (j - 2) * P];
function buildCity() {
  const half = NB * P / 2;
  const ground = new THREE.Mesh(new THREE.PlaneGeometry(half * 2 + 60, half * 2 + 60), lam(0x3a3d43));
  ground.rotation.x = -Math.PI / 2; ground.receiveShadow = true; scene.add(ground);
  // lane dashes
  const dashes = [];
  for (let i = 1; i < NB; i++) { const p = -half + i * P; for (let t = -half; t < half; t += 4) { dashes.push([p, t + 1, 0]); dashes.push([t + 1, p, 1]); } }
  const dm = new THREE.InstancedMesh(new THREE.BoxGeometry(0.18, 0.02, 1.8), new THREE.MeshLambertMaterial({ color: 0xe6e1cf }), dashes.length);
  const o = new THREE.Object3D();
  dashes.forEach((d, k) => { o.position.set(d[0], 0.011, d[1]); o.rotation.y = d[2] ? Math.PI / 2 : 0; o.updateMatrix(); dm.setMatrixAt(k, o.matrix); });
  dm.receiveShadow = true; scene.add(dm);
  const pave = lam(0xa9a59a), lamps = [];
  for (let i = 0; i < NB; i++) for (let j = 0; j < NB; j++) {
    const [cx, cz] = blockC(i, j);
    { const g = new THREE.BoxGeometry(BLOCK, 0.15, BLOCK); g.translate(cx, 0.075, cz); paveGeos.push(g); }
    const e = BLOCK / 2 - 0.6; for (const sx of [-1, 1]) for (const sz of [-1, 1]) lamps.push([cx + sx * e, cz + sz * e]);
    if (i === 2 && j === 2) { buildPark(cx, cz); continue; }
    const inset = 2.5, cell = (BLOCK - inset * 2) / 2;
    for (let a = 0; a < 2; a++) for (let b = 0; b < 2; b++) {
      let w = cell - 1.2 - rng() * 2, d = cell - 1.2 - rng() * 2, h = 8 + rng() * 34; const ci = Math.floor(rng() * BCOL.length);
      const x = cx - BLOCK / 2 + inset + cell * (a + 0.5), z = cz - BLOCK / 2 + inset + cell * (b + 0.5);
      if (i === 3 && j === 3 && a === 0 && b === 0) continue;              // kiosk plaza
      if (i === 3 && j === 2 && a === 0) { w = 8.3; d = 6.5; h = b ? 26 : 32; } // 3 m alley between these two, used for the camera shot
      const office = (i === 4 && j === 2 && a === 0 && b === 1);
      if (office) { w = 8.3; d = 8.3; h = 40; }
      let tint = office ? 0x3f7a66 : 0;
      if (i === 2 && j === 1 && a === 0 && b === 1) { w = 8.3; d = 8.3; h = 13; tint = 0xe9e3d3; }  // bank
      if (i === 1 && j === 2 && a === 1 && b === 0) { w = 8.3; h = Math.max(h, 16); }                // cafe frontage
      building(x, z, w, h, d, ci, tint);
    }
  }
  const cm = new THREE.Mesh(mergeGeometries(cityGeos), new THREE.MeshLambertMaterial({ map: winTex, vertexColors: true })); cm.castShadow = cm.receiveShadow = true; scene.add(cm);
  const pm = new THREE.Mesh(mergeGeometries(paveGeos), pave); pm.receiveShadow = true; scene.add(pm);
  // street lamps
  const pole = new THREE.InstancedMesh(new THREE.CylinderGeometry(0.06, 0.06, 4, 6), lam(0x2a2b30), lamps.length);
  const bulb = new THREE.InstancedMesh(new THREE.SphereGeometry(0.2, 10, 8), new THREE.MeshBasicMaterial({ color: 0xfff3d0 }), lamps.length);
  lamps.forEach((l, k) => { o.rotation.set(0, 0, 0); o.position.set(l[0], 2.15, l[1]); o.updateMatrix(); pole.setMatrixAt(k, o.matrix); o.position.y = 4.25; o.updateMatrix(); bulb.setMatrixAt(k, o.matrix); });
  pole.castShadow = true; scene.add(pole, bulb);
}
function buildPark(cx, cz) {
  addBox(BLOCK - 3, 0.05, BLOCK - 3, cx, 0.175, cz, lam(0x4f8a3f), false, true);
  const stone = lam(0xc4c1b6);
  const cyl = (r, h, y, mat, cast) => { const m = new THREE.Mesh(new THREE.CylinderGeometry(r, r, h, 24), mat); m.position.set(cx, y, cz); m.castShadow = !!cast; m.receiveShadow = true; scene.add(m); };
  cyl(3, 0.6, 0.45, stone, true); cyl(2.6, 0.1, 0.72, lam(0x5a8fc4)); cyl(0.3, 2.4, 1.6, stone, true);
  const trunk = lam(0x5e4430), leaf = new THREE.MeshLambertMaterial({ color: 0x3d7a34, flatShading: true });
  for (let k = 0; k < 8; k++) {
    const a = k * Math.PI / 4 + 0.4, r = BLOCK * 0.36, x = cx + Math.cos(a) * r, z = cz + Math.sin(a) * r;
    const t = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.2, 2.6, 8), trunk); t.position.set(x, 1.3, z); t.castShadow = true; scene.add(t);
    const l = new THREE.Mesh(new THREE.IcosahedronGeometry(1.35, 1), leaf); l.position.set(x, 3.4, z); l.scale.set(1, 0.92, 1); l.castShadow = true; scene.add(l);
  }
}
function signTexture(text, fg, bg) {
  const cv = document.createElement('canvas'); cv.width = 512; cv.height = 128; const g = cv.getContext('2d');
  g.fillStyle = bg; g.fillRect(0, 0, 512, 128); g.fillStyle = fg; g.font = 'bold 84px Arial, sans-serif'; g.textAlign = 'center'; g.textBaseline = 'middle';
  g.fillText(text, 256, 68); const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}
const KIOSK = { x: P / 2, z: P / 2 }, OFFICE = { x: P * 1.5, z: P / 2 };
function buildKioskAndOffice() {
  const green = lam(0x165a47), gold = lam(0xd9ae4f);
  const kx = 27.6, kz = 27.2;
  addBox(4.2, 2.6, 3, kx, 0.15 + 1.3, kz, green, true, true);
  addBox(4.8, 0.25, 3.8, kx, 2.95, kz, gold, true);
  addBox(3.2, 1.0, 0.1, kx, 1.75, kz - 1.52, lam(0x0c2f26));
  const s1 = new THREE.Mesh(new THREE.PlaneGeometry(3.6, 0.9), new THREE.MeshBasicMaterial({ map: signTexture('COFFEE', '#0c3b30', '#e3bd62') }));
  s1.position.set(kx, 3.55, kz - 1.9); s1.rotation.y = Math.PI; scene.add(s1);
  const s1b = s1.clone(); s1b.position.set(kx - 2.4, 3.55, kz); s1b.rotation.y = -Math.PI / 2; scene.add(s1b);
  boxes.push({ x0: kx - 2.4, x1: kx + 2.4, z0: kz - 1.9, z1: kz + 1.9, h: 3.2 });
  // office pylon on the corner facing the approach road
  const px = 58.4, pz = 10.4;
  addBox(0.35, 4.4, 2.6, px, 0.15 + 2.2, pz, green, true);
  addBox(0.45, 0.2, 2.8, px, 4.65, pz, gold);
  const s2 = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 0.6), new THREE.MeshBasicMaterial({ map: signTexture('OFFICE', '#e9c568', '#165a47') }));
  s2.position.set(px - 0.19, 3.6, pz); s2.rotation.y = -Math.PI / 2; scene.add(s2);
}

// ---------- characters ----------
let gltf = null;
function simplified(g0, ratio) {
  const key = 'lod' + ratio; if (g0.userData[key]) return g0.userData[key];
  const pos = g0.attributes.position, idx = new Uint32Array(g0.index.array), target = Math.floor(idx.length * ratio / 3) * 3;
  const [ni] = MeshoptSimplifier.simplify(idx, new Float32Array(pos.array), 3, target, 0.05);
  const remap = new Int32Array(pos.count).fill(-1); let n = 0; for (const i of ni) if (remap[i] < 0) remap[i] = n++;
  const g = new THREE.BufferGeometry();
  for (const k in g0.attributes) { const a = g0.attributes[k], Arr = a.array.constructor, out = new Arr(n * a.itemSize);
    for (let i = 0; i < pos.count; i++) if (remap[i] >= 0) for (let c = 0; c < a.itemSize; c++) out[remap[i] * a.itemSize + c] = a.array[i * a.itemSize + c];
    g.setAttribute(k, new THREE.BufferAttribute(out, a.itemSize, a.normalized)); }
  g.setIndex(Array.from(ni, i => remap[i])); g0.userData[key] = g; return g;
}
function partOf(n) { if (/Head/.test(n)) return 2; if (/Neck/.test(n)) return 4; if (/Hand/.test(n)) return 2; if (/Foot|Toe/.test(n)) return 3; if (/Leg|Hips/.test(n)) return 1; return 0; }
function makeCharacter(pal, opts = {}) {
  const root = SkeletonUtils.clone(gltf.scene);
  const cols = [pal.jacket, pal.trousers, pal.skin, 0x15130f, 0xf1eee6].map(c => new THREE.Color(c));
  let head = null, hand = null, rhand = null;
  root.traverse(o => {
    if (o.isBone && /Head$/.test(o.name)) head = o;
    if (o.isBone && /LeftHand$/.test(o.name)) hand = o;
    if (o.isBone && /RightHand$/.test(o.name)) rhand = o;
    if (!o.isSkinnedMesh) return;
    const g0 = simplified(o.geometry, opts.briefcase ? 0.5 : 0.1); let parts = g0.userData.parts;
    if (!parts) {
      const si = g0.attributes.skinIndex, sw = g0.attributes.skinWeight; parts = new Uint8Array(si.count);
      for (let i = 0; i < si.count; i++) { let b = 0, bw = -1; for (let k = 0; k < 4; k++) { const w = sw.getComponent(i, k); if (w > bw) { bw = w; b = si.getComponent(i, k); } } parts[i] = partOf(o.skeleton.bones[b].name); }
      g0.userData.parts = parts;
    }
    const g = new THREE.BufferGeometry(); for (const k in g0.attributes) g.setAttribute(k, g0.attributes[k]); g.setIndex(g0.index);
    const col = new Float32Array(parts.length * 3); for (let i = 0; i < parts.length; i++) { const c = cols[parts[i]]; col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b; }
    g.setAttribute('color', new THREE.BufferAttribute(col, 3));
    o.geometry = g; o.material = new THREE.MeshLambertMaterial({ vertexColors: true }); o.castShadow = !QS.has('nops') || !!opts.briefcase; o.frustumCulled = false;
  });
  root.updateMatrixWorld(true);
  const ws = new THREE.Vector3(); head.getWorldScale(ws); const k = 1 / ws.x;
  const hair = new THREE.Mesh(new THREE.SphereGeometry(0.118 * k, 14, 10, 0, Math.PI * 2, 0, Math.PI * 0.56), lam(pal.hair));
  hair.position.set(0, 0.105 * k, 0.004 * k); hair.rotation.x = -0.25; hair.castShadow = true; head.add(hair);
  if (opts.briefcase) {
    const bc = new THREE.Mesh(new THREE.BoxGeometry(0.09, 0.30, 0.42), lam(0x5a3a22)); bc.castShadow = true; scene.add(bc);
    const hd = new THREE.Mesh(new THREE.BoxGeometry(0.03, 0.07, 0.14), lam(0x1c1410)); hd.position.y = 0.18; bc.add(hd);
    const cup = new THREE.Mesh(new THREE.CylinderGeometry(0.045, 0.035, 0.12, 12), lam(0xf4f0e6)); cup.castShadow = true; scene.add(cup);
    const lid = new THREE.Mesh(new THREE.CylinderGeometry(0.048, 0.048, 0.02, 12), lam(0x5a3a22)); lid.position.y = 0.07; cup.add(lid);
    root.userData.brief = { bc, hand, cup, rhand };
  }
  const mixer = new THREE.AnimationMixer(root); const act = {};
  for (const n of ['idle', 'walk', 'run']) { act[n] = mixer.clipAction(THREE.AnimationClip.findByName(gltf.animations, n)); act[n].play(); act[n].setEffectiveWeight(n === 'idle' ? 1 : 0); }
  if (opts.scale) root.scale.setScalar(opts.scale);
  scene.add(root);
  return { root, mixer, act };
}

// ---------- paths ----------
class Path {
  constructor(pts, closed = false) {
    this.p = pts.map(q => ({ x: q[0], z: q[1] })); this.closed = closed; this.cum = [0];
    for (let i = 1; i < this.p.length; i++) this.cum.push(this.cum[i - 1] + Math.hypot(this.p[i].x - this.p[i - 1].x, this.p[i].z - this.p[i - 1].z));
    this.len = this.cum[this.cum.length - 1];
  }
  at(s) {
    if (this.closed) s = ((s % this.len) + this.len) % this.len; else s = clamp(s, 0, this.len);
    let i = 1; while (i < this.p.length - 1 && this.cum[i] < s) i++;
    const a = this.p[i - 1], b = this.p[i], L = this.cum[i] - this.cum[i - 1] || 1, t = (s - this.cum[i - 1]) / L;
    return { x: lerp(a.x, b.x, t), z: lerp(a.z, b.z, t), h: Math.atan2(b.x - a.x, b.z - a.z) };
  }
  closest(x, z, sMin = -1e9, sMax = 1e9) {
    let best = 1e9, bs = 0;
    for (let i = 1; i < this.p.length; i++) {
      if (this.cum[i] < sMin || this.cum[i - 1] > sMax) continue;
      const a = this.p[i - 1], b = this.p[i], dx = b.x - a.x, dz = b.z - a.z, L2 = dx * dx + dz * dz || 1;
      const t = clamp(((x - a.x) * dx + (z - a.z) * dz) / L2), d = Math.hypot(a.x + dx * t - x, a.z + dz * t - z);
      if (d < best - 1e-6) { best = d; bs = this.cum[i - 1] + t * Math.sqrt(L2); }
    }
    return bs;
  }
}
function fillet(corners, r) { // closed polygon -> point list with rounded corners
  const out = [], n = corners.length;
  for (let i = 0; i < n; i++) {
    const A = corners[(i + n - 1) % n], C = corners[i], B = corners[(i + 1) % n];
    const l1 = Math.hypot(C[0] - A[0], C[1] - A[1]), l2 = Math.hypot(B[0] - C[0], B[1] - C[1]);
    const p0 = [C[0] - (C[0] - A[0]) / l1 * r, C[1] - (C[1] - A[1]) / l1 * r], p1 = [C[0] + (B[0] - C[0]) / l2 * r, C[1] + (B[1] - C[1]) / l2 * r];
    for (let k = 0; k <= 6; k++) { const t = k / 6, u = 1 - t; out.push([u * u * p0[0] + 2 * u * t * C[0] + t * t * p1[0], u * u * p0[1] + 2 * u * t * C[1] + t * t * p1[1]]); }
  }
  return out;
}
const R = P / 2, START = [-10, R];
const loopPath = new Path([START, ...fillet([[R, R], [R, -R], [-R, -R], [-R, R]], 3.5), START], true);
const KIOSK_S = loopPath.closest(P / 2, P / 2);
const officePath = new Path([START, [OFFICE.x, OFFICE.z]]);

// ---------- player ----------
const player = { x: START[0], z: START[1], y: 0, h: Math.PI / 2, vx: 0, vz: 0, speed: 0, char: null };
const input = { dx: 0, dz: 0, str: 0 };
const onBlock = (x, z) => { const fx = ((x + P * 2.5) % P + P) % P, fz = ((z + P * 2.5) % P + P) % P; return fx > ROAD / 2 && fx < P - ROAD / 2 && fz > ROAD / 2 && fz < P - ROAD / 2; };
function updatePlayer(dt) {
  const stunned = simT < game.stunT || game.phase === 'failed';
  const target = (stunned || input.str < 0.05) ? 0 : (input.str >= 0.9 ? BRISK : WALK);
  const tx = input.dx * target, tz = input.dz * target, ex = tx - player.vx, ez = tz - player.vz, e = Math.hypot(ex, ez), step = ACCEL * dt;
  if (e <= step) { player.vx = tx; player.vz = tz; } else { player.vx += ex / e * step; player.vz += ez / e * step; }
  player.x += player.vx * dt; player.z += player.vz * dt; player.speed = Math.hypot(player.vx, player.vz);
  if (player.speed > 0.1 && !stunned) { const d = wrapA(Math.atan2(player.vx, player.vz) - player.h); player.h += clamp(d, -TURN * dt, TURN * dt); }
  player.y = lerp(player.y, onBlock(player.x, player.z) ? 0.15 : 0, 1 - Math.exp(-14 * dt));
  // idle / walk / brisk blend driven by speed
  const a = player.char.act, v = player.speed, move = clamp(v / WALK), r = clamp((v - WALK) / (BRISK - WALK)), wr = move * r * 0.5, ww = move - wr;
  a.idle.setEffectiveWeight(1 - move); a.walk.setEffectiveWeight(ww); a.run.setEffectiveWeight(wr);
  const Dw = a.walk.getClip().duration, Dr = a.run.getClip().duration, b = wr / Math.max(1e-4, wr + ww), D = lerp(Dw, Dr, b), kc = lerp(1.15, 1.3, r) * lerp(0.6, 1, move);
  a.walk.setEffectiveTimeScale(Dw / D * kc); a.run.setEffectiveTimeScale(Dr / D * kc);
  if (move > 0.25 && dt > 0) { const p0 = Math.floor(player.stepPh || 0); player.stepPh = (player.stepPh || 0) + dt * kc / D * 2; if (Math.floor(player.stepPh) !== p0) sfx('step', clamp(v / BRISK, 0.4, 1)); }
  player.char.mixer.update(dt);
  const fall = game.phase === 'failed' ? smooth(0, 0.6, simT - game.failT) : 0;
  player.char.root.position.set(player.x, player.y + fall * 0.12, player.z); player.char.root.rotation.set(-1.45 * fall, player.h, 0, 'YXZ');
  player.char.root.updateMatrixWorld(true); const bf = player.char.root.userData.brief, hp = new THREE.Vector3(); bf.hand.getWorldPosition(hp);
  bf.bc.position.set(hp.x, hp.y - 0.27, hp.z); bf.bc.rotation.y = player.h;
  bf.cup.visible = simT - game.cupT < 9; if (bf.cup.visible) { bf.rhand.getWorldPosition(hp); bf.cup.position.set(hp.x, hp.y - 0.02, hp.z); }
}

// ---------- follow camera with collision pull-in ----------
const cam = { yaw: Math.PI / 2, pos: new THREE.Vector3(), frac: 1, dist: 3.4, up: 0.6, pitch: 0.12 };
function rayBoxes(ox, oy, oz, dx, dy, dz) { // returns nearest hit fraction along the segment, or 1
  let best = 1; const m = 0.3;
  for (const b of boxes) {
    let t0 = 0, t1 = best; const mn = [b.x0 - m, -1, b.z0 - m], mx = [b.x1 + m, b.h, b.z1 + m], o = [ox, oy, oz], d = [dx, dy, dz]; let ok = true;
    for (let k = 0; k < 3 && ok; k++) {
      if (Math.abs(d[k]) < 1e-8) { if (o[k] < mn[k] || o[k] > mx[k]) ok = false; continue; }
      let a = (mn[k] - o[k]) / d[k], c = (mx[k] - o[k]) / d[k]; if (a > c) [a, c] = [c, a];
      t0 = Math.max(t0, a); t1 = Math.min(t1, c); if (t0 > t1) ok = false;
    }
    if (ok && t0 < best) best = t0;
  }
  return best;
}
function updateCamera(dt, snap) {
  if (game.phase === 'done' && !snap) { const u = (simT - game.doneT) / 3.0; if (u < 1) cam.yaw += 0.95 * dt * 1.5708 * Math.sin(Math.PI * clamp(u)); }
  if (player.speed > 0.2 || snap) cam.yaw += wrapA(player.h - cam.yaw) * (snap ? 1 : 1 - Math.exp(-2.5 * dt));
  const px = player.x, py = player.y + 1.5, pz = player.z;
  const fk = game.phase === 'failed' ? smooth(0, 0.9, simT - game.failT) : 0;
  const want = new THREE.Vector3(px - Math.sin(cam.yaw) * cam.dist, py + cam.up + 2.0 * fk, pz - Math.cos(cam.yaw) * cam.dist);
  if (snap) cam.pos.copy(want); else cam.pos.lerp(want, 1 - Math.exp(-10 * dt));
  const dx = cam.pos.x - px, dy = cam.pos.y - py, dz = cam.pos.z - pz, hit = rayBoxes(px, py, pz, dx, dy, dz);
  const len = Math.hypot(dx, dy, dz) || 1, f = Math.max(1.25 / len, hit);
  cam.frac = snap ? f : lerp(cam.frac, f, 1 - Math.exp(-(f < cam.frac ? 16 : 3) * dt));
  camera.position.set(px + dx * cam.frac, py + dy * cam.frac, pz + dz * cam.frac);
  camera.rotation.set(-cam.pitch - 0.7 * fk, cam.yaw + Math.PI, 0, 'YXZ');
  const sh = clamp(1 - (simT - game.hitT) / 0.45); if (sh > 0) { camera.position.x += Math.sin(simT * 70) * 0.13 * sh; camera.position.y += Math.cos(simT * 55) * 0.1 * sh; }
  sun.target.position.set(Math.round(px), 0, Math.round(pz)); sun.position.copy(sun.target.position).addScaledVector(SUN_DIR, 120);
}

// ---------- pedestrians (data driven: add an entry to PED_DEFS to add a walker) ----------
const JACKETS = [0x7a2f3a, 0x2f4f7a, 0x6b4a2e, 0x2f6b4a, 0x8a8a8a, 0x3a3a44, 0x9a6a3a, 0x4a3a6b];
const SKINS = [0xd9a57c, 0xb07a52, 0x8a5a3c, 0xe6bd98], HAIRS = [0x1c1612, 0x3a2a1c, 0x5a4630, 0x111111];
const peds = [], extras = [];
const blockLoop = (i, j, inset) => { const [cx, cz] = blockC(i, j), a = BLOCK / 2 - inset; return [[cx - a, cz - a], [cx + a, cz - a], [cx + a, cz + a], [cx - a, cz + a]]; };
const stripX = (z1, z2) => [[-10.5, z1], [10.5, z1], [10.5, z2], [-10.5, z2]]; // thin loop along a pavement edge
const stripZ = (x1, x2) => [[x1, -10.5], [x1, 10.5], [x2, 10.5], [x2, -10.5]];
const PED_DEFS = [
  { loop: blockLoop(2, 2, 1.15) }, { loop: blockLoop(2, 2, 1.15) }, { loop: blockLoop(2, 2, 1.15) }, { loop: blockLoop(2, 2, 1.15), rushOnly: true },
  { loop: stripX(22.9, 23.7) }, { loop: stripX(22.9, 23.7) }, { loop: stripX(22.9, 23.7), rushOnly: true },
  { loop: stripX(-22.9, -23.7) }, { loop: stripX(-22.9, -23.7), rushOnly: true },
  { loop: stripZ(-22.9, -23.7) }, { loop: stripZ(-22.9, -23.7), rushOnly: true },
  { loop: blockLoop(3, 3, 1.3) }, { loop: blockLoop(3, 3, 1.65), rushOnly: true }, { loop: blockLoop(1, 3, 1.3) }, { loop: blockLoop(3, 1, 1.3) },
];
function spawnPedestrian(def) {
  const ch = makeCharacter(def.look, { scale: def.scale });
  ch.act.idle.setEffectiveWeight(0); ch.act.walk.setEffectiveWeight(1); ch.act.walk.setEffectiveTimeScale(def.speed / 1.45); ch.act.walk.time = def.s0 % 1;
  const pts = def.loop.slice(); pts.push(pts[0]); if (def.dir < 0) pts.reverse();
  peds.push({ ch, path: new Path(pts, true), s: def.s0, speed: def.speed, h: null, rushOnly: !!def.rushOnly, gone: false });
}
function buildPeds() {
  if (QS.has('nopeds')) return;
  const r = rng32(2026);
  PED_DEFS.forEach((d, k) => spawnPedestrian({ ...d, s0: r() * 90, dir: r() < 0.5 ? 1 : -1, speed: 1.2 + r() * 0.5, scale: 0.94 + r() * 0.1,
    look: { jacket: JACKETS[k % JACKETS.length], trousers: [0x23262d, 0x3b3530, 0x2c3440][k % 3], skin: SKINS[k % SKINS.length], hair: HAIRS[(k * 3) % HAIRS.length] } }));
  // kiosk vendor, standing idle in front of the kiosk
  const v = makeCharacter({ jacket: 0xf1eee6, trousers: 0x165a47, skin: 0xb07a52, hair: 0x1c1612 }, {});
  v.root.position.set(25.4, 0.15, 24.9); v.root.rotation.y = Math.atan2(KIOSK.x - 25.4, KIOSK.z - 24.9); extras.push(v);
}
function updatePeds(dt) {
  for (const p of peds) {
    p.s += p.speed * dt; const q = p.path.at(p.s);
    p.h = p.h === null ? q.h : p.h + clamp(wrapA(q.h - p.h), -5 * dt, 5 * dt);
    p.ch.root.position.set(q.x, 0.15, q.z); p.ch.root.rotation.y = p.h; p.ch.mixer.update(dt);
  }
  for (const e of extras) e.mixer.update(dt);
}

// ---------- traffic: cars keep left, queue behind each other and give way to the player ----------
const cars = [];
function colBox(w, h, d, x, y, z, c) { const g = new THREE.BoxGeometry(w, h, d), col = new Float32Array(72), k = new THREE.Color(c); for (let i = 0; i < 24; i++) { col[i * 3] = k.r; col[i * 3 + 1] = k.g; col[i * 3 + 2] = k.b; } g.setAttribute('color', new THREE.BufferAttribute(col, 3)); g.translate(x, y, z); return g; }
const carMat = new THREE.MeshLambertMaterial({ vertexColors: true });
function carMesh(c) {
  const gs = [colBox(1.8, 0.62, 4.2, 0, 0.62, 0, c), colBox(1.62, 0.55, 2.2, 0, 1.2, -0.25, 0x1d2730), colBox(1.5, 0.06, 2.0, 0, 1.5, -0.25, c)];
  for (const sx of [-1, 1]) { for (const sz of [-1, 1]) gs.push(colBox(0.24, 0.64, 0.64, sx * 0.86, 0.32, sz * 1.35, 0x121216)); gs.push(colBox(0.42, 0.16, 0.06, sx * 0.6, 0.72, 2.1, 0xfff4c8), colBox(0.42, 0.14, 0.06, sx * 0.6, 0.72, -2.1, 0xc32a1e)); }
  const m = new THREE.Mesh(mergeGeometries(gs), carMat); m.castShadow = true; scene.add(m); return m;
}
const LANES = { inner: [[14.5, 14.5], [14.5, -14.5], [-14.5, -14.5], [-14.5, 14.5]], outer: [[19.5, 19.5], [-19.5, 19.5], [-19.5, -19.5], [19.5, -19.5]], ring: [[53.5, 53.5], [-53.5, 53.5], [-53.5, -53.5], [53.5, -53.5]] };
const lanePaths = {};
const CAR_DEFS = [
  { lane: 'inner', s: 40, speed: 6.5, c: 0xb5372e }, { lane: 'inner', s: 95, speed: 6.5, c: 0xe8e4da, rushOnly: true },
  { lane: 'outer', s: 10, speed: 6.5, c: 0x2f5f9e }, { lane: 'outer', s: 70, speed: 6.5, c: 0xe0b23a, rushOnly: true }, { lane: 'outer', s: 120, speed: 6.5, c: 0x4b5560, rushOnly: true },
  { lane: 'ring', s: 20, speed: 9, c: 0x2e7d5b }, { lane: 'ring', s: 160, speed: 9, c: 0x8b3fa0, rushOnly: true }, { lane: 'ring', s: 300, speed: 9, c: 0xd9d9d9 },
];
function buildCars() {
  for (const k in LANES) { const pts = fillet(LANES[k], 3.2); pts.push(pts[0]); lanePaths[k] = new Path(pts, true); }
  for (const d of CAR_DEFS) cars.push({ mesh: carMesh(d.c), lane: d.lane, path: lanePaths[d.lane], s0: d.s, s: d.s, v: d.speed, speed: d.speed, h: null, rushOnly: !!d.rushOnly, gone: false, stopT: -9, ignoreT: -9 });
}
function updateCars(dt) {
  for (const c of cars) {
    const q = c.path.at(c.s), fx = Math.sin(q.h), fz = Math.cos(q.h), rx = player.x - q.x, rz = player.z - q.z, f = rx * fx + rz * fz, lat = Math.abs(rx * fz - rz * fx);
    let target = c.speed; if (simT > c.ignoreT && f > 0 && f < 7 && lat < 2.0) target = 0;        // brake for the player, if there is room
    if (simT < c.stopT || (game.phase === 'failed' && Math.hypot(rx, rz) < 14)) target = 0;
    if (!c.gone && (game.phase === 'laps' || game.phase === 'office') && Math.abs(f) < 2.4 && lat < 1.2) {
      const side = (rx * fz - rz * fx) >= 0 ? 1 : -1;
      if (c.v > 2 && simT > game.invulnT) carHit(c, fx, fz, side);
      else if (c.v <= 2) { player.x += fz * side * (1.2 - lat); player.z += -fx * side * (1.2 - lat); }
    }
    for (const o of cars) if (o !== c && o.lane === c.lane && !o.gone) { const gap = ((o.s - c.s) % c.path.len + c.path.len) % c.path.len; if (gap < 8) target = Math.min(target, gap < 6 ? 0 : o.v); }
    c.v += clamp(target - c.v, -6 * dt, 5 * dt); c.s += c.v * dt;
    const p = c.path.at(c.s); c.h = c.h === null ? p.h : c.h + clamp(wrapA(p.h - c.h), -2.5 * dt, 2.5 * dt);
    c.mesh.position.set(p.x, 0, p.z); c.mesh.rotation.y = c.h;
  }
}

// ---------- coins, sparkles, lap gate ----------
const COIN_PTS = [[-4, R], [0, R], [4, R], [R, -6], [R, -9], [R, -12], [-2, -R], [-5, -R], [-8, -R], [-R, -4], [-R, 0], [-R, 4], [26, 0], [29, 0], [26, R], [31, R], [36, R]];
const LAP_COINS = 14; let coinMesh; const coinAlive = COIN_PTS.map(() => true), coinO = new THREE.Object3D();
function buildCoins() {
  const g = new THREE.CylinderGeometry(0.34, 0.34, 0.07, 20); g.rotateX(Math.PI / 2);
  coinMesh = new THREE.InstancedMesh(g, new THREE.MeshLambertMaterial({ color: 0xf2c230, emissive: 0x7a5200 }), COIN_PTS.length); coinMesh.castShadow = true; coinMesh.frustumCulled = false; scene.add(coinMesh);
}
function drawCoins(t) {
  COIN_PTS.forEach((p, i) => { coinO.position.set(p[0], coinAlive[i] ? 1.0 + Math.sin(t * 3 + i) * 0.08 : -50, p[1]); coinO.rotation.y = t * 3 + i; coinO.updateMatrix(); coinMesh.setMatrixAt(i, coinO.matrix); });
  coinMesh.instanceMatrix.needsUpdate = true;
}
const SPN = 80, spk = { pos: new Float32Array(SPN * 3).fill(-99), vel: new Float32Array(SPN * 3), life: new Float32Array(SPN), i: 0, pts: null, r: rng32(11) };
function buildSparkles() {
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(spk.pos, 3));
  const cv = document.createElement('canvas'); cv.width = cv.height = 32; const c2 = cv.getContext('2d'); const gr = c2.createRadialGradient(16, 16, 0, 16, 16, 16); gr.addColorStop(0, 'rgba(255,255,255,1)'); gr.addColorStop(0.5, 'rgba(255,255,255,0.9)'); gr.addColorStop(1, 'rgba(255,255,255,0)'); c2.fillStyle = gr; c2.fillRect(0, 0, 32, 32);
  spk.pts = new THREE.Points(g, new THREE.PointsMaterial({ color: 0xffe27a, size: 0.085, map: new THREE.CanvasTexture(cv), transparent: true, opacity: 0.95, depthWrite: false, fog: false })); spk.pts.frustumCulled = false; scene.add(spk.pts);
}
function sparkle(n, x, y, z, sp = 2.6) {
  for (let k = 0; k < n; k++) { const i = (spk.i++ % SPN) * 3, a = spk.r() * 6.283, s = sp * (0.4 + spk.r() * 0.6);
    spk.pos[i] = x; spk.pos[i + 1] = y; spk.pos[i + 2] = z; spk.vel[i] = Math.cos(a) * s; spk.vel[i + 1] = 1.5 + spk.r() * 3; spk.vel[i + 2] = Math.sin(a) * s; spk.life[i / 3] = 0.6 + spk.r() * 0.5; }
}
function updateSparkles(dt) {
  for (let k = 0; k < SPN; k++) { if (spk.life[k] <= 0) continue; const i = k * 3; spk.life[k] -= dt; spk.vel[i + 1] -= 7 * dt;
    spk.pos[i] += spk.vel[i] * dt; spk.pos[i + 1] += spk.vel[i + 1] * dt; spk.pos[i + 2] += spk.vel[i + 2] * dt; if (spk.life[k] <= 0) spk.pos[i + 1] = -99; }
  spk.pts.geometry.attributes.position.needsUpdate = true;
}
function buildLapGate() {
  const green = lam(0x165a47), gold = lam(0xd9ae4f), x = START[0];
  for (const z of [11.7, 22.3]) { const p = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.1, 5.2, 8), green); p.position.set(x, 2.6, z); p.castShadow = true; scene.add(p); }
  addBox(0.14, 0.9, 10.8, x, 4.75, R, green, true); addBox(0.18, 0.1, 10.8, x, 5.25, R, gold); addBox(0.18, 0.1, 10.8, x, 4.25, R, gold);
  const s = new THREE.Mesh(new THREE.PlaneGeometry(3.2, 0.8), new THREE.MeshBasicMaterial({ map: signTexture('LAP LINE', '#e9c568', '#165a47') })); s.position.set(x - 0.08, 4.75, R); s.rotation.y = -Math.PI / 2; scene.add(s);
  addBox(0.5, 0.02, 10, x, 0.012, R, new THREE.MeshLambertMaterial({ color: 0xd9ae4f }), false, true);
}
function buildLandmarks() {
  const stone = lam(0xefeadc);
  // bank: portico on the north road
  for (const dx of [-3.3, -1.1, 1.1, 3.3]) { const c = new THREE.Mesh(new THREE.CylinderGeometry(0.28, 0.28, 4.2, 12), stone); c.position.set(-4.75 + dx, 2.25, -24.5); c.castShadow = true; scene.add(c); }
  addBox(8.2, 0.5, 1.4, -4.75, 4.6, -24.45, stone, true); addBox(4.0, 1.0, 0.16, -4.75, 5.35, -24.0, lam(0x2b3a55), true);
  const bs = new THREE.Mesh(new THREE.PlaneGeometry(3.6, 0.9), new THREE.MeshBasicMaterial({ map: signTexture('BANK', '#f1e6c4', '#2b3a55') })); bs.position.set(-4.75, 5.35, -23.91); scene.add(bs);
  // cafe: awning and tables on the west road
  addBox(1.0, 0.12, 6.6, -24.6, 3.0, -4.75, lam(0xb5443a), true);
  const cs = new THREE.Mesh(new THREE.PlaneGeometry(3.2, 0.8), new THREE.MeshBasicMaterial({ map: signTexture('CAFÉ', '#ffffff', '#b5443a') })); cs.position.set(-25.04, 3.9, -4.75); cs.rotation.y = Math.PI / 2; scene.add(cs);
  for (const z of [-6.6, -2.9]) { const top = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.45, 0.05, 16), stone); top.position.set(-24.55, 0.92, z); top.castShadow = true; scene.add(top); addBox(0.08, 0.75, 0.08, -24.55, 0.52, z, lam(0x2a2b30)); }
}

// ---------- guidance: chevrons + destination markers ----------
const chevrons = [];
function buildGuidance() {
  const g = new THREE.BufferGeometry(); const hw = 1.25, th = 0.34, T = [0, 0.02, 0.5], Ti = [0, 0.02, 0.5 - th], Lo = [-hw, 0.02, -0.45], Li = [-hw, 0.02, -0.45 - th], Ro = [hw, 0.02, -0.45], Ri = [hw, 0.02, -0.45 - th];
  g.setAttribute('position', new THREE.Float32BufferAttribute([...T, ...Lo, ...Li, ...T, ...Li, ...Ti, ...T, ...Ti, ...Ri, ...T, ...Ri, ...Ro], 3));
  for (let i = 0; i < 9; i++) { const m = new THREE.Mesh(g, new THREE.MeshBasicMaterial({ color: 0x86e6ae, transparent: true, side: THREE.DoubleSide, depthWrite: false, fog: false })); m.renderOrder = 2; scene.add(m); chevrons.push(m); }
}
function makeMarker(x, z) {
  const grp = new THREE.Group(); grp.position.set(x, 0, z);
  const ring = new THREE.Mesh(new THREE.TorusGeometry(2.5, 0.11, 8, 48), new THREE.MeshBasicMaterial({ color: 0xfff0a8, fog: false })); ring.rotation.x = Math.PI / 2; ring.position.y = 0.12;
  const beam = new THREE.Mesh(new THREE.CylinderGeometry(1.1, 1.1, 34, 24, 1, true), new THREE.MeshBasicMaterial({ color: 0xffe98a, transparent: true, opacity: 0.2, side: THREE.DoubleSide, depthWrite: false, fog: false })); beam.position.y = 17;
  const gem = new THREE.Mesh(new THREE.OctahedronGeometry(0.5), new THREE.MeshBasicMaterial({ color: 0xffe37a, fog: false })); gem.scale.set(1, 1.5, 1);
  const gem2 = new THREE.Mesh(new THREE.OctahedronGeometry(0.5), new THREE.MeshBasicMaterial({ color: 0xfff6c8, transparent: true, opacity: 0.45, fog: false })); gem2.scale.set(1.3, 1.3, 1.3); gem.add(gem2);
  grp.add(ring, beam, gem); grp.userData = { ring, beam, gem }; scene.add(grp); return grp;
}
let markKiosk, markOffice;
function updateGuidance(t) {
  const office = game.phase === 'office'; let path, sp = 0, ahead = 1e9;
  if (game.missed) { path = new Path([[player.x, player.z], [KIOSK.x, KIOSK.z]]); ahead = path.len; }
  else if (office) { path = officePath; sp = path.closest(player.x, player.z); ahead = path.len - sp; }
  else { path = loopPath; sp = path.closest(player.x, player.z); if (!game.coffee) ahead = ((KIOSK_S - sp) % path.len + path.len) % path.len; }
  const show = game.phase !== 'done', N = chevrons.length, gap = 2.3, ph = (t * 2.6) % gap;
  chevrons.forEach((m, i) => {
    const o = 2.2 + i * gap + ph, u = (i * gap + ph) / (N * gap);
    m.visible = show && o < ahead - 2.4; if (!m.visible) return;
    const q = path.at(sp + o), q2 = path.at(sp + o + 0.6); m.position.set(q.x, 0.03, q.z); m.rotation.y = Math.atan2(q2.x - q.x, q2.z - q.z);
    m.material.opacity = smooth(0, 0.14, u) * (1 - smooth(0.72, 1, u)) * 0.9;
  });
  markKiosk.visible = game.phase === 'laps' && !game.coffee; markOffice.visible = office;
  for (const m of [markKiosk, markOffice]) { const u = m.userData; u.gem.position.y = 3.0 + Math.sin(t * 2.4) * 0.22; u.gem.rotation.y = t * 1.6; u.ring.scale.setScalar(1 + Math.sin(t * 3) * 0.035); u.beam.material.opacity = 0.19 + Math.sin(t * 3) * 0.035; }
}

// ---------- game rules ----------
const game = {}, audio = []; window.audio = audio;
function sfx(type, g = 1) { audio.push([+simT.toFixed(3), type, +g.toFixed(2)]); }
const isRush = () => game.phase === 'laps' && game.lap <= 2;
const fmtT = s => { s = Math.round(s); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`; };
function resetGame() {
  Object.assign(game, { phase: 'laps', lap: 1, coffee: false, coffees: 0, xp: 0, coins: 0, time: 0, lapStart: 0, wrongTurns: 0, wrong: false, wrongT: 0, okT: 0, acc: 0, paused: false,
    coffeeVisible: false, coffeeShownT: 0, pressT: -9, xpFloat: null, lapFlashT: -9, lapDone: null, doneT: -9, missed: false, missedCount: 0, kioskSeen: false, graceT: 0, started: false,
    slowT: 0, briskT: 0, nudge: false, cupT: -99, coinT: -9, stars: 3, health: 70, hits: 0, invulnT: -9, stunT: -9, hitT: -9, failT: -9, hpFloat: null });
  Object.assign(player, { x: START[0], z: START[1], y: 0, h: Math.PI / 2, vx: 0, vz: 0, speed: 0 }); input.str = 0;
  game.prevA = Math.atan2(player.z, player.x); coinAlive.fill(true);
  for (const p of peds) p.gone = false; for (const c of cars) { c.gone = false; c.stopT = -9; c.ignoreT = -9; }
}
function addXp(n, label) { game.xp += n; game.xpFloat = { n, label, t: simT }; }
function tapCoffee() {
  if (!game.coffeeVisible) return; game.coffee = true; game.coffees++; game.missed = false; game.graceT = 1.6; game.coffeeVisible = false; game.pressT = simT; game.cupT = simT;
  const h0 = game.health; game.health = Math.min(100, game.health + 30); game.hpFloat = { text: `+${Math.round(game.health - h0)}`, good: true, t: simT };
  addXp(50, 'Coffee'); sparkle(18, player.x, player.y + 1.2, player.z); sfx('coffee'); setOv('Extra|Coffee moment: cup, sparkle, XP and energy restored', 2.6);
}
function carHit(c, fx, fz, side) {
  game.health = Math.max(0, game.health - 40); game.hits++; game.invulnT = simT + 1.6; game.stunT = simT + 0.7; game.hitT = simT; game.hpFloat = { text: '-40', good: false, t: simT };
  player.vx = fz * side * 6 + fx * 3; player.vz = -fx * side * 6 + fz * 3; c.v = 0; c.stopT = simT + 1.0; c.ignoreT = simT + 4;
  sfx('hit'); sparkle(10, player.x, player.y + 1.0, player.z, 3); setOv('Extra|Health: a car hit costs 40 energy', 3);
  if (game.health <= 0) { game.phase = 'failed'; game.failT = simT; game.wrong = false; game.nudge = false; game.missed = false; game.coffeeVisible = false; input.str = 0; sfx('fail'); }
}
function completeLap() {
  game.lapDone = { n: game.lap, time: game.time - game.lapStart, t: simT }; game.lapStart = game.time; addXp(20, 'Lap bonus'); game.lapFlashT = simT;
  game.coffee = false; game.kioskSeen = false; for (let i = 0; i < LAP_COINS; i++) coinAlive[i] = true;
  sparkle(30, player.x, player.y + 1.4, player.z, 3.6); sfx('lap');
  if (game.lap >= 3) game.phase = 'office'; else game.lap++;
}
function updateGame(dt) {
  if (game.phase === 'done' || game.phase === 'failed') return;
  game.time += dt; if (player.speed > 1) game.started = true;
  game.graceT = Math.max(0, game.graceT - dt);
  if (player.speed > 0.5) game.health = Math.max(10, game.health - (player.speed > 4 ? 0.45 : 0.3) * dt); // walking uses energy, coffee restores it
  const rush = isRush(); let along; // speed component along the correct direction
  if (game.phase === 'laps') {
    const a = Math.atan2(player.z, player.x); game.acc -= wrapA(a - game.prevA); game.prevA = a;
    const r = Math.hypot(player.x, player.z) || 1; along = (player.vx * player.z - player.vz * player.x) / r;
    const kx = KIOSK.x - player.x, kz = KIOSK.z - player.z, d = Math.hypot(kx, kz) || 1;
    if (game.acc >= Math.PI * 2 * game.lap) { if (!game.coffee) game.acc -= Math.PI * 2; else completeLap(); } // lap does not count without the coffee tap
    const near = d < 11 && !game.coffee && game.phase === 'laps';
    if (near && !game.coffeeVisible) game.coffeeShownT = simT; game.coffeeVisible = near; if (near) game.kioskSeen = true;
    if (!game.coffee && !game.missed && game.kioskSeen && d > 11.5) {
      const past = ((loopPath.closest(player.x, player.z) - KIOSK_S) % loopPath.len + loopPath.len) % loopPath.len;
      if (past > 2 && past < 45) { game.missed = true; game.missedCount++; sfx('missed'); }
    }
    if (game.missed) along = (player.vx * kx + player.vz * kz) / d;
  } else {
    const ox = OFFICE.x - player.x, oz = OFFICE.z - player.z, d = Math.hypot(ox, oz) || 1; along = (player.vx * ox + player.vz * oz) / d; game.coffeeVisible = false;
    if (d < 2.5) { game.phase = 'done'; addXp(40, 'Arrived'); game.doneT = simT; game.wrong = false; game.nudge = false; input.str = 0;
      game.stars = clamp(3 - Math.ceil((game.wrongTurns + game.missedCount + game.hits) / 3), 1, 3); sfx('finish'); sparkle(40, player.x, player.y + 1.5, player.z, 4); return; }
  }
  if (game.missed || game.graceT > 0) { game.wrong = false; game.wrongT = 0; }
  else if (along < -0.6) { game.wrongT += dt; game.okT = 0; if (game.wrongT > 0.4 && !game.wrong) { game.wrong = true; game.wrongTurns++; sfx('wrong'); } }
  else if (along > 0.4) { game.okT += dt; game.wrongT = 0; if (game.okT > 0.35) game.wrong = false; }
  // pace meter: nudge when slow in rush hour, bonus XP for keeping brisk
  if (rush && game.started && !game.wrong && !game.missed) {
    if (player.speed < 4.2) { game.slowT += dt; game.briskT = 0; } else { game.slowT = 0; game.briskT += dt; if (game.briskT >= 6) { game.briskT = 0; addXp(5, 'Brisk bonus'); sfx('bonus'); } }
    const n = game.slowT > 2; if (n && !game.nudge) { sfx('nudge'); setOv('Extra|Pace meter: nudge when too slow in rush hour', 2.6); } game.nudge = n;
  } else { game.nudge = false; game.slowT = 0; }
  // coins
  COIN_PTS.forEach((p, i) => { if (coinAlive[i] && Math.hypot(p[0] - player.x, p[1] - player.z) < 1.3) { coinAlive[i] = false; if (game.coins === 0) setOv('Extra|Coins to collect along the route', 2.4); game.coins++; game.coinT = simT; sparkle(6, p[0], 1.0, p[1], 1.6); sfx('coin'); } });
}

// ---------- HUD ----------
let note = '', noteOv = null, touch = null;
function setOv(text, dur) { noteOv = { text, until: simT + dur }; }
function tapAt(x, y) { touch = { x, y, t: simT }; sfx('click'); }
function updateHud() {
  const t = simT, vis = (el, on) => { el.style.display = on ? 'block' : 'none'; };
  $('lapv').textContent = `${game.lap}/3`;
  const lf = clamp(1 - (t - game.lapFlashT) / 0.8); $('lap').style.transform = `scale(${1 + 0.18 * Math.sin(lf * Math.PI)})`; $('lap').style.borderColor = lf > 0 ? '#f3d27a' : 'rgba(214,178,94,0.45)';
  let b1, b2; const warn = game.wrong || game.missed || t - game.hitT < 2.4 || game.phase === 'failed';
  const hitNow = t - game.hitT < 2.4;
  if (game.phase === 'failed') { b1 = 'Knocked down'; b2 = 'Too many hits from the traffic.'; }
  else if (hitNow) { b1 = 'Hit by a car'; b2 = 'Stay out of the traffic lanes. Coffee restores your energy.'; }
  else if (game.missed) { b1 = 'Coffee missed'; b2 = 'Go back to the kiosk and tap it. The lap will not count without it.'; }
  else if (game.wrong) { b1 = 'Wrong way'; b2 = 'Turn around and follow the green arrows.'; }
  else if (game.phase === 'done') { b1 = 'Arrived'; b2 = 'You reached the office.'; }
  else if (game.phase === 'office') { b1 = 'Get to the office'; b2 = 'Laps done. Follow the arrows to the yellow marker.'; }
  else if (!game.coffee) { b1 = 'Head for the yellow marker'; b2 = 'The coffee kiosk is ahead. Tap it as you pass.'; }
  else if (game.lap < 3) { b1 = 'Coffee grabbed'; b2 = `Keep going round the park to finish lap ${game.lap}.`; }
  else { b1 = 'Last lap'; b2 = 'Finish the lap, then head to the office.'; }
  $('b1').textContent = b1; $('b2').textContent = b2; $('banner').className = 'card' + (warn ? ' warn' : '');
  const rush = isRush(); $('pacev').textContent = rush ? (game.nudge ? 'Pick up the pace' : 'Rush hour: walk briskly') : 'Rush hour is over: normal pace';
  vis($('pace'), game.phase !== 'done' && game.phase !== 'failed'); $('pace').className = 'card' + (game.nudge ? ' slow' : ''); $('pacebar').style.display = rush ? 'inline-block' : 'none';
  $('pacefill').style.width = `${clamp(player.speed / BRISK) * 100}%`; $('pacefill').style.background = player.speed >= 4.2 ? '#7fe0a0' : '#ffb347';
  $('xpv').textContent = `${game.xp} XP`; $('coinv').textContent = `${game.coins}`;
  const ca = clamp(1 - (t - game.coinT) / 0.35); $('coin').style.transform = `scale(${1 + 0.14 * Math.sin(ca * Math.PI)})`;
  const xf = game.xpFloat, xa = xf ? (t - xf.t) / 1.3 : 2; vis($('xpfloat'), xa < 1);
  if (xa < 1) { $('xpfloat').textContent = `${xf.label ? xf.label + ' ' : ''}+${xf.n} XP`; $('xpfloat').style.top = `${212 - xa * 24}px`; $('xpfloat').style.opacity = 1 - smooth(0.65, 1, xa); }
  const hp = Math.round(game.health); $('hpv').textContent = hp; $('hpfill').style.width = `${hp}%`; $('hpfill').style.background = hp > 50 ? '#7fe0a0' : hp > 25 ? '#ffb347' : '#ff5c5c';
  const hs = clamp(1 - (t - game.hitT) / 0.5); $('hp').style.transform = `scale(${1 + 0.1 * Math.sin(hs * Math.PI)})`; $('hp').style.borderColor = hp <= 25 ? '#ff5c5c' : 'rgba(214,178,94,0.45)';
  const hf = game.hpFloat, ha = hf ? (t - hf.t) / 1.3 : 2; vis($('hpfloat'), ha < 1);
  if (ha < 1) { $('hpfloat').textContent = hf.text; $('hpfloat').style.color = hf.good ? '#7fe0a0' : '#ff6b6b'; $('hpfloat').style.top = `${166 - ha * 20}px`; $('hpfloat').style.opacity = 1 - smooth(0.65, 1, ha); }
  const hu = clamp(1 - (t - game.hitT) / 0.7); vis($('hurt'), hu > 0); $('hurt').style.opacity = hu;
  vis($('wrong'), game.wrong); if (game.wrong) $('wrong').style.transform = `scale(${1 + 0.03 * Math.sin(t * 9)})`;
  const ld = game.lapDone, la = ld ? (t - ld.t) / 2.6 : 2; vis($('lapdone'), la < 1);
  if (la < 1) { $('ld1').textContent = `LAP ${ld.n} COMPLETE`; $('ld2').textContent = `Lap time ${fmtT(ld.time)}  ·  +20 XP`; $('lapdone').style.opacity = smooth(0, 0.1, la) * (1 - smooth(0.85, 1, la)); $('lapdone').style.transform = `scale(${lerp(0.7, 1, smooth(0, 0.12, la))})`; }
  // joystick knob shows the input relative to the camera
  const fx = Math.sin(cam.yaw), fz = Math.cos(cam.yaw), jx = (input.dx * -fz + input.dz * fx) * input.str, jy = (input.dx * fx + input.dz * fz) * input.str;
  $('knob').style.left = `${126 - 52 + jx * 74}px`; $('knob').style.top = `${126 - 52 - jy * 74}px`;
  // contextual coffee button
  const pressA = (t - game.pressT) / 0.3, cshow = game.coffeeVisible || pressA < 1; vis($('coffee'), cshow);
  if (cshow) { const inA = clamp((t - game.coffeeShownT) / 0.25); const s = game.coffeeVisible ? lerp(0.6, 1, smooth(0, 1, inA)) * (1 + 0.03 * Math.sin(t * 6)) : lerp(0.9, 1.1, pressA); $('coffee').style.transform = `scale(${s})`; $('coffee').style.opacity = game.coffeeVisible ? inA : 1 - pressA; }
  // route complete (after the finish camera move)
  const dA = clamp((t - game.doneT - 3.0) / 0.4), done = game.phase === 'done' && dA > 0; vis($('done'), done);
  const fA = clamp((t - game.failT - 1.3) / 0.4), failed = game.phase === 'failed' && fA > 0; vis($('fail'), failed); vis($('dim'), done || failed || game.paused);
  $('dim').style.opacity = game.paused ? 1 : Math.max(dA, failed ? fA : 0);
  if (failed) { $('fail').style.opacity = fA; $('fail').style.transform = `scale(${lerp(0.92, 1, smooth(0, 1, fA))})`;
    $('flap').textContent = `${game.lap - 1}/3`; $('fcoin').textContent = `${game.coins}`; $('ftime').textContent = fmtT(game.time); $('fsub').textContent = `Hit by traffic ${game.hits} times. Use the pavements and grab coffee to recover.`; }
  if (done) { $('done').style.opacity = dA; $('done').style.transform = `scale(${lerp(0.92, 1, smooth(0, 1, dA))})`;
    const bits = [`3 laps`, `${game.coffees} coffees`, `${game.wrongTurns} wrong turn${game.wrongTurns === 1 ? '' : 's'}`]; if (game.missedCount) bits.push(`${game.missedCount} missed kiosk`); if (game.hits) bits.push(`${game.hits} car hit${game.hits === 1 ? '' : 's'}`);
    $('dsub').innerHTML = bits.join(' &middot; ');
    $('stars').innerHTML = [0, 1, 2].map(i => `<span style="color:${i < game.stars ? '#edc76a' : 'rgba(255,255,255,0.18)'}">&#9733;</span>`).join('');
    $('sxp').textContent = `+${game.xp}`; $('scoin').textContent = `+${game.coins}`; $('stime').textContent = fmtT(game.time); }
  vis($('paused'), game.paused);
  const label = (noteOv && t < noteOv.until) ? noteOv.text : note, parts = label.split('|'); vis($('note'), !!label && SHOW_NOTES);
  $('noteb').textContent = parts.length > 1 ? parts[0] : ''; $('notet').textContent = parts[parts.length - 1];
  const ta = touch ? (t - touch.t) / 0.4 : 2; vis($('touch'), ta < 1);
  if (ta < 1) { $('touch').style.left = `${touch.x - 45}px`; $('touch').style.top = `${touch.y - 45}px`; $('touch').style.transform = `scale(${lerp(0.7, 1.25, ta)})`; $('touch').style.opacity = 1 - ta * ta; }
  $('fade').style.opacity = Math.max(1 - simT / 0.5, endT !== null ? (simT - endT) / 0.6 : 0); vis($('fade'), simT < 0.5 || endT !== null);
}

// ---------- scripted playthrough (stands in for the joystick) ----------
const L = 1, Wk = 0.6;
const PROG = [
  { t: 'label', text: 'Brief 1 + 8|Suited character idle, restyled HUD' }, { t: 'wait', s: 3.0 },
  { t: 'label', text: 'Brief 1|Character: idle, walk and brisk walk blend with speed' }, { t: 'go', pts: [[-5, R]], str: Wk }, { t: 'go', pts: [[3, R]], str: L },
  { t: 'label', text: 'Brief 5|Guidance: green chevrons and a yellow marker with light beam' }, { t: 'go', pts: [[R, R], [R, 14]], str: L },
  { t: 'label', text: 'Brief 2|Camera: smooth pull-in, no clipping through buildings' }, { t: 'go', pts: [[R, 5], [20.5, 0], [30.5, 0]], str: Wk }, { t: 'go', pts: [[21, 0], [R, -4.5], [R, -8]], str: Wk },
  { t: 'label', text: 'Brief 7|Pedestrians: 15 on pavement loops, fewer once rush hour ends' }, { t: 'go', pts: [[R, -R], [4, -R]], str: L },
  { t: 'label', text: 'Extra|Landmarks: bank, café and office shopfronts' }, { t: 'go', pts: [[-R, -R], [-R, -6]], str: L },
  { t: 'label', text: 'Extra|Rush-hour traffic keeps left and gives way to the player' }, { t: 'go', pts: [[-R, R], [-4, R]], str: L },
  { t: 'label', text: 'Brief 4|Pace hints: rush hour pill on laps 1 and 2, bonus XP for staying brisk' }, { t: 'go', pts: [[R, R], [R, -R], [2, -R]], str: L },
  { t: 'go', pts: [[9, -R]], str: Wk }, { t: 'go', pts: [[-R, -R], [-R, -2]], str: L },
  { t: 'pause', s: 2.6 },
  { t: 'label', text: 'Extra|Health: energy drops when walking, coffee restores it, cars hurt' }, { t: 'traffic', ax: 'x', v: -19.5, d: 1, until: 'hit', max: 4 },
  { t: 'label', text: 'Brief 8|HUD: dark green and gold restyle' }, { t: 'go', pts: [[-R, 12], [-R, R], [-4, R]], str: L },
  { t: 'label', text: 'Brief 4|Pace hints: rush hour over, normal pace on lap 3' }, { t: 'set', skip: true }, { t: 'go', pts: [[R, R], [R, 3]], str: Wk },
  { t: 'set', skip: false }, { t: 'go', pts: [[R, 10.5]], str: Wk },
  { t: 'label', text: 'Brief 5|Guidance: chevrons follow the route round the park' }, { t: 'go', pts: [[R, -R], [-R, -R]], str: Wk },
  { t: 'label', text: 'Brief 1|Character: normal walk cycle' }, { t: 'go', pts: [[-R, R], [-7, R]], str: Wk },
  { t: 'label', text: 'Route|After lap 3, walk to the office to finish' }, { t: 'go', pts: [[OFFICE.x + 1, R]], str: Wk },
  { t: 'label', text: 'Extra|Finish shot: the camera swings round at the office' }, { t: 'wait', s: 3.1 },
  { t: 'label', text: 'Brief 9|Route complete: XP, coins, time, star rating, Replay and Continue' }, { t: 'wait', s: 6.5 },
  { t: 'replay', xy: [823, 738] }, { t: 'label', text: 'Brief 9|Replay restarts the route at Lap 1/3' }, { t: 'wait', s: 2.0 },
  { t: 'label', text: 'Extra|Second run: walking into the traffic lanes' }, { t: 'go', pts: [[-4, R]], str: L }, { t: 'traffic', chase: true, until: 'fail', max: 30 },
  { t: 'label', text: 'Extra|Mission failed after repeated car hits, with Retry' }, { t: 'wait', s: 6.5 },
  { t: 'replay', xy: [1099, 693] }, { t: 'label', text: 'Extra|Retry restarts the route with energy reset' }, { t: 'wait', s: 2.2 }, { t: 'end' }, { t: 'wait', s: 0.7 },
];
let pc = 0, st = null, apCoffeeT = 0, apSkip = false, endT = null, simT = 0, frame = 0, finished = false;
function autopilot(dt) {
  if (game.coffeeVisible && !game.paused && !apSkip) { apCoffeeT += dt; if (apCoffeeT > 1.6) { tapAt(1759, 825); tapCoffee(); apCoffeeT = 0; } } else apCoffeeT = 0;
  for (let guard = 0; guard < 20; guard++) {
    const s = PROG[pc]; if (!s) { finished = true; return; }
    if (s.t === 'label') { note = s.text; pc++; continue; }
    if (s.t === 'set') { apSkip = s.skip; pc++; continue; }
    if (s.t === 'end') { endT = simT; pc++; continue; }
    if (s.t === 'wait') { st ??= { t: 0 }; input.str = 0; st.t += dt; if (st.t >= s.s) { pc++; st = null; continue; } return; }
    if (s.t === 'go') {
      if (game.phase === 'done' || game.phase === 'failed') { pc++; st = null; continue; }
      st ??= { path: new Path([[player.x, player.z], ...s.pts]), s: 0 };
      st.s = st.path.closest(player.x, player.z, st.s - 0.5, st.s + 5);
      if (st.s >= st.path.len - 0.5) { pc++; st = null; continue; }
      const q = st.path.at(st.s + 2.4), dx = q.x - player.x, dz = q.z - player.z, d = Math.hypot(dx, dz) || 1;
      input.dx = dx / d; input.dz = dz / d; input.str = s.str; return;
    }
    if (s.t === 'pause') {
      st ??= { t: 0, ph: 0 }; st.t += dt;
      if (st.ph === 0) { tapAt(1835, 85); game.paused = true; st.ph = 1; }
      if (st.ph === 1 && st.t > s.s) { tapAt(960, 583); st.ph = 2; }
      if (st.ph === 2 && st.t > s.s + 0.3) { game.paused = false; pc++; st = null; continue; }
      return;
    }
    if (s.t === 'traffic') {
      st ??= { h: game.hits, t: 0 }; st.t += dt;
      if ((s.until === 'fail' ? game.phase === 'failed' : game.hits > st.h) || st.t > s.max) { pc++; st = null; continue; }
      let tx, tz;
      if (s.chase) { let best = 1e9; for (const c of cars) { if (c.gone || c.lane === 'ring' || simT < c.ignoreT) continue; const d = Math.hypot(c.mesh.position.x - player.x, c.mesh.position.z - player.z); if (d < best) { best = d; tx = c.mesh.position.x; tz = c.mesh.position.z; } } if (best > 1e8) { tx = player.x; tz = player.z + 1; } }
      else if (s.ax === 'x') { tx = s.v; tz = player.z + s.d * 4; } else { tx = player.x + s.d * 4; tz = s.v; }
      const dx = tx - player.x, dz = tz - player.z, d = Math.hypot(dx, dz) || 1; input.dx = dx / d; input.dz = dz / d; input.str = L; return;
    }
    if (s.t === 'replay') { st ??= { t: 0 }; if (st.t === 0) tapAt(s.xy[0], s.xy[1]); st.t += dt; if (st.t > 0.35) { resetGame(); for (const c of cars) { c.s = c.s0; c.h = null; } updateCamera(0, true); pc++; st = null; continue; } return; }
  }
}
let prevLap = 1;
function eventLabels() {
  if (game.coffeeVisible) setOv('Brief 6|Contextual action: Tap to grab coffee near the kiosk', 0.3);
  if (game.missed && !game.coffeeVisible) setOv('Brief 6|Missed the kiosk: the lap does not count until coffee is tapped', 0.3);
  if (game.lapDone && simT - game.lapDone.t < DT * 1.5) setOv(`Brief 3|Lap system: lap ${game.lapDone.n} of 3 counted, with a lap celebration`, 3);
  if (game.wrong) setOv('Brief 3|Lap system: wrong-way warning', 1.2);
  if (game.paused) setOv('Brief 8|HUD: pause button', 0.6);
  if (game.phase === 'failed' && simT - game.failT < 1.3) setOv('Extra|Health: energy gone, the player is knocked down', 0.3);
}

// ---------- main step ----------
const SHOW_NOTES = !QS.has('nonotes');
function step(render = true) {
  autopilot(DT);
  if (!game.paused) { updateGame(DT); updatePlayer(DT); updatePeds(DT); updateCars(DT); updateSparkles(DT); updateCamera(DT, false); }
  simT += DT; // UI clock keeps running while the world is frozen
  eventLabels(); frame++; logEvents();
  if (render) draw();
}
function cullScene() {
  const fx = Math.sin(cam.yaw), fz = Math.cos(cam.yaw), rush = isRush();
  const seen = r => { const dx = r.position.x - camera.position.x, dz = r.position.z - camera.position.z, d = Math.hypot(dx, dz); return [d < 62 && (dx * fx + dz * fz > -1.5 || d < 5), d]; };
  for (const p of peds) {
    const r = p.ch.root, [inView, d] = seen(r); if (p.rushOnly && !rush && !inView) p.gone = true;
    r.visible = inView && !p.gone; const cast = d < 26; if (r.userData.cast !== cast) { r.userData.cast = cast; r.traverse(o => { if (o.isMesh) o.castShadow = cast; }); }
  }
  for (const e of extras) e.root.visible = seen(e.root)[0];
  for (const c of cars) { const [inView] = seen(c.mesh); if (c.rushOnly && !rush && !inView) c.gone = true; c.mesh.visible = !c.gone; }
}
window.events = []; let evPrev = '';
function logEvents() { const k = `lap${game.lap} ${game.phase} btn:${game.coffeeVisible} coffee:${game.coffee} wrong:${game.wrong} missed:${game.missed} nudge:${game.nudge} paused:${game.paused} hits:${game.hits} | ${note.split('|')[0]}`; if (k !== evPrev) { evPrev = k; window.events.push(`${frame} ${k} xp${game.xp} c${game.coins} hp${Math.round(game.health)}`); } }
function draw() { cullScene(); updateGuidance(simT); drawCoins(simT); updateHud(); renderer.render(scene, camera); }
window.advance = (n, render = true) => { for (let i = 0; i < n; i++) step(render && i === n - 1); return { frame, finished }; };
window.countFrames = () => { let n = 0; while (!finished && n < 20000) { step(false); n++; } return n; };

await MeshoptSimplifier.ready;
new GLTFLoader().load('./assets/Xbot.glb', g => {
  gltf = g;
  buildCity(); buildKioskAndOffice(); buildLandmarks(); buildLapGate(); buildGuidance(); buildCoins(); buildSparkles(); buildCars();
  markKiosk = makeMarker(KIOSK.x, KIOSK.z); markOffice = makeMarker(OFFICE.x, OFFICE.z);
  player.char = makeCharacter({ jacket: 0x2a3550, trousers: 0x222a3e, skin: 0xc9966b, hair: 0x15110d }, { briefcase: true });
  buildPeds(); resetGame(); updatePlayer(0); updatePeds(0); updateCars(0); updateCamera(0, true); draw();
  renderer.compile(scene, camera);
  window.ready = true;
});
