import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonIcon } from '@ionic/angular/standalone';
import { QuestFolder, QuestFolderCreate, QuestFolderUpdate } from '../../services/quest-board.service';

/**
 * Create/edit dialog for a quest folder. Self-contained:
 * - parent passes `mode` + optional `folder` (for edit)
 * - emits `submitCreate` / `submitUpdate` / `delete` / `close`
 *
 * Emoji input is a plain text field — users paste any Unicode glyph from
 * their OS emoji picker (Win+. / Ctrl+Cmd+Space / GBoard's emoji key).
 * No emoji-picker library required, no curated list to maintain.
 */
@Component({
  selector: 'app-quest-folder-modal',
  templateUrl: './quest-folder-modal.component.html',
  styleUrls: ['./quest-folder-modal.component.scss'],
  imports: [FormsModule, IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestFolderModalComponent {
  readonly mode = input.required<'create' | 'edit'>();
  /** Required for `edit` mode; ignored for `create`. */
  readonly folder = input<QuestFolder | null>(null);
  /** Section-name suggestions: previously used section names across all folders. */
  readonly sectionSuggestions = input<readonly string[]>([]);

  readonly close = output<void>();
  readonly submitCreate = output<QuestFolderCreate>();
  readonly submitUpdate = output<{ id: number; patch: QuestFolderUpdate }>();
  readonly delete = output<QuestFolder>();

  // Local form state — initialised from `folder` input when present.
  protected readonly name = signal('');
  protected readonly emoji = signal('');
  protected readonly color = signal('');
  protected readonly sectionName = signal('');
  /** Track if the user has interacted with the form yet (for hydration). */
  private hydrated = false;

  protected readonly isEdit = computed(() => this.mode() === 'edit');
  protected readonly title = computed(() => (this.isEdit() ? 'Edit folder' : 'New folder'));
  protected readonly canSubmit = computed(() => {
    const name = this.name().trim();
    const emoji = this.emoji().trim();
    return name.length > 0 && emoji.length > 0;
  });

  ngOnInit(): void {
    // Hydrate from the folder input on first render of the edit case.
    if (this.hydrated) return;
    const seed = this.folder();
    if (seed) {
      this.name.set(seed.name);
      this.emoji.set(seed.emoji);
      this.color.set(seed.color ?? '');
      this.sectionName.set(seed.sectionName ?? '');
    }
    this.hydrated = true;
  }

  protected onClose(): void {
    this.close.emit();
  }

  protected onSubmit(event: SubmitEvent): void {
    event.preventDefault();
    if (!this.canSubmit()) return;

    const name = this.name().trim();
    const emoji = this.emoji().trim();
    const color = this.color().trim() || null;
    const sectionName = this.sectionName().trim() || null;

    if (this.isEdit()) {
      const folder = this.folder();
      if (!folder) return;
      this.submitUpdate.emit({
        id: folder.id,
        patch: {
          name,
          emoji,
          color: color ?? undefined,
          clearColor: color === null,
          sectionName: sectionName ?? undefined,
          clearSectionName: sectionName === null,
        },
      });
    } else {
      this.submitCreate.emit({ name, emoji, color, sectionName });
    }
  }

  protected onDelete(): void {
    const folder = this.folder();
    if (!folder) return;
    const ok = window.confirm(
      `Delete "${folder.emoji} ${folder.name}"? Quests inside will stay but lose their folder.`,
    );
    if (ok) this.delete.emit(folder);
  }

  protected pickSuggestion(name: string): void {
    this.sectionName.set(name);
  }
}
