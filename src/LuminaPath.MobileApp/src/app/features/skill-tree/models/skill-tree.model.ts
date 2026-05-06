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

export type SkillTreeQuest = {
  id: string;
  branchId: string;
  text: string;
  xp: number;
  done: boolean;
};

export type SkillTreeState = {
  unlockedNodeIds: string[];
  quests: SkillTreeQuest[];
};

export type SkillTreeNodeStatus = 'locked' | 'available' | 'unlocked';

export type SkillTreePickedNode = {
  branchId: string;
  nodeId: string;
};
