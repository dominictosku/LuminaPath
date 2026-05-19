import { Component } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton } from '@ionic/angular/standalone';
import { Credentials } from 'src/app/core/auth/models/user.model';

@Component({
    selector: 'app-user-create',
    templateUrl: './user-create.page.html',
    styleUrls: ['./user-create.page.scss'],
    imports: [IonContent, IonHeader, IonTitle, IonToolbar, IonButton, FormsModule]
})
export class UserCreatePage {
  credentials = new Credentials()
}
