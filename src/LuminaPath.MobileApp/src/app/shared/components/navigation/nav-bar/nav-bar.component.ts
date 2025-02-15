import { Component, OnInit } from '@angular/core';
import { IonButton, IonIcon, IonHeader } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { logoAmplify } from 'ionicons/icons';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.scss'],
    imports: [IonButton, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit {

  constructor() {
    addIcons({ logoAmplify });
  }

  ngOnInit() { }

  logout() { }
}
