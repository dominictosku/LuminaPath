import { Component, inject } from '@angular/core';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { SkillTreeComponent } from 'src/app/features/skill-tree/components/skill-tree.component';
import { QuestSkill } from '../../services/quest-board.service';
import { ModalMode, SKILL_COLOR_OPTIONS, SKILL_ICON_OPTIONS } from '../../models/quest-board-view.model';
import { QuestBoardStore } from '../../state/quest-board.store';
import { SkillsListComponent } from '../../components/skills-list/skills-list.component';
import { SkillForm, SkillModalComponent } from '../../components/skill-modal/skill-modal.component';

/**
 * "Skills" and "Skill Tree" modes: the skill list with inline train/unlock,
 * the deferred 3D tree, and the add/edit skill (and node) modal. Modal form
 * state is local; skill persistence flows through {@link QuestBoardStore}.
 */
@Component({
  selector: 'app-quest-skills-view',
  templateUrl: './quest-skills-view.component.html',
  imports: [EmptyStateComponent, SkillTreeComponent, SkillsListComponent, SkillModalComponent],
})
export class QuestSkillsViewComponent {
  protected readonly store = inject(QuestBoardStore);

  protected readonly skillIconOptions = [...SKILL_ICON_OPTIONS];
  protected readonly skillColorOptions = [...SKILL_COLOR_OPTIONS];

  protected modalMode: ModalMode = null;
  protected newSkill: SkillForm = this.emptySkillForm();
  protected newNodeName = '';
  protected editingSkillId: number | null = null;
  protected selectedSkillId: number | null = null;

  protected readonly activeLinkedQuestCountFn = (skill: QuestSkill) => this.store.activeLinkedQuestCount(skill);
  protected readonly linkedQuestCountFn = (skill: QuestSkill) => this.store.linkedQuestCount(skill);

  openSkillModal(): void {
    this.newSkill = this.emptySkillForm();
    this.editingSkillId = null;
    this.modalMode = 'skill';
  }

  openEditSkillModal(skill: QuestSkill): void {
    this.newSkill = { name: skill.name, icon: skill.icon, color: skill.color };
    this.editingSkillId = skill.id;
    this.modalMode = 'skill';
  }

  openNodeModal(skill: QuestSkill): void {
    this.selectedSkillId = skill.id;
    this.newNodeName = '';
    this.modalMode = 'node';
  }

  closeModal(): void {
    this.modalMode = null;
    this.selectedSkillId = null;
    this.editingSkillId = null;
    this.newNodeName = '';
  }

  saveSkill(): void {
    if (!this.newSkill.name.trim()) return;
    void this.store.saveSkill(this.newSkill, this.editingSkillId);
    this.closeModal();
  }

  addNode(): void {
    if (this.selectedSkillId == null || !this.newNodeName.trim()) return;
    void this.store.addNode(this.selectedSkillId, this.newNodeName);
    this.closeModal();
  }

  private emptySkillForm(): SkillForm {
    return { name: '', icon: 'code-slash-outline', color: '#2563eb' };
  }
}
