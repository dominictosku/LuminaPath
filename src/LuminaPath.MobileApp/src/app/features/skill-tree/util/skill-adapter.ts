import { QuestSkill } from 'src/app/features/quests/services/quest-board.service';
import { SkillTreeBranch, SkillTreeNode } from '../models/skill-tree.model';

const NODE_XP_BASE = 15;
const NODE_XP_STEP = 5;
const CAPSTONE_XP = 60;

/**
 * Converts the user's DB-backed quest skills into the visual SkillTreeBranch format.
 * Generates 3D positions in a vertical zigzag pattern and linear prereq chains
 * (each node depends on the previous one in the skill).
 */
export function questSkillsToBranches(skills: QuestSkill[]): SkillTreeBranch[] {
  return skills
    .filter((skill) => skill.nodes.length > 0)
    .map((skill) => questSkillToBranch(skill));
}

export function questSkillToBranch(skill: QuestSkill): SkillTreeBranch {
  const positions = generatePositions(skill.nodes.length, skill.id);
  const lastIndex = skill.nodes.length - 1;

  const nodes: SkillTreeNode[] = skill.nodes.map((nodeName, index) => {
    const isCapstone = index === lastIndex && skill.nodes.length >= 4;
    return {
      id: nodeId(skill.id, index),
      name: nodeName,
      description: '',
      position: positions[index],
      prereqIds: index === 0 ? [] : [nodeId(skill.id, index - 1)],
      xp: isCapstone ? CAPSTONE_XP : NODE_XP_BASE + Math.min(index, 6) * NODE_XP_STEP,
      capstone: isCapstone,
    };
  });

  return {
    id: branchId(skill.id),
    name: skill.name,
    subtitle: skill.name,
    hue: hexToHue(skill.color),
    nodes,
  };
}

export function nodeId(skillId: number, nodeIndex: number): string {
  return `s${skillId}-n${nodeIndex}`;
}

export function branchId(skillId: number): string {
  return `s${skillId}`;
}

export function parseNodeId(id: string): { skillId: number; nodeIndex: number } | null {
  const match = id.match(/^s(\d+)-n(\d+)$/);
  if (!match) return null;
  return { skillId: Number(match[1]), nodeIndex: Number(match[2]) };
}

export function parseBranchId(id: string): number | null {
  const match = id.match(/^s(\d+)$/);
  return match ? Number(match[1]) : null;
}

/** All currently unlocked node IDs across the given skills, in the format the visual scene expects. */
export function unlockedNodeIdsFor(skills: QuestSkill[]): string[] {
  const ids: string[] = [];
  for (const skill of skills) {
    for (const index of skill.unlockedNodes) {
      ids.push(nodeId(skill.id, index));
    }
  }
  return ids;
}

function seededRng(seed: number): () => number {
  let s = (seed * 2654435761) >>> 0 || 1;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function generatePositions(count: number, seed: number): [number, number, number][] {
  const rng = seededRng(seed);
  const style = Math.floor(rng() * 4);
  const verticalStep = 1.5 + rng() * 0.3;
  const amplitude = 2.1 + rng() * 1.1;
  const phase = rng() * Math.PI * 2;
  const twistDir = rng() < 0.5 ? -1 : 1;
  const leanDir = rng() < 0.5 ? -1 : 1;
  const freq = 0.7 + rng() * 0.5;

  const out: [number, number, number][] = [];
  for (let i = 0; i < count; i++) {
    const y = i * verticalStep;
    const jx = (rng() - 0.5) * 0.4;
    const jz = (rng() - 0.5) * 0.3;

    if (i === 0) {
      out.push([jx * 0.3, y, jz * 0.3]);
      continue;
    }

    let x = 0;
    let z = 0;
    switch (style) {
      case 0: {
        // seeded zigzag
        const side = i % 2 === 0 ? 1 : -1;
        x = side * twistDir * (amplitude - Math.min(i * 0.08, 0.9));
        z = ((i % 3) - 1) * 0.35;
        break;
      }
      case 1: {
        // sine ribbon
        x = Math.sin(phase + i * freq) * amplitude;
        z = Math.cos(phase + i * freq * 0.6) * 0.7;
        break;
      }
      case 2: {
        // helix spiral
        const angle = phase + i * (0.85 + freq * 0.1) * twistDir;
        x = Math.cos(angle) * amplitude;
        z = Math.sin(angle) * 0.95;
        break;
      }
      default: {
        // diagonal vine
        const lean = leanDir * i * 0.42;
        x = lean + Math.sin(phase + i * freq) * (amplitude * 0.45);
        z = (((i + 1) % 3) - 1) * 0.55;
        break;
      }
    }

    out.push([x + jx, y, z + jz]);
  }
  return out;
}

function hexToHue(hex: string): number {
  const trimmed = hex.replace('#', '');
  if (trimmed.length !== 6) return 200;

  const r = parseInt(trimmed.slice(0, 2), 16) / 255;
  const g = parseInt(trimmed.slice(2, 4), 16) / 255;
  const b = parseInt(trimmed.slice(4, 6), 16) / 255;

  if ([r, g, b].some(Number.isNaN)) return 200;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const d = max - min;
  if (d === 0) return 0;

  let h: number;
  if (max === r) h = ((g - b) / d) % 6;
  else if (max === g) h = (b - r) / d + 2;
  else h = (r - g) / d + 4;

  h = Math.round(h * 60);
  return h < 0 ? h + 360 : h;
}
