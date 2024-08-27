import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, IonRefresherContent } from '@ionic/angular/standalone';
import { TableComponent } from "../../components/media/table/table.component";
import { GridComponent } from "../../components/media/grid/grid.component";
import { PaginationComponent } from "../../components/tools/pagination/pagination.component";
import { ErrorComponent } from "../../components/events/error/error.component";
import { LoadingComponent } from "../../components/events/loading/loading.component";
import { SelectorComponent } from "../../components/tools/selector/selector.component";
import { FilterComponent } from "../../components/media/filter/filter.component";

@Component({
  selector: 'app-media',
  templateUrl: './media.page.html',
  styleUrls: ['./media.page.scss'],
  standalone: true,
  imports: [IonRefresherContent, IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, CommonModule, FormsModule, TableComponent, GridComponent, PaginationComponent, ErrorComponent, LoadingComponent, SelectorComponent, FilterComponent]
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
