import { Injectable } from '@angular/core';
import { SkillTreeBranch, SkillTreeQuest, SkillTreeState } from '../models/skill-tree.model';
import { SKILL_TREE_BRANCHES, SKILL_TREE_SEED_QUESTS } from './skill-tree-data';

const STORAGE_KEY = 'luminapath.skill-tree.v1';

@Injectable({ providedIn: 'root' })
export class SkillTreeService {
  readonly branches: SkillTreeBranch[] = SKILL_TREE_BRANCHES;

  load(): SkillTreeState {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as Partial<SkillTreeState>;
        if (Array.isArray(parsed.unlockedNodeIds) && Array.isArray(parsed.quests)) {
          return {
            unlockedNodeIds: parsed.unlockedNodeIds.filter(id => typeof id === 'string'),
            quests: parsed.quests.map(q => ({ ...q })),
          };
        }
      }
    } catch {
      // ignore corrupted storage
    }
    return this.defaultState();
  }

  save(state: SkillTreeState): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch {
      // ignore quota / private mode
    }
  }

  defaultState(): SkillTreeState {
    return {
      unlockedNodeIds: this.branches.map(branch => branch.nodes[0].id),
      quests: SKILL_TREE_SEED_QUESTS.map(q => ({ ...q })),
    };
  }

  generateQuestId(): string {
    return 'q-' + Math.random().toString(36).slice(2, 10);
  }

  totalXp(state: SkillTreeState): number {
    const questXp = state.quests
      .filter(q => q.done)
      .reduce((sum, q) => sum + q.xp, 0);
    const nodeXp = state.unlockedNodeIds.reduce((sum, id) => {
      const node = this.findNode(id);
      return sum + (node ? node.xp : 0);
    }, 0);
    return questXp + nodeXp;
  }

  findNode(nodeId: string) {
    for (const branch of this.branches) {
      const node = branch.nodes.find(n => n.id === nodeId);
      if (node) {
        return node;
      }
    }
    return null;
  }

  findBranch(branchId: string): SkillTreeBranch | undefined {
    return this.branches.find(b => b.id === branchId);
  }
}
