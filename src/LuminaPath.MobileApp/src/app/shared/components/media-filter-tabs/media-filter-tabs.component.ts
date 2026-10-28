import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface MediaFilterTab { id: string; label: string }

/** Presentation only: each page retains its own supported modes and state. */
@Component({
  selector: 'app-media-filter-tabs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="media-tabs" role="group" [attr.aria-label]="label()">
      @for (option of options(); track option.id) {
        <button type="button" [class.active]="selected() === option.id"
          [attr.aria-pressed]="selected() === option.id" (click)="selectedChange.emit(option.id)">
          {{ option.label }}
        </button>
      }
    </div>`,
  styles: `
    :host { display: block; min-width: 0; }
    .media-tabs { display: flex; gap: 5px; flex-wrap: wrap; }
    button { border: 0; background: transparent; color: var(--collection-muted, #9aa8bb); font: inherit; font-size: 13px; padding: 10px 14px; min-height: 40px; border-radius: 6px; cursor: pointer; transition: background .2s, color .2s; }
    button:hover, button.active { color: var(--collection-accent, var(--app-theme-accent)); background: var(--app-theme-accent-soft); }
    button:focus-visible { outline: 2px solid var(--app-theme-accent); outline-offset: 3px; }
    @media (max-width: 600px) { button { padding-inline: 11px; } }
  `,
})
export class MediaFilterTabsComponent {
  readonly options = input.required<readonly MediaFilterTab[]>();
  readonly selected = input.required<string>();
  readonly label = input('Media type');
  readonly selectedChange = output<string>();
}
