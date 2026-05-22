import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IonIcon, IonProgressBar, IonReorder } from '@ionic/angular/standalone';

import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';
import { Quest, QuestPriority, QuestRecurrence, QuestType } from '../../services/quest-board.service';

export type QuestDueState = 'overdue' | 'today' | 'soon' | 'later' | 'none';

/**
 * Single quest row inside the quest-board list. Owns all of its own pure
 * derivations (due date math, subtask counts, schedule eligibility) and
 * leans on parent-provided callables for label lookups that depend on the
 * page's options arrays — same pattern as quest-detail-sheet.
 *
 * Rendered inside `<ion-reorder-group>`: keeps `<ion-reorder>` at the host
 * level so drag reordering still works through the custom element.
 *
 * Note: the page that hosts this card uses ViewEncapsulation.None and ships
 * the `.quest-card`, `.quest-body`, `.meta-pill*`, etc. styles globally —
 * this component intentionally has no SCSS so it keeps the page's look.
 */
@Component({
  selector: 'app-quest-card',
  templateUrl: './quest-card.component.html',
  imports: [IonIcon, IonProgressBar, IonReorder],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestCardComponent {
  readonly quest = input.required<Quest>();
  readonly expanded = input<boolean>(false);
  readonly manualOrderActive = input<boolean>(false);

  // Parent-provided lookups — these depend on the page's typeOptions /
  // priorityOptions / recurrenceOptions arrays, so we don't duplicate them.
  readonly typeIcon = input<(type: QuestType) => string>(() => 'flag-outline');
  readonly typeLabel = input<(type: QuestType) => string>(() => '');
  readonly priorityLabel = input<(priority: QuestPriority) => string>(() => 'Medium');
  readonly recurrenceLabel = input<(recurrence: QuestRecurrence) => string>(() => 'No repeat');

  readonly toggleComplete = output<Quest>();
  readonly expand = output<Quest>();
  readonly scheduleToday = output<Quest>();
  readonly scheduleTomorrow = output<Quest>();
  readonly clearDueDate = output<Quest>();
  readonly requestDelete = output<Quest>();

  // Computed views over the quest input so the template doesn't recompute on
  // every CD pass.
  readonly dueState = computed<QuestDueState>(() => computeDueState(this.quest()));
  readonly dueDateLabel = computed(() => computeDueDateLabel(this.quest()));
  readonly subtaskCompletedCount = computed(
    () => this.quest().subtasks.filter((s) => s.completed).length,
  );
  readonly subtaskProgress = computed(() => {
    const subtasks = this.quest().subtasks;
    return subtasks.length ? this.subtaskCompletedCount() / subtasks.length : 0;
  });
  readonly canScheduleToday = computed(
    () => !this.quest().completed && this.dueState() !== 'today',
  );
  readonly canScheduleTomorrow = computed(() => {
    const quest = this.quest();
    if (quest.completed) return false;
    const tomorrow = toISODate(addDays(startOfDay(new Date()), 1));
    return quest.dueDate !== tomorrow;
  });
  readonly canMoveToInbox = computed(
    () => !this.quest().completed && Boolean(this.quest().dueDate),
  );
}

function computeDueState(quest: Quest): QuestDueState {
  if (!quest.dueDate) return 'none';
  const due = new Date(quest.dueDate);
  if (Number.isNaN(due.getTime())) return 'none';
  const today = startOfDay(new Date());
  const dueDay = startOfDay(due);
  if (dueDay < today) return 'overdue';
  if (dueDay.getTime() === today.getTime()) return 'today';
  const diff = (dueDay.getTime() - today.getTime()) / 86_400_000;
  return diff <= 3 ? 'soon' : 'later';
}

function computeDueDateLabel(quest: Quest): string {
  if (!quest.dueDate) return '';
  const due = new Date(quest.dueDate);
  if (Number.isNaN(due.getTime())) return '';
  const today = startOfDay(new Date());
  const dueDay = startOfDay(due);
  const diffDays = Math.round((dueDay.getTime() - today.getTime()) / 86_400_000);

  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Tomorrow';
  if (diffDays === -1) return 'Yesterday';
  if (diffDays < -1 && diffDays >= -7) return `${Math.abs(diffDays)}d overdue`;
  if (diffDays > 1 && diffDays <= 7) return `In ${diffDays}d`;

  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(due);
}
