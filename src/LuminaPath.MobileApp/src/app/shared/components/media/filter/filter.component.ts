import { Component, EventEmitter, Output } from '@angular/core';
import { gridOutline, listOutline, add } from 'ionicons/icons';
import { addIcons } from 'ionicons';
import { IonIcon, IonFab, IonFabButton } from "@ionic/angular/standalone";
import { FormsModule } from '@angular/forms';

type MediaViewMode = 'grid' | 'list';

@Component({
    selector: 'app-filter',
    templateUrl: './filter.component.html',
    styleUrls: ['./filter.component.scss'],
    imports: [IonFabButton, IonFab, IonIcon, FormsModule]
})
export class FilterComponent {

  constructor() {
    addIcons({ gridOutline, listOutline, add });
  }

  @Output()
  toggleGrid = new EventEmitter<MediaViewMode>();

  searchString = ""

  modalProps = { form: "media" }

  viewMode: MediaViewMode = 'grid';

  filterMedia() {

  }

  openModal() {

  }

  changeView(mode: MediaViewMode) {
    this.viewMode = mode;
    this.toggleGrid.emit(mode);
  }
}
