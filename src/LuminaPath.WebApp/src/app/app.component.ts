import { Component } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { initFlowbite } from 'flowbite';
import { NavBarComponent } from '../app/components/navigation/nav-bar/nav-bar.component'

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  standalone: true,
  imports: [IonApp, IonRouterOutlet, NavBarComponent],
})
export class AppComponent {
  constructor() { }
  title = 'web-app';

  ngOnInit(): void {
    initFlowbite();
  }
}
