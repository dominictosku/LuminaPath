import { SkillTreeBranch, SkillTreeNode, SkillTreeNodeStatus, SkillTreePickedNode } from '../models/skill-tree.model';

export type BeaconData = {
  phases: Float32Array;
  radii: Float32Array;
  heightOffsets: Float32Array;
  speeds: Float32Array;
  angles: Float32Array;
};

export type NodeMeshes = {
  core: any;
  glow: any;
  ring: any;
  veil?: any;
  beacon?: any;
  beaconData?: BeaconData;
  node: SkillTreeNode;
  status?: SkillTreeNodeStatus;
};

export type Constellation = {
  branch: SkillTreeBranch;
  group: any;
  nodeMeshes: Map<string, NodeMeshes>;
  lineMeshes: any[];
  outline?: any;
};

export type SceneListeners = {
  hover: ((picked: SkillTreePickedNode | null) => void)[];
  click: ((picked: SkillTreePickedNode) => void)[];
  whoosh: (() => void)[];
  hoverSound: (() => void)[];
};
