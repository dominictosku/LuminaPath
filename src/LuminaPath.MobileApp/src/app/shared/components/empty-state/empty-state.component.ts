import { Component, input } from '@angular/core';
import { IonIcon, IonSpinner } from '@ionic/angular/standalone';

/**
 * Shared dashed-border empty-state box used across feature pages.
 * Layout: icon (or spinner if `loading`) + `<strong>title</strong>` + optional message + projected extras.
 *
 * Variants are applied as CSS modifier classes (`empty-state--<variant>`) so feature
 * SCSS can still target them, but the base styles live with this component.
 */
@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss'],
  imports: [IonIcon, IonSpinner],
})
export class EmptyStateComponent {
  readonly icon = input<string | null>(null);
  readonly title = input.required<string>();
  readonly message = input<string | null>(null);
  readonly variant = input<'default' | 'locked' | 'notes' | 'quest-starter'>('default');
  readonly loading = input<boolean>(false);
}
