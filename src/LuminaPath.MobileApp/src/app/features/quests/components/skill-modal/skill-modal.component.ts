import { Component, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon } from '@ionic/angular/standalone';

export type SkillForm = {
  name: string;
  icon: string;
  color: string;
};

type ModalMode = 'skill' | 'node';
type SkillIconOption = { label: string; icon: string };

/**
 * Modal that handles both "new/edit skill" and "add node to skill" flows.
 * Mode + form state are owned by the parent so the page can target the right
 * skill when the node form is open. The component just emits save/close.
 */
@Component({
  selector: 'app-skill-modal',
  templateUrl: './skill-modal.component.html',
  imports: [FormsModule, IonButton, IonIcon],
})
export class SkillModalComponent {
  readonly mode = input.required<ModalMode>();
  readonly editingSkillId = input<number | null>(null);
  readonly skillForm = model.required<SkillForm>();
  readonly nodeName = model<string>('');
  readonly skillIconOptions = input<SkillIconOption[]>([]);
  readonly skillColorOptions = input<string[]>([]);

  readonly close = output<void>();
  readonly saveSkill = output<void>();
  readonly addNode = output<void>();

  selectIcon(icon: string): void {
    this.skillForm.update((current) => ({ ...current, icon }));
  }

  selectColor(color: string): void {
    this.skillForm.update((current) => ({ ...current, color }));
  }

  updateName(value: string): void {
    this.skillForm.update((current) => ({ ...current, name: value }));
  }
}
