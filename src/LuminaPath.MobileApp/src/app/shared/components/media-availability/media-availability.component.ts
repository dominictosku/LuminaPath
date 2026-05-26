import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';
import {
  AvailabilityMediaKind,
  availabilityEyebrow,
  availabilityHeading,
  availabilityIcon,
  availabilityLead,
  buildAvailabilityLinks,
} from './media-availability.links';

@Component({
  selector: 'app-media-availability',
  templateUrl: './media-availability.component.html',
  styleUrls: ['./media-availability.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaAvailabilityComponent {
  readonly kind = input.required<AvailabilityMediaKind>();
  readonly title = input.required<string>();
  readonly releaseDate = input<Date | string | null | undefined>(null);

  readonly heading = computed(() => availabilityHeading(this.kind()));
  readonly eyebrow = computed(() => availabilityEyebrow(this.kind()));
  readonly lead = computed(() => availabilityLead(this.kind()));
  readonly icon = computed(() => availabilityIcon(this.kind()));
  readonly links = computed(() =>
    buildAvailabilityLinks({
      kind: this.kind(),
      title: this.title(),
      releaseDate: this.releaseDate(),
    }),
  );
}
