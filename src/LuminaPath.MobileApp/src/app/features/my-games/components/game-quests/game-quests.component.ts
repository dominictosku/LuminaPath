import { Component, OnChanges, SimpleChanges, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonIcon,
  IonSelect,
  IonSelectOption,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  chevronDownOutline,
  chevronUpOutline,
  gameControllerOutline,
  linkOutline,
  trashOutline,
} from 'ionicons/icons';

import { Quest, QuestBoardService, QuestType } from 'src/app/features/quests/services/quest-board.service';

@Component({
  selector: 'app-game-quests',
  templateUrl: './game-quests.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [FormsModule, IonBadge, IonButton, IonIcon, IonSelect, IonSelectOption],
})
export class GameQuestsComponent implements OnChanges {
  private readonly questBoardService = inject(QuestBoardService);

  readonly myGameId = input<number | null>(null);
  readonly isInLibrary = input<boolean>(false);

  quests: Quest[] = [];
  newQuestTitle = '';
  newQuestType: QuestType = 'sub';
  completedExpanded = false;
  isAddingStarter = false;

  readonly questTypeOptions: { type: QuestType; label: string }[] = [
    { type: 'main', label: 'Main' },
    { type: 'sub', label: 'Sub' },
    { type: 'faction', label: 'Faction' },
  ];

  readonly questStarters: { title: string; type: QuestType }[] = [
    { title: 'Finish the main story', type: 'main' },
    { title: 'Reach max level', type: 'sub' },
    { title: '100% achievements', type: 'sub' },
  ];

  constructor() {
    addIcons({
      addOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      chevronDownOutline,
      chevronUpOutline,
      gameControllerOutline,
      linkOutline,
      trashOutline,
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['myGameId']) {
      void this.refresh();
    }
  }

  get activeQuests(): Quest[] {
    return this.quests.filter((quest) => !quest.completed);
  }

  get completedQuests(): Quest[] {
    return this.quests.filter((quest) => quest.completed);
  }

  toggleCompletedQuests(): void {
    this.completedExpanded = !this.completedExpanded;
  }

  questTypeFor(quest: Quest): QuestType {
    return quest.type;
  }

  questTypeLabel(type: QuestType): string {
    return this.questTypeOptions.find((option) => option.type === type)?.label ?? type;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  async addStarterQuest(suggestion: { title: string; type: QuestType }): Promise<void> {
    const id = this.myGameId();
    if (id == null || this.isAddingStarter) return;

    this.isAddingStarter = true;
    try {
      await this.questBoardService.createQuest({
        title: suggestion.title,
        type: suggestion.type,
        myGameId: id,
      });
      await this.refresh();
    } catch {
      // silent
    } finally {
      this.isAddingStarter = false;
    }
  }

  async addQuest(): Promise<void> {
    const title = this.newQuestTitle.trim();
    const id = this.myGameId();

    if (!title || id == null) return;

    try {
      await this.questBoardService.createQuest({
        title,
        type: this.newQuestType,
        myGameId: id,
      });
      this.newQuestTitle = '';
      await this.refresh();
    } catch {
      // silent; user-visible feedback can be added later
    }
  }

  async toggleQuest(quest: Quest): Promise<void> {
    const previous = quest.completed;
    quest.completed = !previous;
    try {
      await this.questBoardService.updateQuest(quest.id, { completed: !previous });
      await this.refresh();
    } catch {
      quest.completed = previous;
    }
  }

  async deleteQuest(quest: Quest): Promise<void> {
    const id = quest.id;
    this.quests = this.quests.filter((item) => item.id !== id);
    try {
      await this.questBoardService.deleteQuest(id);
    } catch {
      await this.refresh();
    }
  }

  private async refresh(): Promise<void> {
    const id = this.myGameId();
    if (id == null) {
      this.quests = [];
      return;
    }
    try {
      this.quests = await this.questBoardService.getQuestsForGame(id);
    } catch {
      this.quests = [];
    }
  }
}
