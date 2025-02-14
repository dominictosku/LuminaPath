import { Injectable } from '@angular/core';
import { Game } from '../models/games';
import { ApiService } from './api.service';
import { MediaFilter } from '../entities/mediaFilter';
import { IBasicInfo } from '../interfaces/iBasicInfo';
import { InMemoryDataService } from './in-memory-data.service';

@Injectable({
  providedIn: 'root',
})
export class GameService {
  constructor(private api: ApiService) {}

  public type = {
    Games: 'Games',
    MyGames: 'MyGames',
  };

  public labels = ['Title', 'Description', 'Status', 'Release'];
  Id: string = 'games';

  getMedia(filter?: MediaFilter | undefined) {
    let response = this.api.fetchPaginatedMedia<Game>(this.Id, filter);
    return response;
  }

  getMediaById(id: number, endPoint: string) {
    return this.api.fetchMediaById(id, endPoint);
  }

  removeMedia(id: number, endPoint: string) {
    this.api.deleteMedia(id, endPoint);
    return;
  }

  createMedia(media: IBasicInfo, endPoint: string) {
    if (media.id == 0) {
      this.api.PostMedia(media, endPoint);
    } else {
      this.api.PutMedia(media, endPoint);
    }
    return;
  }
}
