import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon } from '@ionic/angular/standalone';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
@Component({
  selector: 'app-game-notes',
  templateUrl: './game-notes.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [FormsModule, IonButton, IonIcon, EmptyStateComponent],
})
export class GameNotesComponent {
  @Input({ required: true }) isInLibrary = false;
  @Input() personalNotes: string | null = null;
  @Input() isSaving = false;
  @Output() readonly save = new EventEmitter<string | null>();

  isEditing = false;
  notesDraft = '';

    get displayedNotes(): string {
    return (this.personalNotes ?? '').trim();
  }

  get hasPersonalNotes(): boolean {
    return this.displayedNotes.length > 0;
  }

  get renderedPersonalNotes(): string {
    return this.renderMarkdown(this.displayedNotes);
  }

  startEditing(): void {
    this.notesDraft = this.displayedNotes;
    this.isEditing = true;
  }

  cancelEditing(): void {
    this.notesDraft = '';
    this.isEditing = false;
  }

  submit(): void {
    if (this.isSaving) return;
    const normalized = this.normalize(this.notesDraft);
    this.save.emit(normalized);
  }

  /** Called by the parent after a successful save to close the editor. */
  resetEditor(): void {
    this.notesDraft = '';
    this.isEditing = false;
  }

  renderMarkdown(markdown: string): string {
    const lines = this.escapeHtml(markdown).split('\n');
    const html: string[] = [];
    let paragraph: string[] = [];
    let listItems: string[] = [];
    let codeLines: string[] | null = null;

    const flushParagraph = () => {
      if (!paragraph.length) return;
      html.push(`<p>${this.renderInline(paragraph.join(' '))}</p>`);
      paragraph = [];
    };

    const flushList = () => {
      if (!listItems.length) return;
      html.push(`<ul>${listItems.map((item) => `<li>${this.renderInline(item)}</li>`).join('')}</ul>`);
      listItems = [];
    };

  for (const line of lines) {
      if (line.trim().startsWith('```')) {
        flushParagraph();
        flushList();
        if (codeLines) {
          html.push(`<pre><code>${codeLines.join('\n')}</code></pre>`);
          codeLines = null;
        } else {
          codeLines = [];
        }
        continue;
      }

      if (codeLines) {
        codeLines.push(line);
        continue;
      }

      const trimmed = line.trim();
      if (!trimmed) {
        flushParagraph();
        flushList();
        continue;
      }

      const heading = trimmed.match(/^(#{1,3})\s+(.+)$/);
      if (heading) {
        flushParagraph();
        flushList();
        const level = heading[1].length + 2;
        html.push(`<h${level}>${this.renderInline(heading[2])}</h${level}>`);
        continue;
      }

      const listItem = trimmed.match(/^[-*]\s+(.+)$/);
      if (listItem) {
        flushParagraph();
        listItems.push(listItem[1]);
        continue;
      }

      flushList();
      paragraph.push(trimmed);
    }

  flushParagraph();
    flushList();
    if (codeLines) {
      html.push(`<pre><code>${codeLines.join('\n')}</code></pre>`);
    }

    return html.join('');
  }

  private renderInline(value: string): string {
    return value
      .replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/(^|[^*])\*([^*\n]+)\*/g, '$1<em>$2</em>');
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private normalize(value: string): string | null {
    const normalized = value.replace(/\r\n/g, '\n').trim();
    return normalized.length ? normalized : null;
  }
}
