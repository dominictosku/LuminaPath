import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, IonRefresherContent } from '@ionic/angular/standalone';
import { TableComponent } from "../../components/media/table/table.component";
import { GridComponent } from "../../components/media/grid/grid.component";
import { PaginationComponent } from "../../components/tools/pagination/pagination.component";

@Component({
  selector: 'app-media',
  templateUrl: './media.page.html',
  styleUrls: ['./media.page.scss'],
  standalone: true,
  imports: [IonRefresherContent, IonContent, IonHeader, IonTitle, IonToolbar, IonButton, IonRefresher, CommonModule, FormsModule, TableComponent, GridComponent, PaginationComponent]
})
export class MediaPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

  refresh = false;
  toggle = async () => {
    this.refresh = !this.refresh
  }

  isGrid = true

  handleRefresh = async (event: any) => {
  };


  changeIsGrid() {
    this.isGrid = !this.isGrid
  }
}
