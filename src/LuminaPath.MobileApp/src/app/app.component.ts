import { Component, HostBinding, signal } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { initFlowbite } from 'flowbite';
import { NavBarComponent } from './shared/components/navigation/nav-bar/nav-bar.component'
import { AiChatComponent } from './shared/components/ai-chat/ai-chat.component';
import { ReleaseNotificationService } from './shared/services/release-notification.service';

@Component({
    selector: 'app-root',
    templateUrl: 'app.component.html',
    imports: [IonApp, IonRouterOutlet, NavBarComponent, AiChatComponent]
})
export class AppComponent {
  constructor(private releaseNotifications: ReleaseNotificationService) { }
  title = 'web-app';
  darkMode = signal<boolean>(true);

  @HostBinding('class.dark') get mode() { return this.darkMode(); }

  ngOnInit(): void {
    initFlowbite();
    this.releaseNotifications.init();
  }
}
