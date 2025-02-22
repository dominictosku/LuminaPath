import { Injectable } from '@angular/core';
import { Game } from '../models/games.model';
import { ApiService } from '../../../shared/services/api.service';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class GameService extends ApiService<Game> {
  constructor(private httpClient: HttpClient) {
    super(httpClient, "games");
  }

  public labels = ['Title', 'Description', 'Status', 'Release'];
  Id: string = 'games';

  createMedia(media: Game, endPoint: string) {
    if (media.id == 0) {
      this.post(media);
    } else {
      this.put(media.id, media);
    }
    return;
  }
}
