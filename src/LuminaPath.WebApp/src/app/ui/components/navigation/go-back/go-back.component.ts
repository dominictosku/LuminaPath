import { Component, OnInit } from '@angular/core';
import { IonHeader, IonToolbar, IonButtons, IonBackButton, IonTitle } from "@ionic/angular/standalone";

@Component({
  selector: 'app-go-back',
  templateUrl: './go-back.component.html',
  styleUrls: ['./go-back.component.scss'],
  imports: [IonHeader, IonToolbar, IonButtons, IonBackButton, IonTitle],
  standalone: true,
})
export class GoBackComponent implements OnInit {

  constructor() { }

  ngOnInit() { }

}
