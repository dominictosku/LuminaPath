import { Component, OnInit, inject } from '@angular/core';
import { Game } from '../../models/games.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { IonicFunctionsService } from 'src/app/shared/services/ionic-functions.service';
import { MyGameFormComponent } from 'src/app/features/my-games/components/my-game-form/my-game-form.component';
import { IonFab, IonFabButton, IonIcon } from '@ionic/angular/standalone';
import { addIcons } from "ionicons";
import { create } from 'ionicons/icons';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

@Component({
    selector: 'app-grid-game-data',
    templateUrl: './grid-game-data.component.html',
    styleUrls: ['./grid-game-data.component.scss'],
    imports: [IonFab, IonFabButton, IonIcon]
})
export class GridGameDataComponent implements OnInit {
  private gameService = inject(GameService);
  private ionicFunctions = inject(IonicFunctionsService);


  constructor() {
    addIcons({ create });
  }

  ngOnInit() {
    this.gameService.getAll().subscribe((event: any) => {
      this.games = event.data
    });
  }

  games: Game[] = [];

  openModal() {
    this.ionicFunctions.openModal(MyGameFormComponent, {});
  }

  navigate() {

  }

  GroupedMedia = () => {
    const media = this.games;
    return media.reduce((result: any, item) => {
      const date = new Date(item.releaseDate);
      const month = date.getMonth() + 1; // Months are zero-based, so we add 1 to get the actual month.
      const year = date.getFullYear();
      const key = `${year}-${month}`;

      if (!result[key]) {
        result[key] = [];
      }

      result[key].push(item);
      return result;
    }, {});
  }

  sortedKeys = () => {
    return Object.keys(this.GroupedMedia()).sort((a, b) => {
      // Convert the keys (in the format "MM-YYYY") to date objects for comparison
      const dateA: any = new Date(b);
      const dateB: any = new Date(a);
      return dateA - dateB;
    });
  }

  formatMonthYear(dateString: string) {
    const [year, month] = dateString.split('-');
    const monthNames = [
      'January', 'February', 'March', 'April',
      'May', 'June', 'July', 'August',
      'September', 'October', 'November', 'December'
    ];

    const formattedDate = `${monthNames[parseInt(month) - 1]} ${year}`;
    return formattedDate;
  }

  getImage(game: Game) {
    return mediaImageUrl(game?.image);
  }
}
