import { ChangeDetectionStrategy, Component, computed, inject, input, model, output, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon } from '@ionic/angular/standalone';

import {
  Quest,
  QuestFolder,
  QuestPriority,
  QuestRecurrence,
  QuestSkill,
  QuestSubtask,
  QuestType,
} from '../../services/quest-board.service';
import { renderMarkdown } from '../../util/markdown';

export type QuestEditDraft = {
  title: string;
  notes: string;
  type: QuestType;
  priority: QuestPriority;
  recurrence: QuestRecurrence;
  dueDate: string | null;
  tags: string;
  myGameId: number | null;
  skillId: number | null;
  folderId: number | null;
};

type DetailTab = 'details' | 'notes' | 'subtasks';

type LibraryGame = { myGameId: number; gameName: string };
type TypeOption = { value: QuestType; label: string; icon: string };
type PriorityOption = { value: QuestPriority; label: string; weight: number };
type RecurrenceOption = { value: QuestRecurrence; label: string };

/**
 * Bottom-sheet style modal that combines per-quest scheduling shortcuts,
 * field-level editing, and subtask management. The form draft is two-way bound
 * so the parent owns the cancel-vs-save lifecycle.
 */
@Component({
  selector: 'app-quest-detail-sheet',
  templateUrl: './quest-detail-sheet.component.html',
  imports: [FormsModule, IonButton, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestDetailSheetComponent {
  private sanitizer = inject(DomSanitizer);

  readonly quest = input.required<Quest>();
  readonly editDraft = model.required<QuestEditDraft | null>();
  readonly typeOptions = input<TypeOption[]>([]);
  readonly priorityOptions = input<PriorityOption[]>([]);
  readonly recurrenceOptions = input<RecurrenceOption[]>([]);
  readonly library = input<LibraryGame[]>([]);
  readonly skills = input<QuestSkill[]>([]);
  readonly folders = input<QuestFolder[]>([]);

  // Parent-provided callables so the component can render labels without
  // depending on the option arrays directly. Keeps the template terse.
  readonly typeIcon = input<(type: QuestType) => string>(() => 'flag-outline');
  readonly typeLabel = input<(type: QuestType) => string>(() => '');
  readonly subtaskDraft = input<(questId: number) => string>(() => '');

  readonly setSubtaskDraft = output<{ questId: number; value: string }>();

  readonly close = output<void>();
  readonly scheduleToday = output<Quest>();
  readonly scheduleTomorrow = output<Quest>();
  readonly clearDueDate = output<Quest>();
  readonly save = output<Quest>();
  readonly toggleSubtask = output<{ quest: Quest; subtask: QuestSubtask }>();
  readonly deleteSubtask = output<{ quest: Quest; subtask: QuestSubtask }>();
  readonly addSubtask = output<Quest>();

  // ----- Internal tab state -------------------------------------------------
  /** Three tabs split the form so the sheet stays scannable on small screens. */
  protected readonly activeTab = signal<DetailTab>('details');
  /** When false, the notes tab shows rendered markdown; tap to flip into edit. */
  protected readonly notesEditing = signal(false);

  /** Rendered markdown for the Notes tab. `renderMarkdown` sanitizes the
   *  parsed HTML with DOMPurify before this value is trusted for binding. */
  protected readonly renderedNotes = computed<SafeHtml>(() => {
    const text = this.editDraft()?.notes ?? this.quest().notes ?? '';
    if (!text.trim()) return '';
    return this.sanitizer.bypassSecurityTrustHtml(renderMarkdown(text));
  });

  protected setTab(tab: DetailTab): void {
    this.activeTab.set(tab);
    // Switching off the notes tab implicitly commits the edit (the parent
    // will save on the global Save button). Reset the inline edit state.
    if (tab !== 'notes') this.notesEditing.set(false);
  }

  protected toggleNotesEdit(): void {
    this.notesEditing.update((on) => !on);
  }

  updateDraft<K extends keyof QuestEditDraft>(key: K, value: QuestEditDraft[K]): void {
    const draft = this.editDraft();
    if (!draft) return;
    this.editDraft.set({ ...draft, [key]: value });
  }

  subtaskCompletedCount(quest: Quest): number {
    return quest.subtasks.filter((s) => s.completed).length;
  }

  trackBySubtask(_: number, subtask: QuestSubtask): number {
    return subtask.id;
  }

  trackByLibrary(_: number, game: LibraryGame): number {
    return game.myGameId;
  }

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  trackByFolder(_: number, folder: QuestFolder): number {
    return folder.id;
  }
}
