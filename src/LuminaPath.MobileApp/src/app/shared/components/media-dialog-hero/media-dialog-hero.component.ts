import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-media-dialog-hero',
  imports: [IonIcon],
  template: `
    <header>
      <div class="artwork">
        @if (image()) { <img [src]="image()" alt="" /> }
      </div>
      <button type="button" aria-label="Close dialog" [disabled]="busy()" (click)="closed.emit()"><ion-icon name="close-outline" /></button>
      <div class="heading">
        <span class="eyebrow">{{ eyebrow() }}</span>
        <h2>{{ title() }}</h2>
        @if (subtitle()) { <p>{{ subtitle() }}</p> }
      </div>
    </header>`,
  styles: `
    :host { display: block; }
    header { position: relative; }
    .artwork { position: relative; height: 260px; overflow: hidden; background: radial-gradient(ellipse at 70% 0, var(--app-theme-accent-soft), transparent 70%); }
    .artwork img { height: 100%; width: 100%; object-fit: cover; object-position: center 32%; opacity: .65; }
    .artwork::after { content: ''; position: absolute; inset: 0; background: linear-gradient(transparent, var(--collection-surface, #151d2b)); }
    button { position: absolute; right: 22px; top: 22px; display: grid; place-items: center; width: 42px; height: 42px; border: 1px solid #a4b7d34d; border-radius: 50%; color: #edf2fa; background: #0b1425b8; cursor: pointer; }
    button:focus-visible { outline: 2px solid var(--app-theme-accent); outline-offset: 3px; }
    button ion-icon { font-size: 24px; }
    .heading { position: relative; margin-top: -40px; padding: 0 36px 8px; }
    .eyebrow { display: inline-flex; border: 1px solid var(--collection-line, #a4b7d330); border-radius: 30px; padding: 8px 12px; color: var(--collection-text, #edf2fa); background: #ffffff06; font-size: 10px; text-transform: uppercase; letter-spacing: 1.8px; }
    h2 { font-family: Manrope, var(--ion-font-family, sans-serif); font-size: clamp(26px, 3.2vw, 38px); font-weight: 600; letter-spacing: -1.2px; line-height: 1.22; margin: 20px 0 14px; overflow-wrap: anywhere; }
    p { color: var(--collection-muted, #9aa8bb); font-size: 14px; line-height: 1.6; margin: 0; }
    @media (max-width: 600px) { .artwork { height: 200px; } .heading { padding-inline: 22px; } button { right: 16px; top: 16px; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaDialogHeroComponent {
  readonly image = input<string | null>(null);
  readonly title = input.required<string>();
  readonly eyebrow = input.required<string>();
  readonly subtitle = input('');
  readonly busy = input(false);
  readonly closed = output<void>();
}
