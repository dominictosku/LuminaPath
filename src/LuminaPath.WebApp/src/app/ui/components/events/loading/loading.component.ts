import { Component, OnInit } from '@angular/core';
import { IonProgressBar, IonSkeletonText } from "@ionic/angular/standalone";

@Component({
  selector: 'app-loading',
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.scss'],
  imports: [IonSkeletonText, IonProgressBar],
  standalone: true,
})
export class LoadingComponent implements OnInit {

  constructor() { }

  ngOnInit() { }

}
