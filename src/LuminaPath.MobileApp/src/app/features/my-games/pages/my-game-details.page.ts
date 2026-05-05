import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonSelect,
  IonSelectOption,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  arrowBackOutline,
  calendarClearOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  gameControllerOutline,
  hourglassOutline,
  libraryOutline,
  linkOutline,
  trashOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';

import { GameService } from 'src/app/features/games/services/game.service';
import { Game, Platforms } from 'src/app/features/games/models/games.model';
import { Quest, QuestBoardService, QuestType } from 'src/app/features/quests/services/quest-board.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

const STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Playing',
  3: 'Story complete',
  4: 'Completed',
  5: 'Main game',
};

type GameWithFlexibleLibrary = Game & {
  myGames?: { id?: number; status?: number } | { id?: number; status?: number }[] | null;
};

@Component({
  selector: 'app-my-game-details',
  templateUrl: './my-game-details.page.html',
  styleUrls: ['./my-game-details.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonSelect,
    IonSelectOption,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class MyGameDetailsPage implements OnInit {
  game: GameWithFlexibleLibrary | null = null;
  quests: Quest[] = [];
  isLoading = true;
  errorMessage = '';

  readonly questTypeOptions: { type: QuestType; label: string }[] = [
    { type: 'main', label: 'Main' },
    { type: 'sub', label: 'Sub' },
    { type: 'faction', label: 'Faction' },
  ];

  newQuestTitle = '';
  newQuestType: QuestType = 'sub';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly gameService: GameService,
    private readonly questBoardService: QuestBoardService,
  ) {
    addIcons({
      addOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      gameControllerOutline,
      hourglassOutline,
      libraryOutline,
      linkOutline,
      trashOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    const gameId = Number(this.route.snapshot.paramMap.get('gameId'));

    if (!Number.isInteger(gameId) || gameId <= 0) {
      this.showError('Game not found.');
      return;
    }

    await this.loadGameAndQuests(gameId);
  }

  get libraryEntry(): { id?: number; status?: number } | null {
    const entry = this.game?.myGames;
    if (Array.isArray(entry)) {
      return entry[0] ?? null;
    }
    return entry ?? null;
  }

  get myGameId(): number | null {
    const id = Number(this.libraryEntry?.id);
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  get isInLibrary(): boolean {
    return this.myGameId !== null;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.libraryEntry?.status;
    return status == null ? 'Catalog' : STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get platformLabel(): string {
    return Platforms.find((platform) => platform.value === Number(this.game?.platforms))?.label ?? 'Unknown platform';
  }

  get releaseLabel(): string {
    if (!this.game?.releaseDate) {
      return 'No release date';
    }

    const date = new Date(this.game.releaseDate);
    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  get playtimeLabel(): string {
    return this.game?.playtime ? `${this.game.playtime}h estimated` : 'No estimate';
  }

  get completedQuestCount(): number {
    return this.quests.filter((quest) => quest.completed).length;
  }

  get questSummaryLabel(): string {
    if (!this.quests.length) {
      return 'No quests linked yet';
    }
    return `${this.completedQuestCount}/${this.quests.length} quests complete`;
  }

  imageUrl(): string {
    return mediaImageUrl(this.game?.image);
  }

  async addQuest(): Promise<void> {
    const title = this.newQuestTitle.trim();
    const myGameId = this.myGameId;

    if (!title || myGameId == null) {
      return;
    }

    const board = await this.questBoardService.getBoard();
    board.quests[this.newQuestType] = [
      ...board.quests[this.newQuestType],
      {
        id: Date.now(),
        title,
        completed: false,
        createdAt: new Date().toISOString(),
        myGameId,
      },
    ];

    await this.questBoardService.saveBoard(board);
    this.newQuestTitle = '';
    await this.refreshQuests();
  }

  async toggleQuest(quest: Quest): Promise<void> {
    const board = await this.questBoardService.getBoard();
    const targetType = this.questTypeFor(quest);
    const target = board.quests[targetType].find((item) => item.id === quest.id);

    if (!target) {
      return;
    }

    target.completed = !target.completed;
    target.completedAt = target.completed ? new Date().toISOString() : undefined;

    await this.questBoardService.saveBoard(board);
    await this.refreshQuests();
  }

  async deleteQuest(quest: Quest): Promise<void> {
    const board = await this.questBoardService.getBoard();
    for (const type of this.questTypeOptions.map((option) => option.type)) {
      board.quests[type] = board.quests[type].filter((item) => item.id !== quest.id);
    }

    await this.questBoardService.saveBoard(board);
    await this.refreshQuests();
  }

  questTypeFor(quest: Quest): QuestType {
    if (quest.rewardXp === 150) return 'main';
    if (quest.rewardXp === 100) return 'faction';
    return 'sub';
  }

  questTypeLabel(type: QuestType): string {
    return this.questTypeOptions.find((option) => option.type === type)?.label ?? type;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/media');
  }

  private async loadGameAndQuests(gameId: number): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.game = await firstValueFrom(this.gameService.get(gameId));
      await this.refreshQuests();
    } catch {
      this.showError('Game could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  private async refreshQuests(): Promise<void> {
    const myGameId = this.myGameId;
    this.quests = myGameId == null ? [] : await this.questBoardService.getQuestsForGame(myGameId);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
