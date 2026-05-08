import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type MediaMode = 'games' | 'animes' | 'movies';

export type MediaModeOption = {
  id: MediaMode;
  label: string;
  singular: string;
  catalogEndpoint: string;
  libraryEndpoint: string;
  libraryIdKey: 'gameId' | 'animeId' | 'movieId';
  themeClass: string;
  icon: string;
};

const STORAGE_KEY = 'luminapath.mediaMode';

export const MEDIA_MODE_OPTIONS: MediaModeOption[] = [
  {
    id: 'games',
    label: 'Games',
    singular: 'game',
    catalogEndpoint: 'games',
    libraryEndpoint: 'mygames',
    libraryIdKey: 'gameId',
    themeClass: 'theme-games',
    icon: 'game-controller-outline',
  },
  {
    id: 'animes',
    label: 'Animes',
    singular: 'anime',
    catalogEndpoint: 'animes',
    libraryEndpoint: 'myanimes',
    libraryIdKey: 'animeId',
    themeClass: 'theme-animes',
    icon: 'sparkles-outline',
  },
  {
    id: 'movies',
    label: 'Movies',
    singular: 'movie',
    catalogEndpoint: 'movies',
    libraryEndpoint: 'mymovies',
    libraryIdKey: 'movieId',
    themeClass: 'theme-movies',
    icon: 'film-outline',
  },
];

@Injectable({
  providedIn: 'root',
})
export class MediaModeService {
  private readonly modeSubject = new BehaviorSubject<MediaModeOption>(this.readInitialMode());

  readonly mode$ = this.modeSubject.asObservable();
  readonly options = MEDIA_MODE_OPTIONS;

  get current(): MediaModeOption {
    return this.modeSubject.value;
  }

  select(mode: MediaMode): void {
    const next = this.options.find((option) => option.id === mode) ?? this.options[0];
    this.modeSubject.next(next);
    localStorage.setItem(STORAGE_KEY, next.id);
  }

  private readInitialMode(): MediaModeOption {
    const saved = localStorage.getItem(STORAGE_KEY) as MediaMode | null;
    return MEDIA_MODE_OPTIONS.find((option) => option.id === saved) ?? MEDIA_MODE_OPTIONS[0];
  }
}
