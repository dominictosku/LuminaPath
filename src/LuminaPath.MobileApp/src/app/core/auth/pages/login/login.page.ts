import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonButton } from '@ionic/angular/standalone';
import { Credentials } from 'src/app/core/auth/models/user.model';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Component({
    selector: 'app-login',
    templateUrl: './login.page.html',
    styleUrls: ['./login.page.scss'],
    imports: [IonContent, IonHeader, IonTitle, IonButton, IonToolbar, CommonModule, FormsModule]
})
export class LoginPage implements OnInit {
  credentials = new Credentials()

  constructor(private authService: AuthService, private route: Router) {
    this.credentials.email = "admin@example.com"
    this.credentials.password = "Admin123*"
  }

  ngOnInit() { }

  async login(){
    await firstValueFrom(this.authService.login(this.credentials));
    this.route.navigate(['/'])
  }
}
