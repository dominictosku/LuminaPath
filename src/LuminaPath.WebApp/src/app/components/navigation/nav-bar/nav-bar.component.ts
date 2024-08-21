import { Component, OnInit } from '@angular/core';
import { IonButton, IonIcon, IonHeader } from '@ionic/angular/standalone';

@Component({
  standalone: true,
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrls: ['./nav-bar.component.scss'],
  imports: [IonButton, IonIcon, IonHeader]
})
export class NavBarComponent implements OnInit {

  constructor() { }

  ngOnInit() { }

  logout() { }
}
