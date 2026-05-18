import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from 'src/app/shared/services/api.service';
import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';
import { MyGame } from '../../games/models/games.model';

export type AddMyGameRequest = {
  id: number;
  gameId: number;
  status: number;
  timeSpend: number | null;
  rating?: number | null;
  startDate?: Date | string | null;
  endDate?: Date | string | null;
  personalNotes?: string | null;
};

export type UserGameAchievement = {
  id: number;
  gameAchievementId: number;
  provider: number;
  providerName: string;
  sourceAchievementId: string;
  title: string;
  description?: string | null;
  iconUrl?: string | null;
  isHidden: boolean;
  trophyType?: string | null;
  unlockedAt?: string | null;
  syncedAt: string;
};

@Injectable({
  providedIn: 'root',
})
export class MyGameService extends ApiService<MyGame> {
  constructor() {
    const httpClient = inject(HttpClient);
    const apiEndpoint = inject(ApiEndpointService);

    super(httpClient, apiEndpoint, 'mygames');
  }

  addToLibrary(gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: 0,
      gameId,
      ...details,
    };

    return this.post(request as MyGame);
  }

  updateLibraryEntry(myGameId: number, gameId: number, details: Omit<AddMyGameRequest, 'id' | 'gameId'>) {
    const request: AddMyGameRequest = {
      id: myGameId,
      gameId,
      ...details,
    };

    return this.put(myGameId, request as MyGame);
  }

  getAchievements(myGameId: number) {
    return this.http.get<UserGameAchievement[]>(`${this.apiUrl}/${myGameId}/achievements`, {
      withCredentials: true,
    });
  }
}
