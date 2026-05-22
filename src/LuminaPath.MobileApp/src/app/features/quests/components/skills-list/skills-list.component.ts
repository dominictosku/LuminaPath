import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IonButton, IonIcon, IonProgressBar } from '@ionic/angular/standalone';

import { QuestSkill } from '../../services/quest-board.service';

type SkillNodeState = 'completed' | 'available' | 'locked';

export type SkillNodeAction = { skill: QuestSkill; nodeIndex: number };
export type SkillNodeQuestAction = { skill: QuestSkill; node: string };

/**
 * Skills mode view: grid of skill cards with level, progress, next node, and
 * the per-node unlock affordance. Pure events out — no async work happens here.
 */
@Component({
  selector: 'app-skills-list',
  templateUrl: './skills-list.component.html',
  imports: [IonButton, IonIcon, IonProgressBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkillsListComponent {
  readonly skills = input<QuestSkill[]>([]);

  readonly addSkill = output<void>();
  readonly editSkill = output<QuestSkill>();
  readonly deleteSkill = output<number>();
  readonly addNode = output<QuestSkill>();
  readonly trainSkill = output<QuestSkill>();
  readonly unlockNode = output<SkillNodeAction>();
  readonly startNodeQuest = output<SkillNodeQuestAction>();

  skillLevel(skill: QuestSkill): number {
    return Math.floor(skill.xp / 100) + 1;
  }

  skillProgress(skill: QuestSkill): number {
    return (skill.xp % 100) / 100;
  }

  skillCompletion(skill: QuestSkill): number {
    if (!skill.nodes.length) return 0;
    return skill.unlockedNodes.length / skill.nodes.length;
  }

  unlockedCount(skill: QuestSkill): number {
    return skill.unlockedNodes.length;
  }

  nextNodeIndex(skill: QuestSkill): number {
    return skill.nodes.findIndex((_, index) => !skill.unlockedNodes.includes(index));
  }

  nextNodeName(skill: QuestSkill): string {
    const index = this.nextNodeIndex(skill);
    if (index >= 0) return skill.nodes[index];
    return skill.nodes.length === 0 ? 'Add your first node' : 'Mastery path complete';
  }

  skillNodeState(skill: QuestSkill, nodeIndex: number): SkillNodeState {
    if (skill.unlockedNodes.includes(nodeIndex)) return 'completed';
    return nodeIndex === this.nextNodeIndex(skill) ? 'available' : 'locked';
  }

  /** Active = not-yet-completed quests linked to this skill. Provided by parent
   *  because we don't have access to the quest list here. */
  readonly activeLinkedQuestCount = input<(skill: QuestSkill) => number>(() => 0);
  readonly linkedQuestCount = input<(skill: QuestSkill) => number>(() => 0);

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  trackByText(_: number, item: string): string {
    return item;
  }
}
