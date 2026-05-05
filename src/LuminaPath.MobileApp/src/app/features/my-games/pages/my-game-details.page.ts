import { CommonModule } from '@angular/common';
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
  linkOutline,
  trashOutline,
} from 'ionicons/icons';
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
  game: Game | null = null;
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
    private route: ActivatedRoute,
    private router: Router,
    private gameService: GameService,
    private questBoardService: QuestBoardService,
  ) {
    addIcons({
      addOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      gameControllerOutline,
      hourglassOutline,
      linkOutline,
      trashOutline,
    });
  }

  async ngOnInit() {
    const gameId = Number(this.route.snapshot.paramMap.get('gameId'));

    if (!Number.isFinite(gameId) || gameId <= 0) {
      this.errorMessage = 'Game not found.';
      this.isLoading = false;
      return;
    }

    await this.loadGameAndQuests(gameId);
  }

  get myGameId(): number | null {
    return this.game?.myGames?.id ?? null;
  }

  get isInLibrary(): boolean {
    return this.myGameId != null;
  }

  get statusLabel(): string {
    const status = this.game?.myGames?.status;
    return status == null ? 'Catalog' : STATUS_LABELS[status] ?? 'Catalog';
  }

  get platformLabel(): string {
    return Platforms.find((platform) => platform.value === Number(this.game?.platforms))?.label ?? 'Unknown';
  }

  get releaseLabel(): string {
    if (!this.game?.releaseDate) {
      return 'No date';
    }

    const date = new Date(this.game.releaseDate);
    if (Number.isNaN(date.getTime())) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
  }

  get playtimeLabel(): string {
    return this.game?.playtime ? `${this.game.playtime}h estimated` : 'No estimate';
  }

  imageUrl(): string {
    return mediaImageUrl(this.game?.image);
  }

  async addQuest() {
    const title = this.newQuestTitle.trim();
    const myGameId = this.myGameId;

    if (!title || myGameId == null) {
      return;
    }

    const board = await this.questBoardService.getBoard();
    board.quests[this.newQuestType] = [
      ...board.quests[this.newQuestType],
      {
        id: 0,
        title,
        completed: false,
        createdAt: new Date().toISOString(),
        myGameId,
      },
    ];

    await this.questBoardService.saveBoard(board);
    this.newQuestTitle = '';
    this.quests = await this.questBoardService.getQuestsForGame(myGameId);
  }

  async toggleQuest(quest: Quest) {
    const board = await this.questBoardService.getBoard();
    const allQuests = [...board.quests.main, ...board.quests.sub, ...board.quests.faction];
    const target = allQuests.find((item) => item.id === quest.id);

    if (!target) {
      return;
    }

    target.completed = !target.completed;
    target.completedAt = target.completed ? new Date().toISOString() : undefined;

    await this.questBoardService.saveBoard(board);
    if (this.myGameId != null) {
      this.quests = await this.questBoardService.getQuestsForGame(this.myGameId);
    }
  }

  async deleteQuest(quest: Quest) {
    const board = await this.questBoardService.getBoard();
    for (const type of ['main', 'sub', 'faction'] as QuestType[]) {
      board.quests[type] = board.quests[type].filter((item) => item.id !== quest.id);
    }

    await this.questBoardService.saveBoard(board);
    if (this.myGameId != null) {
      this.quests = await this.questBoardService.getQuestsForGame(this.myGameId);
    }
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

  goBack() {
    this.router.navigate(['/media']);
  }

  private async loadGameAndQuests(gameId: number) {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const game = await new Promise<Game>((resolve, reject) => {
        this.gameService.get(gameId).subscribe({ next: resolve, error: reject });
      });
      this.game = game;

      if (game.myGames) {
        this.quests = await this.questBoardService.getQuestsForGame(game.myGames.id);
      } else {
        this.quests = [];
      }
    } catch {
      this.errorMessage = 'Game could not be loaded.';
    } finally {
      this.isLoading = false;
    }
  }
}
