import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { gridOutline, listOutline, add } from 'ionicons/icons';
import { addIcons } from 'ionicons';
import { IonIcon, IonFab, IonFabButton } from "@ionic/angular/standalone";
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-filter',
  templateUrl: './filter.component.html',
  styleUrls: ['./filter.component.scss'],
  imports: [IonFabButton, IonFab, IonIcon, FormsModule],
  standalone: true,
})
export class FilterComponent implements OnInit {

  constructor() {
    addIcons({ gridOutline, listOutline, add });
  }

  @Output()
  toggleGrid = new EventEmitter();

  searchString = ""

  modalProps = { form: "media" }

  ngOnInit() { }

  filterMedia() {

  }

  openModal() {

  }

  changeView(evt: HTMLElement) {
    const tablinks: any = document.getElementsByClassName("tablinks");

    for (let i = 0; i < tablinks.length; i++) {
      tablinks[i].className = tablinks[i].className.replace(" active", "");
    }
    evt.className += " active";
    this.toggleGrid.emit();
  }
}
