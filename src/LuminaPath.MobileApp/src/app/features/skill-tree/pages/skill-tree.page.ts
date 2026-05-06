import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, NgZone, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonContent, IonIcon } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  arrowBackOutline,
  checkmarkCircle,
  checkmarkCircleOutline,
  closeOutline,
  lockClosedOutline,
  musicalNotesOutline,
  refreshOutline,
  sparkles,
  sparklesOutline,
  starOutline,
  volumeMuteOutline,
} from 'ionicons/icons';
import {
  SkillTreeBranch,
  SkillTreeNode,
  SkillTreeNodeStatus,
  SkillTreePickedNode,
  SkillTreeQuest,
  SkillTreeState,
} from '../models/skill-tree.model';
import { SkillTreeService } from '../services/skill-tree.service';
import { SkillTreeAudioService } from '../services/skill-tree-audio.service';
import { SkillTreeScene } from '../services/skill-tree-scene';

const XP_PER_LEVEL = 200;

@Component({
  selector: 'app-skill-tree',
  templateUrl: './skill-tree.page.html',
  styleUrls: ['./skill-tree.page.scss'],
  imports: [CommonModule, FormsModule, IonContent, IonIcon],
})
export class SkillTreePage implements OnInit, OnDestroy {
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  branches: SkillTreeBranch[] = [];
  state: SkillTreeState = { unlockedNodeIds: [], quests: [] };
  branchIdx = 0;
  loading = true;
  muted = false;
  toast: { kind: 'unlock' | 'xp'; title: string; sub?: string } | null = null;

  hoveredNode: SkillTreePickedNode | null = null;
  selectedNode: SkillTreePickedNode | null = null;

  newQuestText = '';
  newQuestBranchId = '';

  totalXp = 0;
  level = 1;
  xpInLevel = 0;

  private scene: SkillTreeScene | null = null;
  private toastTimer: number | undefined;
  private keyHandler = (e: KeyboardEvent) => this.onKey(e);
  private dragStartX: number | null = null;
  private dragHandlerDown = (e: MouseEvent) => this.onDragDown(e);
  private dragHandlerMove = (e: MouseEvent) => this.onDragMove(e);
  private dragHandlerUp = () => this.onDragUp();

  constructor(
    private skillTree: SkillTreeService,
    private audio: SkillTreeAudioService,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
  ) {
    addIcons({
      arrowBackOutline,
      checkmarkCircle,
      checkmarkCircleOutline,
      closeOutline,
      lockClosedOutline,
      musicalNotesOutline,
      refreshOutline,
      sparkles,
      sparklesOutline,
      starOutline,
      volumeMuteOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    this.branches = this.skillTree.branches;
    this.state = this.skillTree.load();
    this.newQuestBranchId = this.branches[0]?.id ?? '';
    this.recomputeXp();

    this.zone.runOutsideAngular(() => this.startScene());

    window.addEventListener('keydown', this.keyHandler);
    const canvas = this.canvasRef.nativeElement;
    canvas.addEventListener('mousedown', this.dragHandlerDown);
    window.addEventListener('mousemove', this.dragHandlerMove);
    window.addEventListener('mouseup', this.dragHandlerUp);

    setTimeout(() => {
      this.loading = false;
      this.cdr.detectChanges();
    }, 600);
  }

  ngOnDestroy(): void {
    window.removeEventListener('keydown', this.keyHandler);
    const canvas = this.canvasRef?.nativeElement;
    if (canvas) canvas.removeEventListener('mousedown', this.dragHandlerDown);
    window.removeEventListener('mousemove', this.dragHandlerMove);
    window.removeEventListener('mouseup', this.dragHandlerUp);
    if (this.toastTimer) window.clearTimeout(this.toastTimer);
    this.scene?.destroy();
  }

  get currentBranch(): SkillTreeBranch {
    return this.branches[this.branchIdx];
  }

  get prevBranch(): SkillTreeBranch {
    const idx = (this.branchIdx - 1 + this.branches.length) % this.branches.length;
    return this.branches[idx];
  }

  get nextBranch(): SkillTreeBranch {
    return this.branches[(this.branchIdx + 1) % this.branches.length];
  }

  get unlockedInBranch(): number {
    return this.currentBranch.nodes.filter(n => this.state.unlockedNodeIds.includes(n.id)).length;
  }

  get branchQuests(): SkillTreeQuest[] {
    return this.state.quests.filter(q => q.branchId === this.currentBranch.id);
  }

  get otherQuests(): SkillTreeQuest[] {
    return this.state.quests.filter(q => q.branchId !== this.currentBranch.id);
  }

  get openQuestCount(): number {
    return this.state.quests.filter(q => !q.done).length;
  }

  get detailNode(): SkillTreeNode | null {
    const id = this.selectedNode?.nodeId ?? this.hoveredNode?.nodeId;
    if (!id) return null;
    return this.currentBranch.nodes.find(n => n.id === id) ?? null;
  }

  get detailStatus(): SkillTreeNodeStatus | null {
    const node = this.detailNode;
    if (!node) return null;
    if (this.state.unlockedNodeIds.includes(node.id)) return 'unlocked';
    return node.prereqIds.every(p => this.state.unlockedNodeIds.includes(p)) ? 'available' : 'locked';
  }

  get detailIsHoverOnly(): boolean {
    return !this.selectedNode && !!this.hoveredNode;
  }

  get xpProgress(): number {
    return Math.min(1, this.xpInLevel / XP_PER_LEVEL);
  }

  prereqInfo(node: SkillTreeNode) {
    return node.prereqIds.map(id => {
      const target = this.currentBranch.nodes.find(n => n.id === id);
      return {
        id,
        name: target?.name ?? id,
        met: this.state.unlockedNodeIds.includes(id),
      };
    });
  }

  goToBranch(idx: number): void {
    if (idx === this.branchIdx) return;
    this.branchIdx = idx;
    this.selectedNode = null;
    this.scene?.goToSkill(idx, this.branches.length);
  }

  shiftBranch(delta: number): void {
    const next = (this.branchIdx + delta + this.branches.length) % this.branches.length;
    this.goToBranch(next);
  }

  toggleMute(): void {
    this.muted = !this.muted;
    this.audio.setMuted(this.muted);
  }

  closeDetail(): void {
    this.selectedNode = null;
  }

  unlockNode(node: SkillTreeNode): void {
    if (this.state.unlockedNodeIds.includes(node.id)) return;
    const prereqsMet = node.prereqIds.every(p => this.state.unlockedNodeIds.includes(p));
    if (!prereqsMet) {
      this.audio.denied();
      return;
    }
    this.audio.unlock();
    this.scene?.unlockNode(this.currentBranch.id, node.id);
    this.state = {
      ...this.state,
      unlockedNodeIds: [...this.state.unlockedNodeIds, node.id],
    };
    this.recomputeXp();
    this.persist();
    this.showToast({ kind: 'unlock', title: node.name });
  }

  toggleQuest(quest: SkillTreeQuest): void {
    quest.done = !quest.done;
    if (quest.done) {
      this.audio.complete();
      const branch = this.skillTree.findBranch(quest.branchId);
      this.showToast({ kind: 'xp', title: `+${quest.xp} XP`, sub: branch?.subtitle });
    } else {
      this.audio.click();
    }
    this.recomputeXp();
    this.persist();
  }

  addQuest(): void {
    const text = this.newQuestText.trim();
    if (!text || !this.newQuestBranchId) return;
    this.state.quests = [
      {
        id: this.skillTree.generateQuestId(),
        branchId: this.newQuestBranchId,
        text,
        xp: 20,
        done: false,
      },
      ...this.state.quests,
    ];
    this.newQuestText = '';
    this.persist();
  }

  resetProgress(): void {
    if (!confirm('Reset your skill tree progress and quests?')) return;
    this.state = this.skillTree.defaultState();
    this.scene?.setUnlocked(this.state.unlockedNodeIds);
    this.selectedNode = null;
    this.recomputeXp();
    this.persist();
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }

  branchSubtitle(branchId: string): string {
    return this.skillTree.findBranch(branchId)?.subtitle ?? '';
  }

  trackByBranch(_: number, b: SkillTreeBranch) { return b.id; }
  trackByNode(_: number, n: SkillTreeNode) { return n.id; }
  trackByQuest(_: number, q: SkillTreeQuest) { return q.id; }
  trackByPrereq(_: number, p: { id: string }) { return p.id; }

  private startScene(): void {
    this.scene = new SkillTreeScene();
    this.scene.init(this.canvasRef.nativeElement, this.branches, this.state.unlockedNodeIds);
    this.scene.on('hover', (picked: SkillTreePickedNode | null) => {
      this.zone.run(() => {
        this.hoveredNode = picked;
      });
    });
    this.scene.on('click', (picked: SkillTreePickedNode) => {
      this.zone.run(() => {
        this.selectedNode = picked;
        this.audio.click();
      });
    });
    this.scene.on('whoosh', () => this.audio.whoosh());
    this.scene.on('hoverSound', () => this.audio.hover());
  }

  private onKey(e: KeyboardEvent): void {
    const target = e.target as HTMLElement;
    if (target && (target.tagName === 'INPUT' || target.tagName === 'SELECT' || target.tagName === 'TEXTAREA')) return;
    if (e.key === 'ArrowLeft') {
      this.zone.run(() => this.shiftBranch(-1));
    } else if (e.key === 'ArrowRight') {
      this.zone.run(() => this.shiftBranch(1));
    } else if (e.key === 'Escape') {
      this.zone.run(() => this.closeDetail());
    } else if (e.key === 'm' || e.key === 'M') {
      this.zone.run(() => this.toggleMute());
    }
  }

  private onDragDown(e: MouseEvent): void {
    this.dragStartX = e.clientX;
  }

  private onDragMove(e: MouseEvent): void {
    if (this.dragStartX === null) return;
    const dx = e.clientX - this.dragStartX;
    if (Math.abs(dx) > 100) {
      this.zone.run(() => this.shiftBranch(dx < 0 ? 1 : -1));
      this.dragStartX = null;
    }
  }

  private onDragUp(): void {
    this.dragStartX = null;
  }

  private recomputeXp(): void {
    this.totalXp = this.skillTree.totalXp(this.state);
    this.level = Math.floor(this.totalXp / XP_PER_LEVEL) + 1;
    this.xpInLevel = this.totalXp % XP_PER_LEVEL;
  }

  private persist(): void {
    this.skillTree.save(this.state);
  }

  private showToast(toast: { kind: 'unlock' | 'xp'; title: string; sub?: string }): void {
    this.toast = toast;
    if (this.toastTimer) window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => {
      this.toast = null;
      this.cdr.detectChanges();
    }, toast.kind === 'unlock' ? 2400 : 1800);
  }
}
