import { Component } from '@angular/core';
import { IonProgressBar, IonSkeletonText } from "@ionic/angular/standalone";

@Component({
    selector: 'app-loading',
    templateUrl: './loading.component.html',
    styleUrls: ['./loading.component.scss'],
    imports: [IonSkeletonText, IonProgressBar]
})
export class LoadingComponent {}
