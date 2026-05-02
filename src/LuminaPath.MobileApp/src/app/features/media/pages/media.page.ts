import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, IonRefresherContent } from '@ionic/angular/standalone';
import { TableComponent } from "../../../shared/components/media/table/table.component";
import { GridComponent } from "../../../shared/components/media/grid/grid.component";
import { ErrorComponent } from "../../../shared/components/error/error.component";
import { LoadingComponent } from "../../../shared/components/loading/loading.component";
import { SelectorComponent } from "../../../shared/components/selector/selector.component";
import { FilterComponent } from "../../../shared/components/media/filter/filter.component";

@Component({
    selector: 'app-media',
    templateUrl: './media.page.html',
    styleUrls: ['./media.page.scss'],
    imports: [IonRefresherContent, IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, CommonModule, FormsModule, TableComponent, GridComponent, ErrorComponent, LoadingComponent, SelectorComponent, FilterComponent]
})
export class MediaPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  isMyMedia = false;
  isGrid = false;
  pending = false;
  error = false;

  handleRefresh = async (event: any) => {
  };

  toggleMediaType() {
    this.isMyMedia = !this.isMyMedia
  }

  changeIsGrid() {
    this.isGrid = !this.isGrid
  }
}
