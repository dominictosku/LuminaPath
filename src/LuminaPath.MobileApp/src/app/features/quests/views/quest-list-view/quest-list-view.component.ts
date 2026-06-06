import { Component, inject } from '@angular/core';
import { IonBadge, IonIcon, IonReorderGroup } from '@ionic/angular/standalone';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { Quest } from '../../services/quest-board.service';
import { QuestDueState, dueDateLabel, dueState } from '../../util/quest-due';
import { QuestFilter, QuestSection } from '../../domain/quest-sections.builder';
import {
  QUEST_FILTER_OPTIONS,
  QUEST_PRIORITY_OPTIONS,
  QUEST_RECURRENCE_OPTIONS,
  QUEST_TYPE_OPTIONS,
  questPriorityLabel,
  questRecurrenceLabel,
  questTypeIcon,
  questTypeLabel,
} from '../../models/quest-board-view.model';
import { QuestBoardStore } from '../../state/quest-board.store';
import { QuestQuickAddComponent } from '../../components/quest-quick-add/quest-quick-add.component';
import { QuestBoardToolbarComponent } from '../../components/quest-board-toolbar/quest-board-toolbar.component';
import { QuestAchievementsComponent } from '../../components/quest-achievements/quest-achievements.component';
import { QuestCardComponent } from '../../components/quest-card/quest-card.component';
import { QuestFolderPickerComponent } from '../../components/quest-folder-picker/quest-folder-picker.component';

/**
 * "Quests" mode: capture row, filter/search toolbar, and the grouped quest
 * sections (card or compact layout). Pure view layer over {@link QuestBoardStore} —
 * the only local state is which compact row currently has its folder picker open.
 */
@Component({
  selector: 'app-quest-list-view',
  templateUrl: './quest-list-view.component.html',
  styleUrls: ['./quest-list-view.component.scss'],
  imports: [
    IonBadge,
    IonIcon,
    IonReorderGroup,
    EmptyStateComponent,
    QuestQuickAddComponent,
    QuestBoardToolbarComponent,
    QuestAchievementsComponent,
    QuestCardComponent,
    QuestFolderPickerComponent,
  ],
})
export class QuestListViewComponent {
  protected readonly store = inject(QuestBoardStore);

  protected readonly typeOptions = [...QUEST_TYPE_OPTIONS];
  protected readonly priorityOptions = [...QUEST_PRIORITY_OPTIONS];
  protected readonly recurrenceOptions = [...QUEST_RECURRENCE_OPTIONS];
  protected readonly filterOptions = [...QUEST_FILTER_OPTIONS];

  /** Compact view only: id of the row whose folder-picker popover is open. */
  protected compactFolderPickerQuestId: number | null = null;

  // Label/icon lookups exposed as properties so the template can both call
  // them (compact rows) and pass them as function inputs to child components.
  protected readonly typeIcon = questTypeIcon;
  protected readonly typeLabel = questTypeLabel;
  protected readonly priorityLabel = questPriorityLabel;
  protected readonly recurrenceLabel = questRecurrenceLabel;
  protected readonly filterCountFn = (filter: QuestFilter) => this.store.filterCount(filter);

  dueStateOf(quest: Quest): QuestDueState {
    return dueState(quest);
  }

  dueLabelOf(quest: Quest): string {
    return dueDateLabel(quest);
  }

  subtaskCompletedCount(quest: Quest): number {
    return quest.subtasks.filter((subtask) => subtask.completed).length;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackByQuestSection(_: number, section: QuestSection): string {
    return section.id;
  }

  handleReorder(event: CustomEvent, sectionQuests: Quest[]): void {
    const detail = event.detail as { from: number; to: number; complete: () => void };
    detail.complete();
    void this.store.reorderQuests(detail.from, detail.to, sectionQuests);
  }

  openCompactFolderPicker(quest: Quest, event: MouseEvent): void {
    event.stopPropagation();
    this.compactFolderPickerQuestId = quest.id;
  }

  closeCompactFolderPicker(): void {
    this.compactFolderPickerQuestId = null;
  }

  pickCompactFolder(quest: Quest, folderId: number | null): void {
    this.compactFolderPickerQuestId = null;
    void this.store.assignFolderToQuest(quest, folderId);
  }
}
