import { Component, HostBinding, signal } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { initFlowbite } from 'flowbite';
import { NavBarComponent } from './ui/components/navigation/nav-bar/nav-bar.component'

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html',
    imports: [IonApp, IonRouterOutlet, NavBarComponent]
})
export class AppComponent {
  constructor() { }
  title = 'web-app';
  darkMode = signal<boolean>(true);

  @HostBinding('class.dark') get mode() { return this.darkMode(); }

  ngOnInit(): void {
    initFlowbite();
  }
}
