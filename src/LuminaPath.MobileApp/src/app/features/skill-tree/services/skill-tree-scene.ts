import {
  AdditiveBlending,
  BufferAttribute,
  BufferGeometry,
  CanvasTexture,
  CatmullRomCurve3,
  Color,
  DoubleSide,
  FogExp2,
  Group,
  IcosahedronGeometry,
  Line,
  Mesh,
  MeshBasicMaterial,
  NormalBlending,
  PerspectiveCamera,
  PlaneGeometry,
  Points,
  PointsMaterial,
  Raycaster,
  RingGeometry,
  Scene,
  ShaderMaterial,
  Sprite,
  SpriteMaterial,
  Vector2,
  Vector3,
  WebGLRenderer,
} from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { ShaderPass } from 'three/addons/postprocessing/ShaderPass.js';
import { OutputPass } from 'three/addons/postprocessing/OutputPass.js';
import { SkillTreeBranch, SkillTreeNode, SkillTreeNodeStatus, SkillTreePickedNode } from '../models/skill-tree.model';
import { BeaconData, Constellation, NodeMeshes, SceneListeners } from './skill-tree-scene.types';
import {
  BEACON_FRAGMENT,
  BEACON_VERTEX,
  NEBULA_FRAGMENT,
  NEBULA_VERTEX,
  OUTLINE_FRAGMENT,
  OUTLINE_VERTEX,
  STARFIELD_FRAGMENT,
  STARFIELD_VERTEX,
  VEIL_FRAGMENT,
  VEIL_VERTEX,
} from './skill-tree-scene.shaders';

const SKILL_SPACING = 22;
const NODE_RADIUS = 0.28;
const CAPSTONE_RADIUS = 0.42;

function easeInOutCubic(x: number): number {
  return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
}

export class SkillTreeScene {
  private renderer: any;
  private scene: any;
  private camera: any;
  private raycaster: any;
  private mouse: any;
  private starField: any;
  private nebula: any;
  private constellations: Constellation[] = [];
  private currentSkillIdx = 0;
  private targetSkillIdx = 0;
  private camTransitionT = 1;
  private camFromX = 0;
  private camToX = 0;
  private cameraTargetY = 5;
  private cameraTargetZ = 26;
  private cameraDriftX = 0;
  private cameraDriftY = 0;
  private hoveredNode: SkillTreePickedNode | null = null;
  private listeners: SceneListeners = { hover: [], click: [], whoosh: [], hoverSound: [] };
  private unlockedIds = new Set<string>();
  private animations: { update(): boolean }[] = [];
  private glowTexture: any = null;
  private canvas!: HTMLCanvasElement;
  private rafId: number | null = null;
  private lastTime = 0;
  private composer: EffectComposer | null = null;
  private bloomPass: UnrealBloomPass | null = null;
  private caPass: ShaderPass | null = null;
  private lineTimeUniform = { value: 0 };
  private resizeHandler = () => this.onResize();
  private pointerMoveHandler = (e: PointerEvent) => this.onPointerMove(e);
  private clickHandler = () => this.onClick();
  private destroyed = false;

  init(canvas: HTMLCanvasElement, branches: SkillTreeBranch[], initialUnlocked: string[]): void {
    this.canvas = canvas;
    this.unlockedIds = new Set(initialUnlocked);

    this.renderer = new WebGLRenderer({ canvas, antialias: true, alpha: false });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setSize(canvas.clientWidth || window.innerWidth, canvas.clientHeight || window.innerHeight);
    this.renderer.setClearColor(0x06101f, 1);

    this.scene = new Scene();
    this.scene.fog = new FogExp2(0x06101f, 0.018);

    const aspect = (canvas.clientWidth || window.innerWidth) / (canvas.clientHeight || window.innerHeight);
    this.camera = new PerspectiveCamera(55, aspect, 0.1, 800);
    this.camera.position.set(0, 5, 26);
    this.camera.lookAt(0, 5, 0);

    this.raycaster = new Raycaster();
    this.raycaster.params.Points = { threshold: 0.4 };
    this.mouse = new Vector2(-10, -10);

    this.buildStarfield();
    this.buildNebula(branches);
    this.buildConstellations(branches);
    this.buildComposer();

    window.addEventListener('resize', this.resizeHandler);
    canvas.addEventListener('pointermove', this.pointerMoveHandler);
    canvas.addEventListener('click', this.clickHandler);

    this.lastTime = performance.now();
    this.rafId = requestAnimationFrame(now => this.animate(now));
  }

  private buildComposer(): void {
    const w = this.canvas.clientWidth || window.innerWidth;
    const h = this.canvas.clientHeight || window.innerHeight;

    this.composer = new EffectComposer(this.renderer);
    this.composer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.composer.setSize(w, h);

    this.composer.addPass(new RenderPass(this.scene, this.camera));

    this.bloomPass = new UnrealBloomPass(new Vector2(w, h), 0.75, 0.55, 0.82);
    this.composer.addPass(this.bloomPass);

    this.caPass = new ShaderPass({
      uniforms: {
        tDiffuse: { value: null },
        uAmount: { value: 0 },
      },
      vertexShader: `
        varying vec2 vUv;
        void main() {
          vUv = uv;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform sampler2D tDiffuse;
        uniform float uAmount;
        varying vec2 vUv;
        void main() {
          vec2 dir = (vUv - 0.5);
          vec2 offset = dir * uAmount;
          float r = texture2D(tDiffuse, vUv - offset).r;
          float g = texture2D(tDiffuse, vUv).g;
          float b = texture2D(tDiffuse, vUv + offset).b;
          float a = texture2D(tDiffuse, vUv).a;
          gl_FragColor = vec4(r, g, b, a);
        }
      `,
    });
    this.composer.addPass(this.caPass);

    this.composer.addPass(new OutputPass());
  }

  destroy(): void {
    this.destroyed = true;
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    window.removeEventListener('resize', this.resizeHandler);
    if (this.canvas) {
      this.canvas.removeEventListener('pointermove', this.pointerMoveHandler);
      this.canvas.removeEventListener('click', this.clickHandler);
    }
    this.listeners = { hover: [], click: [], whoosh: [], hoverSound: [] };
    if (this.composer) {
      this.composer.dispose?.();
      this.composer = null;
    }
    if (this.renderer) {
      this.renderer.dispose?.();
    }
    this.constellations = [];
  }

  on(evt: 'hover' | 'click' | 'whoosh' | 'hoverSound', fn: any): void {
    this.listeners[evt].push(fn);
  }

  goToSkill(idx: number, branchCount: number): void {
    idx = Math.max(0, Math.min(branchCount - 1, idx));
    if (idx === this.targetSkillIdx) return;
    this.targetSkillIdx = idx;
    this.camFromX = this.camera.position.x;
    this.camToX = idx * SKILL_SPACING;
    this.camTransitionT = 0;
    this.listeners.whoosh.forEach(fn => fn());
  }

  unlockNode(branchId: string, nodeId: string): void {
    const skillIdx = this.constellations.findIndex(c => c.branch.id === branchId);
    if (skillIdx < 0) return;
    const c = this.constellations[skillIdx];
    const m = c.nodeMeshes.get(nodeId);
    if (!m) return;

    this.unlockedIds.add(nodeId);
    this.updateOutline(skillIdx);

    const t0 = performance.now();
    const dur = 1600;
    const baseGlowSize = m.glow.userData.baseSize;
    const startCol = m.core.material.color.clone();
    const endCol = m.core.userData.baseColor.clone();

    const ringGeo = new RingGeometry(0.3, 0.34, 64);
    const ringMat = new MeshBasicMaterial({
      color: m.core.userData.baseColor.clone(),
      transparent: true,
      opacity: 0.9,
      side: DoubleSide,
      blending: AdditiveBlending,
      depthWrite: false,
    });
    const shock = new Mesh(ringGeo, ringMat);
    shock.position.copy(m.core.position);
    shock.lookAt(this.camera.position);
    c.group.add(shock);

    const partCount = 24;
    const partGeo = new BufferGeometry();
    const partPos = new Float32Array(partCount * 3);
    const partVel: [number, number, number][] = [];
    for (let i = 0; i < partCount; i++) {
      partPos[i * 3] = m.core.position.x;
      partPos[i * 3 + 1] = m.core.position.y;
      partPos[i * 3 + 2] = m.core.position.z;
      const phi = Math.random() * Math.PI * 2;
      const theta = Math.acos(Math.random() * 2 - 1);
      const speed = 0.9 + Math.random() * 0.6;
      partVel.push([
        Math.sin(theta) * Math.cos(phi) * speed,
        Math.sin(theta) * Math.sin(phi) * speed,
        Math.cos(theta) * speed,
      ]);
    }
    partGeo.setAttribute('position', new BufferAttribute(partPos, 3));
    const partMat = new PointsMaterial({
      color: m.core.userData.baseColor.clone(),
      size: 4,
      sizeAttenuation: false,
      transparent: true,
      opacity: 1,
      blending: AdditiveBlending,
      depthWrite: false,
      map: this.getGlowTexture(),
    });
    const points = new Points(partGeo, partMat);
    c.group.add(points);

    this.animations.push({
      update: () => {
        const t = (performance.now() - t0) / dur;
        if (t >= 1) {
          c.group.remove(shock); shock.geometry.dispose(); shock.material.dispose();
          c.group.remove(points); points.geometry.dispose(); points.material.dispose();
          this.refreshSkillVisuals(skillIdx);
          this.drawLinesFrom(skillIdx, nodeId);
          return true;
        }
        const eIn = 1 - Math.pow(1 - t, 3);
        shock.scale.setScalar(1 + eIn * 14);
        shock.material.opacity = 0.9 * (1 - t);
        const pos = points.geometry.attributes['position'].array;
        for (let i = 0; i < partCount; i++) {
          pos[i * 3] = m.core.position.x + partVel[i][0] * eIn * 3;
          pos[i * 3 + 1] = m.core.position.y + partVel[i][1] * eIn * 3;
          pos[i * 3 + 2] = m.core.position.z + partVel[i][2] * eIn * 3;
        }
        points.geometry.attributes['position'].needsUpdate = true;
        points.material.opacity = 1 - t;
        const pulse = 1 + Math.sin(t * Math.PI) * 0.6;
        m.core.scale.setScalar(pulse);
        m.glow.scale.setScalar(baseGlowSize * (1 + Math.sin(t * Math.PI) * 0.6));
        m.core.material.color.copy(startCol).lerp(endCol, eIn);
        m.glow.material.opacity = 0.4 + Math.sin(t * Math.PI) * 0.6;
        return false;
      },
    });
  }

  setUnlocked(ids: string[]): void {
    this.unlockedIds = new Set(ids);
    this.constellations.forEach((_, idx) => {
      this.updateOutline(idx);
      this.refreshSkillVisuals(idx);
    });
  }

  getCurrentSkillIdx(): number {
    return this.targetSkillIdx;
  }

  private buildStarfield(): void {
    const N = 4000;
    const geo = new BufferGeometry();
    const pos = new Float32Array(N * 3);
    const col = new Float32Array(N * 3);
    const sizes = new Float32Array(N);
    for (let i = 0; i < N; i++) {
      pos[i * 3] = (Math.random() - 0.5) * 600;
      pos[i * 3 + 1] = (Math.random() - 0.3) * 200;
      pos[i * 3 + 2] = (Math.random() - 0.5) * 200 - 30;
      const tint = Math.random();
      if (tint < 0.85) {
        col[i * 3] = 0.85 + Math.random() * 0.15;
        col[i * 3 + 1] = 0.88 + Math.random() * 0.12;
        col[i * 3 + 2] = 1.0;
      } else if (tint < 0.95) {
        col[i * 3] = 1.0; col[i * 3 + 1] = 0.85; col[i * 3 + 2] = 0.65;
      } else {
        col[i * 3] = 0.7; col[i * 3 + 1] = 0.78; col[i * 3 + 2] = 1.0;
      }
      sizes[i] = Math.random() * 1.6 + 0.2;
    }
    geo.setAttribute('position', new BufferAttribute(pos, 3));
    geo.setAttribute('color', new BufferAttribute(col, 3));
    geo.setAttribute('aSize', new BufferAttribute(sizes, 1));

    const mat = new ShaderMaterial({
      uniforms: { uTime: { value: 0 }, uPixelRatio: { value: this.renderer.getPixelRatio() } },
      vertexShader: STARFIELD_VERTEX,
      fragmentShader: STARFIELD_FRAGMENT,
      transparent: true,
      depthWrite: false,
      blending: AdditiveBlending,
    });
    this.starField = new Points(geo, mat);
    this.scene.add(this.starField);
  }

  private buildNebula(branches: SkillTreeBranch[]): void {
    const group = new Group();
    branches.forEach((branch, i) => {
      const color = new Color().setHSL(branch.hue / 360, 0.55, 0.32);
      const planeGeo = new PlaneGeometry(80, 60);
      const mat = new ShaderMaterial({
        transparent: true,
        depthWrite: false,
        blending: AdditiveBlending,
        uniforms: { uColor: { value: color }, uTime: { value: 0 } },
        vertexShader: NEBULA_VERTEX,
        fragmentShader: NEBULA_FRAGMENT,
      });
      const mesh = new Mesh(planeGeo, mat);
      mesh.position.set(i * SKILL_SPACING, 5, -34);
      mesh.userData['shaderMat'] = mat;
      group.add(mesh);
    });
    this.nebula = group;
    this.scene.add(this.nebula);
  }

  private buildConstellations(branches: SkillTreeBranch[]): void {
    branches.forEach((branch, i) => {
      const group = new Group();
      group.position.x = i * SKILL_SPACING;
      const baseColor = new Color().setHSL(branch.hue / 360, 0.7, 0.62);
      const dimColor = new Color().setHSL(branch.hue / 360, 0.3, 0.28);

      const nodeMeshes = new Map<string, NodeMeshes>();
      const lineMeshes: any[] = [];

      branch.nodes.forEach(node => {
        const isCapstone = !!node.capstone;
        const r = isCapstone ? CAPSTONE_RADIUS : NODE_RADIUS;

        const coreGeo = new IcosahedronGeometry(r, 1);
        const coreMat = new MeshBasicMaterial({
          color: baseColor.clone(),
          transparent: true,
          opacity: 1,
        });
        const core = new Mesh(coreGeo, coreMat);
        core.position.set(node.position[0], node.position[1], node.position[2]);
        core.userData = { branchId: branch.id, nodeId: node.id, baseColor: baseColor.clone(), dimColor: dimColor.clone() };
        core.renderOrder = 2;
        group.add(core);

        const glowMat = new SpriteMaterial({
          map: this.getGlowTexture(),
          color: baseColor.clone(),
          transparent: true,
          blending: AdditiveBlending,
          depthWrite: false,
          opacity: 1,
        });
        const glow = new Sprite(glowMat);
        const glowSize = isCapstone ? 4.5 : 2.4;
        glow.scale.set(glowSize, glowSize, glowSize);
        glow.position.copy(core.position);
        glow.userData = { branchId: branch.id, nodeId: node.id, isGlow: true, baseSize: glowSize };
        glow.renderOrder = 1;
        group.add(glow);

        const ringGeo = new RingGeometry(r * 1.4, r * 1.6, 32);
        const ringMat = new MeshBasicMaterial({
          color: baseColor.clone(),
          transparent: true,
          opacity: 0.4,
          side: DoubleSide,
          blending: AdditiveBlending,
          depthWrite: false,
        });
        const ring = new Mesh(ringGeo, ringMat);
        ring.position.copy(core.position);
        ring.userData['isRing'] = true;
        ring.lookAt(this.camera.position);
        group.add(ring);

        const veil = this.buildVeil(core.position, dimColor, isCapstone);
        group.add(veil);

        const { beacon, beaconData } = this.buildBeacon(core.position, baseColor);
        group.add(beacon);

        nodeMeshes.set(node.id, { core, glow, ring, veil, beacon, beaconData, node });
      });

      branch.nodes.forEach(node => {
        node.prereqIds.forEach(prereqId => {
          const from = branch.nodes.find(n => n.id === prereqId);
          if (!from) return;
          const points = [
            new Vector3(from.position[0], from.position[1], from.position[2]),
            new Vector3(node.position[0], node.position[1], node.position[2]),
          ];
          const geo = new BufferGeometry().setFromPoints(points);
          geo.setAttribute('aProgress', new BufferAttribute(new Float32Array([0, 1]), 1));

          const mat = new ShaderMaterial({
            transparent: true,
            depthWrite: false,
            blending: AdditiveBlending,
            uniforms: {
              uTime: this.lineTimeUniform,
              uColor: { value: dimColor.clone() },
              uPulseColor: { value: baseColor.clone() },
              uOpacity: { value: 0.25 },
              uActive: { value: 0 },
              uPulseSpeed: { value: 0.45 },
              uPulseWidth: { value: 28.0 },
            },
            vertexShader: `
              attribute float aProgress;
              varying float vProgress;
              void main() {
                vProgress = aProgress;
                gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
              }
            `,
            fragmentShader: `
              uniform float uTime;
              uniform vec3 uColor;
              uniform vec3 uPulseColor;
              uniform float uOpacity;
              uniform float uActive;
              uniform float uPulseSpeed;
              uniform float uPulseWidth;
              varying float vProgress;
              void main() {
                float pulsePos = fract(uTime * uPulseSpeed);
                float d = abs(vProgress - pulsePos);
                float pulse = exp(-d * uPulseWidth) * uActive;
                vec3 color = mix(uColor, uPulseColor, pulse);
                float alpha = uOpacity + pulse * 0.9;
                gl_FragColor = vec4(color * (1.0 + pulse * 1.8), alpha);
              }
            `,
          });
          const line = new Line(geo, mat);
          line.userData = {
            from: prereqId,
            to: node.id,
            baseColor: baseColor.clone(),
            dimColor: dimColor.clone(),
            originalPoints: points,
          };
          group.add(line);
          lineMeshes.push(line);
        });
      });

      const outline = this.buildOutline(baseColor);
      group.add(outline);

      this.scene.add(group);
      this.constellations.push({ branch, group, nodeMeshes, lineMeshes, outline });
      this.updateOutline(i);
      this.refreshSkillVisuals(i);
    });
  }

  private buildVeil(at: any, dimColor: any, isCapstone: boolean): any {
    const size = isCapstone ? 3.0 : 2.0;
    const geo = new PlaneGeometry(size, size);
    const mat = new ShaderMaterial({
      transparent: true,
      depthWrite: false,
      blending: NormalBlending,
      uniforms: {
        uTime: this.lineTimeUniform,
        uColor: { value: dimColor.clone().multiplyScalar(0.55) },
        uOpacity: { value: 0.6 },
      },
      vertexShader: VEIL_VERTEX,
      fragmentShader: VEIL_FRAGMENT,
    });
    const veil = new Mesh(geo, mat);
    veil.position.copy(at);
    veil.position.z -= 0.35;
    veil.renderOrder = 0;
    return veil;
  }

  private buildBeacon(at: any, baseColor: any): { beacon: any; beaconData: BeaconData } {
    const N = 5;
    const positions = new Float32Array(N * 3);
    const alphas = new Float32Array(N);
    const sizes = new Float32Array(N);
    const phases = new Float32Array(N);
    const radii = new Float32Array(N);
    const heightOffsets = new Float32Array(N);
    const speeds = new Float32Array(N);
    const angles = new Float32Array(N);
    for (let i = 0; i < N; i++) {
      phases[i] = Math.random();
      radii[i] = 0.65 + Math.random() * 0.55;
      heightOffsets[i] = (Math.random() - 0.5) * 0.5;
      speeds[i] = 0.32 + Math.random() * 0.22;
      angles[i] = Math.random() * Math.PI * 2;
    }
    const geo = new BufferGeometry();
    geo.setAttribute('position', new BufferAttribute(positions, 3));
    geo.setAttribute('aAlpha', new BufferAttribute(alphas, 1));
    geo.setAttribute('aSize', new BufferAttribute(sizes, 1));
    const mat = new ShaderMaterial({
      transparent: true,
      depthWrite: false,
      blending: AdditiveBlending,
      uniforms: {
        uColor: { value: baseColor.clone() },
        uTexture: { value: this.getGlowTexture() },
        uPixelRatio: { value: this.renderer.getPixelRatio() },
      },
      vertexShader: BEACON_VERTEX,
      fragmentShader: BEACON_FRAGMENT,
    });
    const beacon = new Points(geo, mat);
    beacon.position.copy(at);
    beacon.visible = false;
    beacon.frustumCulled = false;
    beacon.renderOrder = 2;
    return { beacon, beaconData: { phases, radii, heightOffsets, speeds, angles } };
  }

  private buildOutline(baseColor: any): any {
    const samples = 64;
    const positions = new Float32Array(samples * 3);
    const progress = new Float32Array(samples);
    for (let i = 0; i < samples; i++) progress[i] = i / (samples - 1);
    const geo = new BufferGeometry();
    geo.setAttribute('position', new BufferAttribute(positions, 3));
    geo.setAttribute('aProgress', new BufferAttribute(progress, 1));
    geo.setDrawRange(0, 0);

    const mat = new ShaderMaterial({
      transparent: true,
      depthWrite: false,
      blending: AdditiveBlending,
      uniforms: {
        uTime: this.lineTimeUniform,
        uColor: { value: baseColor.clone().lerp(new Color(0xffffff), 0.4) },
        uOpacity: { value: 0.55 },
      },
      vertexShader: OUTLINE_VERTEX,
      fragmentShader: OUTLINE_FRAGMENT,
    });
    const line = new Line(geo, mat);
    line.frustumCulled = false;
    line.renderOrder = 1;
    return line;
  }

  private updateOutline(skillIdx: number): void {
    const c = this.constellations[skillIdx];
    if (!c || !c.outline) return;
    const unlocked = c.branch.nodes.filter(n => this.unlockedIds.has(n.id));
    const geo = c.outline.geometry;
    if (unlocked.length < 2) {
      geo.setDrawRange(0, 0);
      return;
    }
    const points = unlocked.map(n => new Vector3(n.position[0], n.position[1], n.position[2]));
    const curve = new CatmullRomCurve3(points, false, 'catmullrom', 0.35);
    const sampleCount = 64;
    const samples = curve.getSpacedPoints(sampleCount - 1);
    const arr = geo.attributes.position.array as Float32Array;
    for (let i = 0; i < sampleCount; i++) {
      arr[i * 3] = samples[i].x;
      arr[i * 3 + 1] = samples[i].y;
      arr[i * 3 + 2] = samples[i].z;
    }
    geo.attributes.position.needsUpdate = true;
    geo.setDrawRange(0, sampleCount);
  }

  private updateBeacon(m: NodeMeshes, dt: number): void {
    if (!m.beacon || !m.beaconData) return;
    const { phases, radii, heightOffsets, speeds, angles } = m.beaconData;
    const posAttr = m.beacon.geometry.attributes.position;
    const alphaAttr = m.beacon.geometry.attributes.aAlpha;
    const sizeAttr = m.beacon.geometry.attributes.aSize;
    const pos = posAttr.array as Float32Array;
    const alpha = alphaAttr.array as Float32Array;
    const sizes = sizeAttr.array as Float32Array;
    for (let i = 0; i < phases.length; i++) {
      phases[i] = (phases[i] + dt * speeds[i]) % 1.0;
      const p = phases[i];
      const inward = 1 - p;
      const r = radii[i] * inward * 0.55 + 0.22;
      const angle = angles[i] + p * Math.PI * 3.2;
      pos[i * 3] = Math.cos(angle) * r;
      pos[i * 3 + 1] = heightOffsets[i] * inward;
      pos[i * 3 + 2] = Math.sin(angle) * r;
      alpha[i] = Math.sin(p * Math.PI) * 0.95;
      sizes[i] = 3 + Math.sin(p * Math.PI) * 4;
    }
    posAttr.needsUpdate = true;
    alphaAttr.needsUpdate = true;
    sizeAttr.needsUpdate = true;
  }

  private getGlowTexture(): any {
    if (this.glowTexture) return this.glowTexture;
    const c = document.createElement('canvas');
    c.width = c.height = 128;
    const ctx = c.getContext('2d')!;
    const grad = ctx.createRadialGradient(64, 64, 0, 64, 64, 64);
    grad.addColorStop(0, 'rgba(255,255,255,1)');
    grad.addColorStop(0.2, 'rgba(255,255,255,0.7)');
    grad.addColorStop(0.5, 'rgba(255,255,255,0.18)');
    grad.addColorStop(1, 'rgba(255,255,255,0)');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, 128, 128);
    this.glowTexture = new CanvasTexture(c);
    return this.glowTexture;
  }

  private nodeStatus(node: SkillTreeNode): SkillTreeNodeStatus {
    if (this.unlockedIds.has(node.id)) return 'unlocked';
    return node.prereqIds.every(p => this.unlockedIds.has(p)) ? 'available' : 'locked';
  }

  private refreshSkillVisuals(skillIdx: number): void {
    const c = this.constellations[skillIdx];
    if (!c) return;
    const { branch, nodeMeshes, lineMeshes } = c;
    branch.nodes.forEach(node => {
      const m = nodeMeshes.get(node.id);
      if (!m) return;
      const status = this.nodeStatus(node);
      m.status = status;
      if (status === 'unlocked') {
        m.core.material.color.copy(m.core.userData.baseColor);
        m.core.material.opacity = 1;
        m.glow.material.color.copy(m.core.userData.baseColor);
        m.glow.material.opacity = 1;
        m.ring.material.opacity = 0.55;
      } else if (status === 'available') {
        m.core.material.color.copy(m.core.userData.baseColor).multiplyScalar(0.9);
        m.core.material.opacity = 0.95;
        m.glow.material.color.copy(m.core.userData.baseColor);
        m.glow.material.opacity = 0.7;
        m.ring.material.opacity = 0.35;
      } else {
        m.core.material.color.copy(m.core.userData.dimColor);
        m.core.material.opacity = 0.7;
        m.glow.material.color.copy(m.core.userData.dimColor);
        m.glow.material.opacity = 0.35;
        m.ring.material.opacity = 0.12;
      }
    });
    lineMeshes.forEach(line => {
      const fromUnlocked = this.unlockedIds.has(line.userData.from);
      const toUnlocked = this.unlockedIds.has(line.userData.to);
      const u = line.material.uniforms;
      if (fromUnlocked && toUnlocked) {
        u.uColor.value.copy(line.userData.baseColor);
        u.uPulseColor.value.copy(line.userData.baseColor);
        u.uOpacity.value = 0.75;
        u.uActive.value = 0;
      } else if (fromUnlocked) {
        u.uColor.value.copy(line.userData.baseColor).lerp(line.userData.dimColor, 0.55);
        u.uPulseColor.value.copy(line.userData.baseColor).multiplyScalar(1.4);
        u.uOpacity.value = 0.45;
        u.uActive.value = 1;
      } else {
        u.uColor.value.copy(line.userData.dimColor);
        u.uPulseColor.value.copy(line.userData.dimColor);
        u.uOpacity.value = 0.2;
        u.uActive.value = 0;
      }
    });
  }

  private drawLinesFrom(skillIdx: number, fromNodeId: string): void {
    const c = this.constellations[skillIdx];
    const lines = c.lineMeshes.filter(l => l.userData.from === fromNodeId || l.userData.to === fromNodeId);
    lines.forEach(line => {
      const t0 = performance.now();
      const dur = 700;
      const original = line.userData.originalPoints;
      let p0 = original[0], p1 = original[1];
      if (line.userData.to === fromNodeId) { p0 = original[1]; p1 = original[0]; }
      this.animations.push({
        update: () => {
          const t = Math.min(1, (performance.now() - t0) / dur);
          const eased = 1 - Math.pow(1 - t, 3);
          const mid = new Vector3().lerpVectors(p0, p1, eased);
          line.geometry.setFromPoints([p0, mid]);
          line.geometry.attributes.position.needsUpdate = true;
          if (t >= 1) {
            line.geometry.setFromPoints(original);
            return true;
          }
          return false;
        },
      });
    });
  }

  private onPointerMove(e: PointerEvent): void {
    const rect = this.canvas.getBoundingClientRect();
    this.mouse.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    this.cameraDriftX = this.mouse.x * 1.6;
    this.cameraDriftY = this.mouse.y * 1.0;
  }

  private onClick(): void {
    if (!this.hoveredNode) return;
    this.listeners.click.forEach(fn => fn(this.hoveredNode!));
  }

  private pickNode(): SkillTreePickedNode | null {
    this.raycaster.setFromCamera(this.mouse, this.camera);
    const c = this.constellations[this.currentSkillIdx];
    if (!c) return null;
    const meshes: any[] = [];
    c.nodeMeshes.forEach(m => {
      meshes.push(m.core);
      meshes.push(m.glow);
    });
    const hits = this.raycaster.intersectObjects(meshes, false);
    if (hits.length === 0) return null;
    const hit = hits[0].object;
    return { branchId: hit.userData.branchId, nodeId: hit.userData.nodeId };
  }

  private setHovered(picked: SkillTreePickedNode | null): void {
    if (!picked && !this.hoveredNode) return;
    if (picked && this.hoveredNode &&
        picked.nodeId === this.hoveredNode.nodeId &&
        picked.branchId === this.hoveredNode.branchId) return;
    this.hoveredNode = picked;
    this.canvas.style.cursor = picked ? 'pointer' : '';
    this.listeners.hover.forEach(fn => fn(picked));
    if (picked) this.listeners.hoverSound.forEach(fn => fn());
  }

  private animate(now: number): void {
    if (this.destroyed) return;
    this.rafId = requestAnimationFrame(n => this.animate(n));
    const dt = Math.min(0.05, (now - this.lastTime) / 1000) || 0.016;
    this.lastTime = now;
    const t = now * 0.001;

    if (this.starField) this.starField.material.uniforms.uTime.value = t;
    if (this.nebula) this.nebula.children.forEach((p: any) => p.userData['shaderMat'].uniforms.uTime.value = t);
    this.lineTimeUniform.value = t;

    if (this.caPass) {
      const whoosh = this.camTransitionT < 1 ? Math.sin(this.camTransitionT * Math.PI) : 0;
      const idle = 0.0008 * (0.5 + 0.5 * Math.sin(t * 0.4));
      this.caPass.uniforms['uAmount'].value = whoosh * 0.018 + idle;
    }

    if (this.camTransitionT < 1) {
      this.camTransitionT = Math.min(1, this.camTransitionT + dt / 1.6);
      const e = easeInOutCubic(this.camTransitionT);
      const baseX = this.camFromX + (this.camToX - this.camFromX) * e;
      const dollyBack = Math.sin(this.camTransitionT * Math.PI) * 1.8;
      this.camera.position.z = this.cameraTargetZ + dollyBack;
      this.camera.position.x = baseX + this.cameraDriftX * 1.5;
      this.camera.position.y = this.cameraTargetY + this.cameraDriftY;
      if (this.camTransitionT >= 1) this.currentSkillIdx = this.targetSkillIdx;
    } else {
      const targetX = this.currentSkillIdx * SKILL_SPACING + this.cameraDriftX * 1.5;
      const targetY = this.cameraTargetY + this.cameraDriftY;
      const targetZ = this.cameraTargetZ;
      this.camera.position.x += (targetX - this.camera.position.x) * 0.06;
      this.camera.position.y += (targetY - this.camera.position.y) * 0.06;
      this.camera.position.z += (targetZ - this.camera.position.z) * 0.06;
    }
    // Look-at follows the camera's own X so the pan feels continuous instead of
    // pivoting around the destination constellation.
    this.camera.lookAt(this.camera.position.x - this.cameraDriftX * 1.5, 5, 0);

    this.constellations.forEach((c, idx) => {
      const isActive = idx === this.currentSkillIdx;
      c.group.rotation.y = Math.sin(t * 0.07 + idx * 1.3) * 0.04;
      c.nodeMeshes.forEach(m => {
        m.ring.lookAt(this.camera.position);
        const breath = 1 + Math.sin(t * 1.4 + m.core.position.x * 0.5) * 0.06;
        if (m.status !== 'unlocked' || !isActive) {
          m.core.scale.lerp(new Vector3(breath, breath, breath), 0.1);
        } else {
          m.core.scale.lerp(new Vector3(1, 1, 1), 0.1);
        }

        const isUnlocked = this.unlockedIds.has(m.node.id);
        const prereqsMet = m.node.prereqIds.every(p => this.unlockedIds.has(p));
        const isAvailable = !isUnlocked && prereqsMet;

        if (m.veil) {
          const target = isUnlocked || isAvailable ? 0 : 0.6;
          const cur = m.veil.material.uniforms['uOpacity'].value;
          const next = cur + (target - cur) * 0.06;
          m.veil.material.uniforms['uOpacity'].value = next;
          m.veil.visible = next > 0.01;
          if (m.veil.visible) m.veil.lookAt(this.camera.position);
        }

        if (m.beacon) {
          m.beacon.visible = isAvailable;
          if (isAvailable) this.updateBeacon(m, dt);
        }
      });
    });

    if (this.camTransitionT >= 1) {
      this.setHovered(this.pickNode());
    } else {
      this.setHovered(null);
    }

    for (let i = this.animations.length - 1; i >= 0; i--) {
      if (this.animations[i].update()) this.animations.splice(i, 1);
    }

    if (this.composer) {
      this.composer.render();
    } else {
      this.renderer.render(this.scene, this.camera);
    }
  }

  private onResize(): void {
    const w = this.canvas.clientWidth || window.innerWidth;
    const h = this.canvas.clientHeight || window.innerHeight;
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(w, h, false);
    if (this.composer) {
      this.composer.setSize(w, h);
    }
    if (this.bloomPass) {
      this.bloomPass.setSize(w, h);
    }
  }
}
