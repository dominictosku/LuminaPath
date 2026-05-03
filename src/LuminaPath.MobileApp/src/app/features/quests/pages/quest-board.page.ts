import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
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

type QuestType = 'main' | 'sub' | 'faction';
type PageMode = 'quests' | 'skills';
type ModalMode = 'skill' | 'node' | null;

type Quest = {
  id: number;
  title: string;
  completed: boolean;
  completedAt?: string;
};

type QuestColumn = {
  type: QuestType;
  title: string;
  label: string;
  description: string;
  icon: string;
};

type Skill = {
  id: number;
  name: string;
  icon: string;
  color: string;
  xp: number;
  nodes: string[];
  unlockedNodes: number[];
};

type QuestState = {
  xp: number;
  quests: Record<QuestType, Quest[]>;
  skills: Skill[];
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

  skills: Skill[] = [];
  newSkill = this.emptySkillForm();
  newNodeName = '';
  selectedSkillId: number | null = null;

  private readonly storageKey = 'luminapath-rpg-quest-board-v1';
  private readonly xpPerLevel = 200;
  private readonly questRewards: Record<QuestType, number> = {
    main: 150,
    sub: 75,
    faction: 100,
  };
  private toastTimer: number | undefined;

  constructor() {
    addIcons({
      addOutline,
      bookOutline,
      brushOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      closeOutline,
      codeSlashOutline,
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

  ngOnInit() {
    this.restoreState();
    this.rebuildStats();
  }

  addQuest(type: QuestType) {
    const title = this.newQuest[type].trim();

    if (!title) {
      return;
    }

    this.quests[type] = [
      {
        id: Date.now(),
        title,
        completed: false,
      },
      ...this.quests[type],
    ];
    this.newQuest[type] = '';
    this.persist();
  }

  toggleQuest(type: QuestType, quest: Quest) {
    quest.completed = !quest.completed;

    if (quest.completed && !quest.completedAt) {
      quest.completedAt = new Date().toISOString();
      this.xp += this.questRewards[type];
      this.showToast(`+${this.questRewards[type]} XP earned`);
    }

    if (!quest.completed) {
      quest.completedAt = undefined;
    }

    this.persist();
  }

  deleteQuest(type: QuestType, questId: number) {
    this.quests[type] = this.quests[type].filter((quest) => quest.id !== questId);
    this.persist();
  }

  trainSkill(skill: Skill) {
    skill.xp += 40;
    this.xp += 15;
    this.showToast(`${skill.name} training complete`);
    this.persist();
  }

  unlockNode(skill: Skill, nodeIndex: number) {
    if (skill.unlockedNodes.includes(nodeIndex)) {
      return;
    }

    skill.unlockedNodes = [...skill.unlockedNodes, nodeIndex];
    skill.xp += 25;
    this.xp += 25;
    this.showToast(`${skill.nodes[nodeIndex]} unlocked`);
    this.persist();
  }

  openSkillModal() {
    this.newSkill = this.emptySkillForm();
    this.modalMode = 'skill';
  }

  openNodeModal(skill: Skill) {
    this.selectedSkillId = skill.id;
    this.newNodeName = '';
    this.modalMode = 'node';
  }

  closeModal() {
    this.modalMode = null;
    this.selectedSkillId = null;
    this.newNodeName = '';
  }

  addSkill() {
    const name = this.newSkill.name.trim();

    if (!name) {
      return;
    }

    this.skills = [
      ...this.skills,
      {
        id: Date.now(),
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
    this.persist();
  }

  addNode() {
    const skill = this.skills.find((item) => item.id === this.selectedSkillId);
    const nodeName = this.newNodeName.trim();

    if (!skill || !nodeName) {
      return;
    }

    skill.nodes = [...skill.nodes, nodeName];
    this.closeModal();
    this.showToast(`${nodeName} added`);
    this.persist();
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

  skillLevel(skill: Skill): number {
    return Math.floor(skill.xp / 100) + 1;
  }

  skillProgress(skill: Skill): number {
    return (skill.xp % 100) / 100;
  }

  unlockedCount(skill: Skill): number {
    return skill.unlockedNodes.length;
  }

  trackByQuest(_: number, quest: Quest): number {
    return quest.id;
  }

  trackBySkill(_: number, skill: Skill): number {
    return skill.id;
  }

  trackByColumn(_: number, column: QuestColumn): QuestType {
    return column.type;
  }

  trackByText(_: number, item: string): string {
    return item;
  }

  private restoreState() {
    const saved = this.readStorage();

    if (saved) {
      this.xp = saved.xp;
      this.quests = saved.quests;
      this.skills = saved.skills;
      return;
    }

    this.quests = {
      main: [
        { id: 1, title: 'Define the next personal milestone', completed: false },
        { id: 2, title: 'Finish one meaningful project sprint', completed: false },
      ],
      sub: [
        { id: 3, title: 'Clear the desk before starting', completed: false },
        { id: 4, title: 'Plan tomorrow in three bullets', completed: true, completedAt: new Date().toISOString() },
      ],
      faction: [
        { id: 5, title: 'Check in with someone you care about', completed: false },
      ],
    };
    this.skills = [
      {
        id: 11,
        name: 'Programming',
        icon: 'code-slash-outline',
        color: '#2563eb',
        xp: 120,
        nodes: ['Debugging', 'Architecture', 'Shipping'],
        unlockedNodes: [0],
      },
      {
        id: 12,
        name: 'Drawing',
        icon: 'brush-outline',
        color: '#0891b2',
        xp: 60,
        nodes: ['Sketching', 'Color study', 'Finished piece'],
        unlockedNodes: [],
      },
      {
        id: 13,
        name: 'Cooking',
        icon: 'restaurant-outline',
        color: '#0f766e',
        xp: 90,
        nodes: ['Knife basics', 'Meal prep', 'Signature dish'],
        unlockedNodes: [0],
      },
    ];
  }

  private readStorage(): QuestState | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      return raw ? (JSON.parse(raw) as QuestState) : null;
    } catch {
      return null;
    }
  }

  private persist() {
    this.rebuildStats();

    try {
      localStorage.setItem(
        this.storageKey,
        JSON.stringify({
          xp: this.xp,
          quests: this.quests,
          skills: this.skills,
        } satisfies QuestState)
      );
    } catch {
      this.showToast('Progress could not be saved on this device');
    }
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
