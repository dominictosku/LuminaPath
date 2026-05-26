import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
} from '@ionic/angular/standalone';
import { NodeUnlockEvent, SkillTreeComponent } from '../components/skill-tree.component';
import type { SkillTreeBranch } from '../models/skill-tree.model';
import {
  parseNodeId,
  questSkillsToBranches,
  unlockedNodeIdsFor,
} from '../util/skill-adapter';
import {
  QuestBoardService,
  QuestBoardState,
  QuestSkill,
} from 'src/app/features/quests/services/quest-board.service';

@Component({
  selector: 'app-skill-tree-page',
  templateUrl: './skill-tree.page.html',
  styleUrls: ['./skill-tree.page.scss'],
  imports: [
    RouterLink,
    IonButton,
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    SkillTreeComponent,
  ],
})
export class SkillTreePage implements OnInit {
  private questBoardService = inject(QuestBoardService);

  board: QuestBoardState | null = null;
  skills: QuestSkill[] = [];
  xp = 0;
  isLoading = true;
  isSaving = false;
  errorMessage = '';
  toastMessage = '';

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  get skillTreeBranches(): SkillTreeBranch[] {
    return questSkillsToBranches(this.skills);
  }

  get skillTreeUnlockedNodeIds(): string[] {
    return unlockedNodeIdsFor(this.skills);
  }

  get nodeCount(): number {
    return this.skills.reduce((sum, skill) => sum + skill.nodes.length, 0);
  }

  get unlockedNodeCount(): number {
    return this.skills.reduce((sum, skill) => sum + skill.unlockedNodes.length, 0);
  }

  async load(event?: CustomEvent): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.applyBoard(await this.questBoardService.getBoard());
    } catch {
      this.errorMessage = 'Skill tree could not be loaded.';
    } finally {
      this.isLoading = false;
      this.completeRefresh(event);
    }
  }

  async handleSkillTreeNodeUnlocked(event: NodeUnlockEvent): Promise<void> {
    if (this.isSaving) return;

    const parsed = parseNodeId(event.nodeId);
    if (!parsed) return;

    const skill = this.skills.find((item) => item.id === parsed.skillId);
    if (!skill) return;

    if (skill.unlockedNodes.includes(parsed.nodeIndex)) {
      return;
    }

    if (this.skillNodeState(skill, parsed.nodeIndex) === 'locked') {
      this.showToast('Unlock the previous node first');
      return;
    }

    skill.unlockedNodes = [...skill.unlockedNodes, parsed.nodeIndex];
    skill.xp += event.xp;
    this.xp += event.xp;
    this.showToast(`${skill.nodes[parsed.nodeIndex]} unlocked`);
    await this.persistSkills();
  }

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  private async persistSkills(): Promise<void> {
    this.isSaving = true;
    try {
      const fallback: QuestBoardState = {
        xp: this.xp,
        currentStreakDays: 0,
        longestStreakDays: 0,
        quests: [],
        skills: this.skills,
        achievements: [],
      };
      const next = {
        ...(this.board ?? fallback),
        xp: this.xp,
        skills: this.skills,
      };
      this.applyBoard(await this.questBoardService.saveSkills(next));
    } catch {
      this.errorMessage = 'Progress could not be saved.';
    } finally {
      this.isSaving = false;
    }
  }

  private applyBoard(board: QuestBoardState): void {
    this.board = board;
    this.xp = board.xp;
    this.skills = board.skills.map((skill) => ({
      ...skill,
      nodes: [...skill.nodes],
      unlockedNodes: [...skill.unlockedNodes],
    }));
  }

  private skillNodeState(skill: QuestSkill, nodeIndex: number): 'completed' | 'available' | 'locked' {
    if (skill.unlockedNodes.includes(nodeIndex)) return 'completed';
    const next = skill.nodes.findIndex((_, index) => !skill.unlockedNodes.includes(index));
    return nodeIndex === next ? 'available' : 'locked';
  }

  private completeRefresh(event?: CustomEvent): void {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }

  private showToast(message: string): void {
    this.toastMessage = message;
    window.setTimeout(() => {
      if (this.toastMessage === message) {
        this.toastMessage = '';
      }
    }, 2600);
  }
}

