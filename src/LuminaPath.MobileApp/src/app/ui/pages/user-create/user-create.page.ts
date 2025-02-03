import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton } from '@ionic/angular/standalone';
import { Credentials } from 'src/app/core/models/user';

@Component({
    selector: 'app-user-create',
    templateUrl: './user-create.page.html',
    styleUrls: ['./user-create.page.scss'],
    imports: [IonContent, IonHeader, IonTitle, IonToolbar, IonButton, CommonModule, FormsModule]
})
export class UserCreatePage implements OnInit {

  constructor() {
    this.credentials.email = "admin@example.com"
    this.credentials.password = "Admin123*"
  }

  ngOnInit() { }
  credentials = new Credentials()

}
