import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-collection-section',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section [attr.aria-label]="heading()">
      <header><h2>{{ heading() }}</h2><div><ng-content select="[section-actions]" /></div></header>
      <ng-content />
    </section>`,
  styles: `
    :host { display: block; min-width: 0; }
    header { display: flex; justify-content: space-between; align-items: center; gap: 16px; margin-bottom: 20px; }
    h2 { margin: 0; color: var(--collection-muted, #9aa8bb); font-size: 11px; font-weight: 600; letter-spacing: 2px; text-transform: uppercase; line-height: 1.6; }
    header > div { display: flex; align-items: center; gap: 10px; }
    @media (max-width: 600px) { header { flex-wrap: wrap; gap: 10px; } }
  `,
})
export class CollectionSectionComponent {
  readonly heading = input.required<string>();
}
