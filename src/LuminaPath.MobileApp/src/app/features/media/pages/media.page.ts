import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonIcon,
  IonModal,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSelect,
  IonSelectOption,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  albumsOutline,
  addOutline,
  calendarClearOutline,
  checkmarkCircleOutline,
  closeOutline,
  gameControllerOutline,
  gridOutline,
  hourglassOutline,
  listOutline,
  searchOutline,
  starOutline,
  timeOutline,
} from 'ionicons/icons';
import { Game, Platforms } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MyGameService } from '../../my-games/services/my-game.service';

enum GameStatus {
  OnHold = 0,
  Planned = 1,
  Playing = 2,
  StoryComplete = 3,
  Completed = 4,
  MainGame = 5,
}

type ViewMode = 'grid' | 'list';
type OwnershipFilter = 'all' | 'mine' | 'catalog';
type AddGameForm = {
  status: number;
  timeSpend: number | null;
  rating: number | null;
  startDate: string;
  endDate: string;
};

@Component({
  selector: 'app-media',
  templateUrl: './media.page.html',
  styleUrls: ['./media.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonModal,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonSegment,
    IonSegmentButton,
    IonSelect,
    IonSelectOption,
    IonSkeletonText,
  ],
})
export class MediaPage implements OnInit {
  games: Game[] = [];
  filteredGames: Game[] = [];
  searchTerm = '';
  ownershipFilter: OwnershipFilter = 'all';
  statusFilter = 'all';
  platformFilter = 'all';
  viewMode: ViewMode = 'grid';
  isLoading = true;
  errorMessage = '';
  successMessage = '';
  addingGameIds = new Set<number>();
  selectedGame: Game | null = null;
  isAddDialogOpen = false;
  addGameForm: AddGameForm = this.createAddGameForm();

  readonly platforms = Platforms;
  readonly statusOptions = [
    { label: 'On hold', value: GameStatus.OnHold },
    { label: 'Planned', value: GameStatus.Planned },
    { label: 'Playing', value: GameStatus.Playing },
    { label: 'Story complete', value: GameStatus.StoryComplete },
    { label: 'Completed', value: GameStatus.Completed },
    { label: 'Main game', value: GameStatus.MainGame },
  ];

  constructor(
    private gameService: GameService,
    private myGameService: MyGameService
  ) {
    addIcons({
      addOutline,
      albumsOutline,
      calendarClearOutline,
      checkmarkCircleOutline,
      closeOutline,
      gameControllerOutline,
      gridOutline,
      hourglassOutline,
      listOutline,
      searchOutline,
      starOutline,
      timeOutline,
    });
  }

  ngOnInit() {
    this.loadGames();
  }

  loadGames(event?: CustomEvent) {
    this.isLoading = !event;
    this.errorMessage = '';

    this.gameService.getAll().subscribe({
      next: (result) => {
        this.games = result.data ?? [];
        this.applyFilters();
        this.isLoading = false;
        this.completeRefresh(event);
      },
      error: () => {
        this.games = [];
        this.filteredGames = [];
        this.errorMessage = 'Games could not be loaded.';
        this.isLoading = false;
        this.completeRefresh(event);
      },
    });
  }

  applyFilters() {
    const normalizedSearch = this.searchTerm.trim().toLowerCase();

    this.filteredGames = this.games
      .filter((game) => this.matchesOwnership(game))
      .filter((game) => this.matchesStatus(game))
      .filter((game) => this.matchesPlatform(game))
      .filter((game) => {
        if (!normalizedSearch) {
          return true;
        }

        return [
          game.name,
          game.description,
          game.genre,
          this.platformLabel(game.platforms),
          this.statusLabel(game),
        ]
          .filter(Boolean)
          .some((value) => String(value).toLowerCase().includes(normalizedSearch));
      })
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  clearFilters() {
    this.searchTerm = '';
    this.ownershipFilter = 'all';
    this.statusFilter = 'all';
    this.platformFilter = 'all';
    this.applyFilters();
  }

  openAddDialog(game: Game) {
    if (game.myGames || this.addingGameIds.has(game.id)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.selectedGame = game;
    this.addGameForm = this.createAddGameForm();
    this.isAddDialogOpen = true;
  }

  closeAddDialog() {
    if (this.selectedGame && this.isAdding(this.selectedGame)) {
      return;
    }

    this.isAddDialogOpen = false;
    this.selectedGame = null;
  }

  submitAddGame() {
    const game = this.selectedGame;

    if (!game || game.myGames || this.addingGameIds.has(game.id)) {
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.addingGameIds.add(game.id);

    this.myGameService.addToLibrary(game.id, {
      status: Number(this.addGameForm.status),
      timeSpend: this.numberOrNull(this.addGameForm.timeSpend),
      rating: this.numberOrNull(this.addGameForm.rating),
      startDate: this.addGameForm.startDate || null,
      endDate: this.addGameForm.endDate || null,
    }).subscribe({
      next: (myGame) => {
        game.myGames = myGame;
        this.applyFilters();
        this.successMessage = `${game.name} was added to your game list.`;
        this.addingGameIds.delete(game.id);
        this.isAddDialogOpen = false;
        this.selectedGame = null;
      },
      error: (error) => {
        this.errorMessage = this.addGameErrorMessage(error);
        this.addingGameIds.delete(game.id);
      },
    });
  }

  isAdding(game: Game): boolean {
    return this.addingGameIds.has(game.id);
  }

  get totalGames() {
    return this.games.length;
  }

  get ownedGames() {
    return this.games.filter((game) => !!game.myGames).length;
  }

  get playingGames() {
    return this.games.filter((game) => this.statusOf(game) === GameStatus.Playing).length;
  }

  get remainingHours() {
    return Math.round(
      this.games
        .filter((game) => !!game.myGames)
        .reduce((sum, game) => sum + this.remainingOf(game), 0)
    );
  }

  imageFor(game: Game): string {
    return mediaImageUrl(game.image);
  }

  platformLabel(value: number | null | undefined): string {
    return Platforms.find((platform) => platform.value === Number(value))?.label ?? 'Unknown';
  }

  statusLabel(game: Game): string {
    const status = this.statusOptions.find((option) => option.value === this.statusOf(game));
    return status?.label ?? 'Catalog';
  }

  releaseLabel(game: Game): string {
    const date = game.releaseDate ? new Date(game.releaseDate) : null;

    if (!date || Number.isNaN(date.getTime())) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  progressOf(game: Game): number {
    const estimated = Number(game.playtime) || 0;

    if (estimated <= 0) {
      return this.statusOf(game) === GameStatus.Completed ? 100 : 0;
    }

    return Math.min(100, Math.round((this.playedOf(game) / estimated) * 100));
  }

  playedLabel(game: Game): string {
    return `${Math.round(this.playedOf(game))}h played`;
  }

  remainingLabel(game: Game): string {
    return `${Math.round(this.remainingOf(game))}h left`;
  }

  trackByGameId(_: number, game: Game): number {
    return game.id;
  }

  private matchesOwnership(game: Game): boolean {
    if (this.ownershipFilter === 'mine') {
      return !!game.myGames;
    }

    if (this.ownershipFilter === 'catalog') {
      return !game.myGames;
    }

    return true;
  }

  private matchesStatus(game: Game): boolean {
    return this.statusFilter === 'all' || this.statusOf(game) === Number(this.statusFilter);
  }

  private matchesPlatform(game: Game): boolean {
    return this.platformFilter === 'all' || Number(game.platforms) === Number(this.platformFilter);
  }

  private playedOf(game: Game): number {
    const manual = Number(game.myGames?.timeSpend) || 0;
    const tracked = Number(game.myGames?.myGameInfo?.trackedHours) || 0;
    return manual + tracked;
  }

  private remainingOf(game: Game): number {
    return Math.max(0, (Number(game.playtime) || 0) - this.playedOf(game));
  }

  private statusOf(game: Game): number {
    return Number(game.myGames?.status ?? -1);
  }

  private completeRefresh(event?: CustomEvent) {
    const target = event?.target as HTMLIonRefresherElement | undefined;
    target?.complete();
  }

  private createAddGameForm(): AddGameForm {
    return {
      status: GameStatus.Planned,
      timeSpend: 0,
      rating: null,
      startDate: '',
      endDate: '',
    };
  }

  private numberOrNull(value: number | string | null): number | null {
    if (value === null || value === '') {
      return null;
    }

    const numericValue = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private addGameErrorMessage(error: unknown): string {
    const payload = (error as { error?: unknown })?.error;

    if (typeof payload === 'string') {
      return payload;
    }

    if (payload && typeof payload === 'object' && 'message' in payload) {
      return String((payload as { message: unknown }).message);
    }

    if (payload && typeof payload === 'object' && 'errorMessage' in payload) {
      const messages = (payload as { errorMessage: unknown }).errorMessage;
      return Array.isArray(messages) ? messages.join(' ') : String(messages);
    }

    if (payload && typeof payload === 'object' && 'errors' in payload) {
      const errors = (payload as { errors: Record<string, string[]> }).errors;
      const messages = Object.values(errors).flat();
      return messages.length ? messages.join(' ') : 'Game could not be added to your list.';
    }

    return 'Game could not be added to your list.';
  }
}
