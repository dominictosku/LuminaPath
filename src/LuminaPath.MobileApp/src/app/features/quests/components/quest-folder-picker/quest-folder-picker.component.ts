import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  input,
  output,
} from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';
import { QuestFolder } from '../../services/quest-board.service';

/**
 * Floating folder picker — small list-style popover anchored by its
 * positioned parent (the host wraps a `position: relative` container in
 * each card / row). Lists every folder with its emoji + name, plus a
 * "Remove from folder" entry when the current quest is assigned.
 *
 * Why a hand-rolled popover instead of `ion-popover`:
 *   - `ion-popover` positions globally (portal-attached to <body>), which
 *     loses the per-row anchor we want here.
 *   - This list is short and visual — a 280px-wide grid pinned to its
 *     button is exactly what TickTick/Things do for the same action.
 *   - Click-outside dismissal + ESC are tiny additions; no library
 *     needed.
 */
@Component({
  selector: 'app-quest-folder-picker',
  templateUrl: './quest-folder-picker.component.html',
  styleUrls: ['./quest-folder-picker.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestFolderPickerComponent {
  private hostRef = inject(ElementRef<HTMLElement>);

  readonly folders = input.required<readonly QuestFolder[]>();
  readonly currentFolderId = input<number | null>(null);

  /** Emits the selected folder id, or `null` to clear assignment. */
  readonly select = output<number | null>();
  /** Emits when the user clicks outside / presses ESC. */
  readonly close = output<void>();

  /**
   * Window-level click swallowed when it lands outside this component.
   * Bound on `window` so it fires regardless of where the click started.
   * Parent components stop propagation on the trigger button so opening
   * the picker doesn't immediately close it again.
   */
  @HostListener('window:mousedown', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (target && this.hostRef.nativeElement.contains(target)) {
      return;
    }
    this.close.emit();
  }

  @HostListener('window:keydown.escape')
  protected onEscape(): void {
    this.close.emit();
  }

  protected isCurrent(folderId: number | null): boolean {
    return (this.currentFolderId() ?? null) === folderId;
  }

  protected pick(folderId: number | null): void {
    if (this.isCurrent(folderId)) {
      // Picking the already-selected folder = no-op; just close.
      this.close.emit();
      return;
    }
    this.select.emit(folderId);
  }

  protected trackByFolder(_: number, folder: QuestFolder): number {
    return folder.id;
  }
}
