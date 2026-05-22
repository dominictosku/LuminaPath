import { Component, effect, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon } from '@ionic/angular/standalone';

import { QuestPriority, QuestRecurrence, QuestSkill, QuestType } from '../../services/quest-board.service';

export type QuestQuickAddSubmit = {
  title: string;
  type: QuestType;
  priority: QuestPriority;
  recurrence: QuestRecurrence;
  dueDate: string | null;
  myGameId: number | null;
  skillId: number | null;
};

/** Drop-in field values pushed from the parent (e.g. "Start a quest for this
 *  skill node" pre-fills title/due/skill on the quick-add row). Every property
 *  is optional — only the keys present are applied. Pass a *new* object each
 *  time you want the preset to take effect; the child watches by reference. */
export type QuestQuickAddPreset = {
  title?: string;
  dueDate?: string | null;
  skillId?: number | null;
  myGameId?: number | null;
  advancedOpen?: boolean;
};

type LibraryGame = { myGameId: number; gameName: string };
type TypeOption = { value: QuestType; label: string; icon: string };
type PriorityOption = { value: QuestPriority; label: string; weight: number };
type RecurrenceOption = { value: QuestRecurrence; label: string };
type QuickPreset = 'none' | 'today' | 'tomorrow';

/**
 * Quick-capture row at the top of the quest board: a title input, day-presets,
 * and an "advanced" tray for type/priority/recurrence/game/skill. Form state
 * lives here; the parent receives a single `submit` event with the snapshot.
 */
@Component({
  selector: 'app-quest-quick-add',
  templateUrl: './quest-quick-add.component.html',
  imports: [FormsModule, IonButton, IonIcon],
})
export class QuestQuickAddComponent {
  readonly typeOptions = input<TypeOption[]>([]);
  readonly priorityOptions = input<PriorityOption[]>([]);
  readonly recurrenceOptions = input<RecurrenceOption[]>([]);
  readonly library = input<LibraryGame[]>([]);
  readonly skills = input<QuestSkill[]>([]);
  /** Parent-driven pre-fill. Pass a new reference to apply (see type docs). */
  readonly preset = input<QuestQuickAddPreset | null>(null);

  // Form state two-way bound so the parent can persist preferences across sessions.
  readonly quickAddType = model<QuestType>('sub');
  readonly quickAddPriority = model<QuestPriority>('medium');
  readonly quickAddRecurrence = model<QuestRecurrence>('none');

  readonly submitQuest = output<QuestQuickAddSubmit>();

  quickAddTitle = '';
  quickAddDue: string | null = null;
  quickAddGameId: number | null = null;
  quickAddSkillId: number | null = null;
  quickAddAdvancedOpen = false;

  constructor() {
    // Apply parent presets as they arrive. We only touch the keys the parent
    // actually specified so individual presets can target a subset (e.g. only
    // title + skill from the skill-tree "Quest" button).
    effect(() => {
      const preset = this.preset();
      if (!preset) return;
      if (preset.title !== undefined) this.quickAddTitle = preset.title;
      if (preset.dueDate !== undefined) this.quickAddDue = preset.dueDate;
      if (preset.skillId !== undefined) this.quickAddSkillId = preset.skillId;
      if (preset.myGameId !== undefined) this.quickAddGameId = preset.myGameId;
      if (preset.advancedOpen !== undefined) this.quickAddAdvancedOpen = preset.advancedOpen;
    });
  }

  submit(): void {
    const title = this.quickAddTitle.trim();
    if (!title) return;

    this.submitQuest.emit({
      title,
      type: this.quickAddType(),
      priority: this.quickAddPriority(),
      recurrence: this.quickAddRecurrence(),
      dueDate: this.quickAddDue,
      myGameId: this.quickAddGameId,
      skillId: this.quickAddSkillId,
    });

    this.quickAddTitle = '';
  }

  setDuePreset(preset: QuickPreset): void {
    if (preset === 'none') {
      this.quickAddDue = null;
      return;
    }
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (preset === 'tomorrow') today.setDate(today.getDate() + 1);
    this.quickAddDue = this.toISODate(today);
  }

  isDuePreset(preset: QuickPreset): boolean {
    if (preset === 'none') return this.quickAddDue === null;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (preset === 'tomorrow') today.setDate(today.getDate() + 1);
    return this.quickAddDue === this.toISODate(today);
  }

  toggleAdvanced(): void {
    this.quickAddAdvancedOpen = !this.quickAddAdvancedOpen;
  }

  trackByLibrary(_: number, game: LibraryGame): number {
    return game.myGameId;
  }

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  private toISODate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
