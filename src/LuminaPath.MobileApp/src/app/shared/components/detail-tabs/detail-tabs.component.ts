import { Component, EventEmitter, Input, Output } from '@angular/core';
import { IonLabel, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';

export interface DetailTabOption {
  value: string;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-detail-tabs',
  templateUrl: './detail-tabs.component.html',
  styleUrls: ['./detail-tabs.component.scss'],
  imports: [IonLabel, IonSegment, IonSegmentButton],
})
export class DetailTabsComponent {
  @Input({ required: true }) tabs: readonly DetailTabOption[] = [];
  @Input({ required: true }) value = '';

  @Output() valueChange = new EventEmitter<string>();

  onTabChange(value: unknown): void {
    if (typeof value === 'string') {
      this.valueChange.emit(value);
    }
  }
}
