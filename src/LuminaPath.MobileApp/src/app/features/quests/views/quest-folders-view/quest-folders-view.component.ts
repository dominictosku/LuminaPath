import { Component, ViewEncapsulation, inject } from '@angular/core';
import { IonIcon, IonReorder, IonReorderGroup, ItemReorderEventDetail } from '@ionic/angular/standalone';
import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import {
  Quest,
  QuestFolder,
  QuestFolderCreate,
  QuestFolderUpdate,
} from '../../services/quest-board.service';
import { QuestDueState, dueDateLabel, dueState } from '../../util/quest-due';
import { QuestBoardStore } from '../../state/quest-board.store';
import { QuestFolderModalComponent } from '../../components/quest-folder-modal/quest-folder-modal.component';

type FolderModalState = { mode: 'create' | 'edit'; folder: QuestFolder | null };

/**
 * "Overview" mode: folders grouped by section, drag-and-drop quest filing,
 * folder reorder, and the create/edit folder modal. The modal's open/saving
 * flags are local; all persistence flows through {@link QuestBoardStore}.
 */
@Component({
  selector: 'app-quest-folders-view',
  templateUrl: './quest-folders-view.component.html',
  styleUrls: ['./quest-folders-view.component.scss'],
  imports: [
    IonIcon,
    IonReorder,
    IonReorderGroup,
    CdkDrag,
    CdkDropList,
    EmptyStateComponent,
    QuestFolderModalComponent,
  ],
  // CDK drag preview/placeholder classes are attached dynamically during
  // drag-drop; keeping these namespace-specific styles global preserves the
  // previous page-level behaviour after the stylesheet split.
  encapsulation: ViewEncapsulation.None,
})
export class QuestFoldersViewComponent {
  protected readonly store = inject(QuestBoardStore);

  protected folderModalState: FolderModalState | null = null;
  protected folderSaving = false;

  dueStateOf(quest: Quest): QuestDueState {
    return dueState(quest);
  }

  dueLabelOf(quest: Quest): string {
    return dueDateLabel(quest);
  }

  folderDropListId(folderId: number | null): string {
    return folderId == null ? 'quest-drop-unfiled' : `quest-drop-folder-${folderId}`;
  }

  trackByFolder(_: number, folder: QuestFolder): number {
    return folder.id;
  }

  trackByFolderGroup(_: number, group: { section: string | null }): string {
    return group.section ?? '__unfiled__';
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  openCreateFolderModal(): void {
    this.folderModalState = { mode: 'create', folder: null };
  }

  openEditFolderModal(folder: QuestFolder): void {
    this.folderModalState = { mode: 'edit', folder };
  }

  closeFolderModal(): void {
    if (this.folderSaving) return;
    this.folderModalState = null;
  }

  async createFolder(input: QuestFolderCreate): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      if (await this.store.createFolder(input)) {
        this.folderModalState = null;
      }
    } finally {
      this.folderSaving = false;
    }
  }

  async updateFolder(id: number, patch: QuestFolderUpdate): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      if (await this.store.updateFolder(id, patch)) {
        this.folderModalState = null;
      }
    } finally {
      this.folderSaving = false;
    }
  }

  async deleteFolder(folder: QuestFolder): Promise<void> {
    if (this.folderSaving) return;
    this.folderSaving = true;
    try {
      if (await this.store.deleteFolder(folder)) {
        this.folderModalState = null;
      }
    } finally {
      this.folderSaving = false;
    }
  }

  handleFolderReorder(event: CustomEvent): void {
    const detail = event.detail as ItemReorderEventDetail;
    detail.complete();
    void this.store.reorderFolders(detail.from, detail.to);
  }

  handleQuestDropToFolder(event: CdkDragDrop<unknown>): void {
    if (event.previousContainer === event.container) return;
    const quest = event.item.data as Quest;
    if (!quest) return;
    const raw = event.container.data;
    const targetFolderId = typeof raw === 'number' ? raw : null;
    void this.store.assignFolderToQuest(quest, targetFolderId);
  }
}
