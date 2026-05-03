import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AlertController,
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonProgressBar,
  IonSegment,
  IonSegmentButton,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  bookOutline,
  brushOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  closeOutline,
  codeSlashOutline,
  createOutline,
  flagOutline,
  flashOutline,
  libraryOutline,
  lockClosedOutline,
  mapOutline,
  restaurantOutline,
  schoolOutline,
  shieldCheckmarkOutline,
  sparkles,
  sparklesOutline,
  starOutline,
  trashOutline,
  trophyOutline,
} from 'ionicons/icons';
import { Quest, QuestBoardService, QuestBoardState, QuestSkill, QuestType } from '../services/quest-board.service';

type PageMode = 'quests' | 'skills';
type ModalMode = 'skill' | 'node' | null;

type QuestColumn = {
  type: QuestType;
  title: string;
  label: string;
  description: string;
  icon: string;
};

@Component({
  selector: 'app-quest-board',
  templateUrl: './quest-board.page.html',
  styleUrls: ['./quest-board.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonProgressBar,
    IonSegment,
    IonSegmentButton,
    IonTitle,
    IonToolbar,
  ],
})
export class QuestBoardPage implements OnInit {
  readonly questColumns: QuestColumn[] = [
    {
      type: 'main',
      title: 'Main Quests',
      label: 'Big arcs',
      description: 'Important life missions that deserve focus.',
      icon: 'map-outline',
    },
    {
      type: 'sub',
      title: 'Sub Quests',
      label: 'Small wins',
      description: 'Useful tasks that keep the day moving.',
      icon: 'flag-outline',
    },
    {
      type: 'faction',
      title: 'Faction Quests',
      label: 'People and teams',
      description: 'Social, family, work, or community commitments.',
      icon: 'shield-checkmark-outline',
    },
  ];

  readonly skillIconOptions = [
    { label: 'Code', icon: 'code-slash-outline' },
    { label: 'Art', icon: 'brush-outline' },
    { label: 'Cook', icon: 'restaurant-outline' },
    { label: 'Study', icon: 'book-outline' },
    { label: 'Craft', icon: 'school-outline' },
  ];

  readonly skillColorOptions = ['#2563eb', '#0891b2', '#0f766e', '#7c3aed', '#be123c'];

  mode: PageMode = 'quests';
  modalMode: ModalMode = null;
  xp = 0;
  level = 1;
  title = 'Initiate';
  xpIntoLevel = 0;
  xpProgress = 0;
  completedQuestCount = 0;
  activeQuestCount = 0;
  unlockedNodeCount = 0;
  toastMessage = '';
  isLoading = true;
  errorMessage = '';

  quests: Record<QuestType, Quest[]> = {
    main: [],
    sub: [],
    faction: [],
  };

  newQuest: Record<QuestType, string> = {
    main: '',
    sub: '',
    faction: '',
  };

  skills: QuestSkill[] = [];
  newSkill = this.emptySkillForm();
  newNodeName = '';
  selectedSkillId: number | null = null;
  editingSkillId: number | null = null;

  private readonly xpPerLevel = 200;
  private readonly questRewards: Record<QuestType, number> = {
    main: 150,
    sub: 75,
    faction: 100,
  };
  private toastTimer: number | undefined;

  constructor(
    private questBoardService: QuestBoardService,
    private alertController: AlertController
  ) {
    addIcons({
      addOutline,
      bookOutline,
      brushOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      closeOutline,
      codeSlashOutline,
      createOutline,
      flagOutline,
      flashOutline,
      libraryOutline,
      lockClosedOutline,
      mapOutline,
      restaurantOutline,
      schoolOutline,
      shieldCheckmarkOutline,
      sparkles,
      sparklesOutline,
      starOutline,
      trashOutline,
      trophyOutline,
    });
  }

  async ngOnInit() {
    await this.loadBoard();
  }

  async addQuest(type: QuestType) {
    const title = this.newQuest[type].trim();

    if (!title) {
      return;
    }

    this.quests[type] = [
      {
        id: this.nextTemporaryId(),
        title,
        completed: false,
        createdAt: new Date().toISOString(),
        rewardXp: this.questRewards[type],
      },
      ...this.quests[type],
    ];
    this.newQuest[type] = '';
    await this.persist();
  }

  async toggleQuest(type: QuestType, quest: Quest) {
    quest.completed = !quest.completed;

    if (quest.completed && !quest.completedAt) {
      quest.completedAt = new Date().toISOString();
      this.xp += this.questRewards[type];
      this.showToast(`+${this.questRewards[type]} XP earned`);
    }

    if (!quest.completed) {
      quest.completedAt = undefined;
    }

    await this.persist();
  }

  async deleteQuest(type: QuestType, questId: number) {
    const quest = this.quests[type].find((item) => item.id === questId);
    const confirmed = await this.confirmDelete(
      'Delete quest',
      quest ? `Delete "${quest.title}"? This cannot be undone.` : 'Delete this quest? This cannot be undone.'
    );

    if (!confirmed) {
      return;
    }

    const previousBoard = this.currentBoardSnapshot();
    this.quests[type] = this.quests[type].filter((quest) => quest.id !== questId);
    const saved = await this.persist();

    if (!saved) {
      this.applyBoard(previousBoard);
    }
  }

  async trainSkill(skill: QuestSkill) {
    skill.xp += 40;
    this.xp += 15;
    this.showToast(`${skill.name} training complete`);
    await this.persist();
  }

  async unlockNode(skill: QuestSkill, nodeIndex: number) {
    if (skill.unlockedNodes.includes(nodeIndex)) {
      return;
    }

    skill.unlockedNodes = [...skill.unlockedNodes, nodeIndex];
    skill.xp += 25;
    this.xp += 25;
    this.showToast(`${skill.nodes[nodeIndex]} unlocked`);
    await this.persist();
  }

  openSkillModal() {
    this.newSkill = this.emptySkillForm();
    this.editingSkillId = null;
    this.modalMode = 'skill';
  }

  openEditSkillModal(skill: QuestSkill) {
    this.newSkill = {
      name: skill.name,
      icon: skill.icon,
      color: skill.color,
    };
    this.editingSkillId = skill.id;
    this.modalMode = 'skill';
  }

  openNodeModal(skill: QuestSkill) {
    this.selectedSkillId = skill.id;
    this.newNodeName = '';
    this.modalMode = 'node';
  }

  closeModal() {
    this.modalMode = null;
    this.selectedSkillId = null;
    this.editingSkillId = null;
    this.newNodeName = '';
  }

  async saveSkill() {
    const name = this.newSkill.name.trim();

    if (!name) {
      return;
    }

    if (this.editingSkillId !== null) {
      const skill = this.skills.find((item) => item.id === this.editingSkillId);

      if (!skill) {
        return;
      }

      skill.name = name;
      skill.icon = this.newSkill.icon;
      skill.color = this.newSkill.color;
      this.closeModal();
      this.showToast(`${name} updated`);
      await this.persist();
      return;
    }

    this.skills = [
      ...this.skills,
      {
        id: this.nextTemporaryId(),
        name,
        icon: this.newSkill.icon,
        color: this.newSkill.color,
        xp: 0,
        nodes: ['First practice', 'Weekly streak', 'Personal project'],
        unlockedNodes: [],
      },
    ];
    this.closeModal();
    this.showToast(`${name} added to your skill tree`);
    await this.persist();
  }

  async deleteSkill(skillId: number) {
    const skill = this.skills.find((item) => item.id === skillId);
    const confirmed = await this.confirmDelete(
      'Delete skill',
      skill ? `Delete "${skill.name}" and all of its nodes? This cannot be undone.` : 'Delete this skill? This cannot be undone.'
    );

    if (!confirmed) {
      return;
    }

    const previousBoard = this.currentBoardSnapshot();
    this.skills = this.skills.filter((item) => item.id !== skillId);
    this.showToast(skill ? `${skill.name} deleted` : 'Skill deleted');
    const saved = await this.persist();

    if (!saved) {
      this.applyBoard(previousBoard);
    }
  }

  async addNode() {
    const skill = this.skills.find((item) => item.id === this.selectedSkillId);
    const nodeName = this.newNodeName.trim();

    if (!skill || !nodeName) {
      return;
    }

    skill.nodes = [...skill.nodes, nodeName];
    this.closeModal();
    this.showToast(`${nodeName} added`);
    await this.persist();
  }

  questProgress(column: QuestColumn): number {
    const quests = this.quests[column.type];

    if (quests.length === 0) {
      return 0;
    }

    return quests.filter((quest) => quest.completed).length / quests.length;
  }

  questReward(type: QuestType): number {
    return this.questRewards[type];
  }

  skillLevel(skill: QuestSkill): number {
    return Math.floor(skill.xp / 100) + 1;
  }

  skillProgress(skill: QuestSkill): number {
    return (skill.xp % 100) / 100;
  }

  unlockedCount(skill: QuestSkill): number {
    return skill.unlockedNodes.length;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackBySkill(_: number, skill: QuestSkill): number {
    return skill.id;
  }

  trackByColumn(_: number, column: QuestColumn): QuestType {
    return column.type;
  }

  trackByText(_: number, item: string): string {
    return item;
  }

  private async loadBoard() {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.applyBoard(await this.questBoardService.getBoard());
    } catch {
      this.errorMessage = 'Quest board could not be loaded.';
      this.showToast(this.errorMessage);
      this.rebuildStats();
    } finally {
      this.isLoading = false;
    }
  }

  private async persist(): Promise<boolean> {
    this.rebuildStats();

    try {
      this.applyBoard(await this.questBoardService.saveBoard(this.currentBoard()));
      return true;
    } catch {
      this.showToast('Progress could not be saved');
      return false;
    }
  }

  private applyBoard(board: QuestBoardState) {
    this.xp = board.xp;
    this.quests = board.quests;
    this.skills = board.skills;
    this.rebuildStats();
  }

  private currentBoard(): QuestBoardState {
    return {
      xp: this.xp,
      quests: this.quests,
      skills: this.skills,
    };
  }

  private currentBoardSnapshot(): QuestBoardState {
    return {
      xp: this.xp,
      quests: {
        main: this.quests.main.map((quest) => ({ ...quest })),
        sub: this.quests.sub.map((quest) => ({ ...quest })),
        faction: this.quests.faction.map((quest) => ({ ...quest })),
      },
      skills: this.skills.map((skill) => ({
        ...skill,
        nodes: [...skill.nodes],
        unlockedNodes: [...skill.unlockedNodes],
      })),
    };
  }

  private async confirmDelete(header: string, message: string): Promise<boolean> {
    const alert = await this.alertController.create({
      header,
      message,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Delete',
          role: 'destructive',
        },
      ],
    });

    await alert.present();
    const result = await alert.onDidDismiss();
    return result.role === 'destructive';
  }

  private rebuildStats() {
    const allQuests = [...this.quests.main, ...this.quests.sub, ...this.quests.faction];
    this.level = Math.floor(this.xp / this.xpPerLevel) + 1;
    this.xpIntoLevel = this.xp % this.xpPerLevel;
    this.xpProgress = this.xpIntoLevel / this.xpPerLevel;
    this.title = this.titleForLevel(this.level);
    this.completedQuestCount = allQuests.filter((quest) => quest.completed).length;
    this.activeQuestCount = allQuests.filter((quest) => !quest.completed).length;
    this.unlockedNodeCount = this.skills.reduce((sum, skill) => sum + skill.unlockedNodes.length, 0);
  }

  private titleForLevel(level: number): string {
    if (level >= 15) {
      return 'Legend';
    }

    if (level >= 10) {
      return 'Master';
    }

    if (level >= 6) {
      return 'Adept';
    }

    if (level >= 3) {
      return 'Apprentice';
    }

    return 'Initiate';
  }

  private emptySkillForm() {
    return {
      name: '',
      icon: 'code-slash-outline',
      color: '#2563eb',
    };
  }

  private nextTemporaryId(): number {
    return -Math.floor(Math.random() * 1_000_000_000);
  }

  private showToast(message: string) {
    this.toastMessage = message;

    if (this.toastTimer) {
      window.clearTimeout(this.toastTimer);
    }

    this.toastTimer = window.setTimeout(() => {
      this.toastMessage = '';
    }, 2600);
  }
}
