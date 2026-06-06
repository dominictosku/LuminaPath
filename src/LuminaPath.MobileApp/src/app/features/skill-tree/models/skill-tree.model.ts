export type SkillTreeNode = {
  id: string;
  name: string;
  description: string;
  position: [number, number, number];
  prereqIds: string[];
  xp: number;
  capstone?: boolean;
};

export type SkillTreeBranch = {
  id: string;
  name: string;
  subtitle: string;
  hue: number;
  nodes: SkillTreeNode[];
};

export type SkillTreeNodeStatus = 'locked' | 'available' | 'unlocked';

export type SkillTreePickedNode = {
  branchId: string;
  nodeId: string;
};
