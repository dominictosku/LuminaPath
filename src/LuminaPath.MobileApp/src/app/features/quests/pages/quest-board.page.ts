import { Component, OnDestroy, OnInit, ViewEncapsulation, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonContent, IonIcon, IonLabel, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';
import { Subscription } from 'rxjs';

import { Quest } from '../services/quest-board.service';
import { QuestBoardFeedbackService } from '../services/quest-board-feedback.service';
import { QuestBoardStore } from '../state/quest-board.store';
import {
  QUEST_PRIORITY_OPTIONS,
  QUEST_RECURRENCE_OPTIONS,
  QUEST_TYPE_OPTIONS,
  questTypeIcon,
  questTypeLabel,
} from '../models/quest-board-view.model';
import { QuestBoardHeroComponent } from '../components/quest-board-hero/quest-board-hero.component';
import { QuestDetailSheetComponent } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import { QuestListViewComponent } from '../views/quest-list-view/quest-list-view.component';
import { QuestFoldersViewComponent } from '../views/quest-folders-view/quest-folders-view.component';
import { QuestSkillsViewComponent } from '../views/quest-skills-view/quest-skills-view.component';

/**
 * Quest-board shell: hero, the mode segment, the active per-mode view, the
 * shared inline-edit detail sheet, and the floating toasts. All board state and
 * behaviour lives in {@link QuestBoardStore}; this component only wires the
 * route deep-link to the store and renders the chrome.
 */
@Component({
  selector: 'app-quest-board',
  templateUrl: './quest-board.page.html',
  styleUrls: ['./quest-board.page.scss'],
  providers: [QuestBoardFeedbackService, QuestBoardStore],
  // Quest-board ships a single coherent visual system whose selectors are well
  // namespaced (`.quest-*`, `.skill-*`). Loading them globally on this route
  // lets all sub-components share the styling without duplicating SCSS.
  encapsulation: ViewEncapsulation.None,
  imports: [
    FormsModule,
    IonContent,
    IonIcon,
    IonLabel,
    IonSegment,
    IonSegmentButton,
    QuestBoardHeroComponent,
    QuestDetailSheetComponent,
    QuestListViewComponent,
    QuestFoldersViewComponent,
    QuestSkillsViewComponent,
  ],
})
export class QuestBoardPage implements OnInit, OnDestroy {
  protected readonly store = inject(QuestBoardStore);
  protected readonly feedback = inject(QuestBoardFeedbackService);
  private readonly route = inject(ActivatedRoute);

  // Option lists + label lookups the shared detail sheet needs.
  protected readonly typeOptions = [...QUEST_TYPE_OPTIONS];
  protected readonly priorityOptions = [...QUEST_PRIORITY_OPTIONS];
  protected readonly recurrenceOptions = [...QUEST_RECURRENCE_OPTIONS];
  protected readonly typeIcon = questTypeIcon;
  protected readonly typeLabel = questTypeLabel;
  protected readonly subtaskDraftFn = (questId: number) => this.store.subtaskDraft(questId);

  private routeSub?: Subscription;

  ngOnInit(): void {
    // Deep link: ?questId=N opens that quest. The store applies it once the
    // board has loaded (and on every subsequent query-param change).
    this.routeSub = this.route.queryParamMap.subscribe((params) => {
      const questId = Number(params.get('questId'));
      this.store.focusQuest(Number.isInteger(questId) && questId > 0 ? questId : null);
    });
  }

  // Ionic keeps this page alive in the router outlet, so ngOnInit only runs
  // once. Reload the board on every entry so quests created on other pages
  // (e.g. a game's detail page) appear when the user returns here.
  ionViewWillEnter(): void {
    void this.store.load();
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }
}
