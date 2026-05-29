// GLSL shader sources for the skill-tree scene. Extracted verbatim from the
// scene builders; GLSL is whitespace-insensitive so behavior is unchanged.

export const STARFIELD_VERTEX = `
  attribute float aSize;
  attribute vec3 color;
  varying vec3 vColor;
  varying float vTwinkle;
  uniform float uTime;
  uniform float uPixelRatio;
  void main() {
    vColor = color;
    float phase = position.x * 0.13 + position.y * 0.27 + position.z * 0.07;
    vTwinkle = 0.55 + 0.45 * sin(uTime * 1.3 + phase);
    vec4 mv = modelViewMatrix * vec4(position, 1.0);
    gl_PointSize = aSize * (300.0 / -mv.z) * uPixelRatio;
    gl_Position = projectionMatrix * mv;
  }
`;

export const STARFIELD_FRAGMENT = `
  varying vec3 vColor;
  varying float vTwinkle;
  void main() {
    vec2 uv = gl_PointCoord - 0.5;
    float d = length(uv);
    float core = smoothstep(0.5, 0.0, d);
    float halo = smoothstep(0.5, 0.15, d) * 0.4;
    float a = (core + halo) * vTwinkle;
    if (a < 0.01) discard;
    gl_FragColor = vec4(vColor, a);
  }
`;

export const NEBULA_VERTEX = `
  varying vec2 vUv;
  void main() {
    vUv = uv;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }
`;

export const NEBULA_FRAGMENT = `
  varying vec2 vUv;
  uniform vec3 uColor;
  uniform float uTime;
  float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
  float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    float a = hash(i), b = hash(i + vec2(1,0)), c = hash(i + vec2(0,1)), d = hash(i + vec2(1,1));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
  }
  void main() {
    vec2 p = vUv * 3.0;
    float n = noise(p + uTime * 0.04) * 0.6 + noise(p * 2.0 - uTime * 0.02) * 0.4;
    float r = distance(vUv, vec2(0.5));
    float falloff = smoothstep(0.6, 0.05, r);
    float a = n * falloff * 0.55;
    gl_FragColor = vec4(uColor * (0.6 + n * 0.6), a);
  }
`;

export const VEIL_VERTEX = `
  varying vec2 vUv;
  void main() {
    vUv = uv;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }
`;

export const VEIL_FRAGMENT = `
  uniform float uTime;
  uniform vec3 uColor;
  uniform float uOpacity;
  varying vec2 vUv;
  float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
  float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    float a = hash(i), b = hash(i + vec2(1,0)), c = hash(i + vec2(0,1)), d = hash(i + vec2(1,1));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
  }
  void main() {
    vec2 p = vUv * 3.4;
    float n = noise(p + uTime * 0.13) * 0.65 + noise(p * 2.3 - uTime * 0.08) * 0.35;
    float r = distance(vUv, vec2(0.5));
    float falloff = smoothstep(0.5, 0.05, r);
    float a = pow(n, 1.3) * falloff * uOpacity;
    gl_FragColor = vec4(uColor * (0.6 + n * 0.35), a);
  }
`;

export const BEACON_VERTEX = `
  attribute float aAlpha;
  attribute float aSize;
  varying float vAlpha;
  uniform float uPixelRatio;
  void main() {
    vAlpha = aAlpha;
    vec4 mv = modelViewMatrix * vec4(position, 1.0);
    gl_PointSize = aSize * (180.0 / -mv.z) * uPixelRatio;
    gl_Position = projectionMatrix * mv;
  }
`;

export const BEACON_FRAGMENT = `
  uniform vec3 uColor;
  uniform sampler2D uTexture;
  varying float vAlpha;
  void main() {
    vec4 t = texture2D(uTexture, gl_PointCoord);
    gl_FragColor = vec4(uColor, vAlpha * t.a);
  }
`;

export const OUTLINE_VERTEX = `
  attribute float aProgress;
  varying float vProgress;
  void main() {
    vProgress = aProgress;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }
`;

export const OUTLINE_FRAGMENT = `
  uniform float uTime;
  uniform vec3 uColor;
  uniform float uOpacity;
  varying float vProgress;
  void main() {
    float shimmer = 0.55 + 0.45 * sin(uTime * 1.8 + vProgress * 10.0);
    gl_FragColor = vec4(uColor * (0.6 + shimmer * 0.7), uOpacity * shimmer);
  }
`;
