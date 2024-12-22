import { Component, OnInit } from '@angular/core';
import { Game } from 'src/app/core/models/games';
import { GameService } from 'src/app/core/services/game.service';
import { IonicFunctionsService } from 'src/app/core/services/ionic-functions.service';
import { MyGameFormComponent } from 'src/app/ui/components/forms/my-game-form/my-game-form.component';
import { IonFab, IonFabButton, IonIcon, IonInfiniteScrollContent, IonInfiniteScroll } from '@ionic/angular/standalone';
import { addIcons } from "ionicons";
import { create } from 'ionicons/icons';

@Component({
  selector: 'app-grid-game-data',
  standalone: true,
  templateUrl: './grid-game-data.component.html',
  styleUrls: ['./grid-game-data.component.scss'],
  imports: [IonInfiniteScrollContent, IonFab, IonFabButton, IonIcon, IonInfiniteScroll]
})
export class GridGameDataComponent implements OnInit {

  constructor(private gameService: GameService, private ionicFunctions: IonicFunctionsService) {
    addIcons({ create });
  }

  ngOnInit() {
    this.gameService.getMedia().subscribe((event: any) => {
      this.games = event.data
    });
  }

  countMedia = 50

  games: Game[] = [];

  openModal() {
    this.ionicFunctions.openModal(MyGameFormComponent, {});
  }

  navigate() {

  }

  ionInfinite = async (ev: any) => {
    this.countMedia += 50;
    this.gameService.getMedia().subscribe((event: any) => {
      this.games = event.data
    });
    setTimeout(() => {
      ev.target.complete();
    }, 500);
  };

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
    if (game == undefined || game.image == null) {
      return "assets/png/Placeholder.png";
    }
    return game.image.uri;
  }
}
