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
  const positions = generatePositions(skill.nodes.length);
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

function generatePositions(count: number): [number, number, number][] {
  const xPattern = [0, -2.0, 2.0, -2.6, 2.6, -3.2, 3.2];
  return Array.from({ length: count }, (_, i) => {
    const y = i * 1.6;
    const x = i === 0 ? 0 : xPattern[i % xPattern.length];
    const z = ((i % 3) - 1) * 0.3;
    return [x, y, z] as [number, number, number];
  });
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
