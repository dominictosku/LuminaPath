import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
    selector: 'app-planing',
    templateUrl: './planing.page.html',
    styleUrls: ['./planing.page.scss'],
    imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class PlaningPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
