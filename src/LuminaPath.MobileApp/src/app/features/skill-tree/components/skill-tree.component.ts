import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonIcon } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  closeOutline,
  lockClosedOutline,
  musicalNotesOutline,
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
} from '../models/skill-tree.model';
import { SkillTreeAudioService } from '../services/skill-tree-audio.service';
import { SkillTreeScene } from '../services/skill-tree-scene';

const QUESTS_STORAGE_KEY = 'luminapath.skill-tree.quests.v2';

export interface NodeUnlockEvent {
  branchId: string;
  nodeId: string;
  xp: number;
}

@Component({
  selector: 'app-skill-tree',
  templateUrl: './skill-tree.component.html',
  styleUrls: ['./skill-tree.component.scss'],
  imports: [CommonModule, FormsModule, IonIcon],
})
export class SkillTreeComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  @Input() branches: SkillTreeBranch[] = [];
  @Input() unlockedNodeIds: string[] = [];

  @Output() nodeUnlocked = new EventEmitter<NodeUnlockEvent>();

  branchIdx = 0;
  loading = true;
  muted = false;

  hoveredNode: SkillTreePickedNode | null = null;
  selectedNode: SkillTreePickedNode | null = null;

  quests: SkillTreeQuest[] = [];
  newQuestText = '';
  newQuestBranchId = '';

  private scene: SkillTreeScene | null = null;
  private viewReady = false;
  private keyHandler = (e: KeyboardEvent) => this.onKey(e);
  private dragStartX: number | null = null;
  private dragHandlerDown = (e: MouseEvent) => this.onDragDown(e);
  private dragHandlerMove = (e: MouseEvent) => this.onDragMove(e);
  private dragHandlerUp = () => this.onDragUp();

  constructor(
    private audio: SkillTreeAudioService,
    private zone: NgZone,
    private cdr: ChangeDetectorRef,
  ) {
    addIcons({
      closeOutline,
      lockClosedOutline,
      musicalNotesOutline,
      sparkles,
      sparklesOutline,
      starOutline,
      volumeMuteOutline,
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.quests = this.loadQuests();
    if (this.branches.length) {
      this.startScene();
    }
    if (!this.newQuestBranchId && this.branches.length) {
      this.newQuestBranchId = this.branches[0].id;
    }

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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['branches']) {
      const branchesChanged = this.didBranchesChange(
        changes['branches'].previousValue as SkillTreeBranch[] | undefined,
        this.branches,
      );
      if (this.viewReady && branchesChanged) {
        this.branchIdx = Math.min(this.branchIdx, Math.max(0, this.branches.length - 1));
        this.selectedNode = null;
        this.hoveredNode = null;
        this.restartScene();
      }
      // Drop quests whose branch was deleted, and seed select if empty.
      const validBranchIds = new Set(this.branches.map((b) => b.id));
      const filtered = this.quests.filter((q) => validBranchIds.has(q.branchId));
      if (filtered.length !== this.quests.length) {
        this.quests = filtered;
        this.persistQuests();
      }
      if (!validBranchIds.has(this.newQuestBranchId) && this.branches.length) {
        this.newQuestBranchId = this.branches[0].id;
      }
      return;
    }

    if (changes['unlockedNodeIds'] && this.scene && this.viewReady) {
      this.zone.runOutsideAngular(() => this.scene?.setUnlocked(this.unlockedNodeIds));
    }
  }

  private didBranchesChange(prev: SkillTreeBranch[] | undefined, next: SkillTreeBranch[]): boolean {
    if (!prev) return true;
    if (prev.length !== next.length) return true;
    for (let i = 0; i < next.length; i++) {
      const a = prev[i];
      const b = next[i];
      if (a.id !== b.id || a.name !== b.name || a.hue !== b.hue) return true;
      if (a.nodes.length !== b.nodes.length) return true;
      for (let j = 0; j < a.nodes.length; j++) {
        if (a.nodes[j].id !== b.nodes[j].id || a.nodes[j].name !== b.nodes[j].name) return true;
      }
    }
    return false;
  }

  ngOnDestroy(): void {
    window.removeEventListener('keydown', this.keyHandler);
    const canvas = this.canvasRef?.nativeElement;
    if (canvas) canvas.removeEventListener('mousedown', this.dragHandlerDown);
    window.removeEventListener('mousemove', this.dragHandlerMove);
    window.removeEventListener('mouseup', this.dragHandlerUp);
    this.scene?.destroy();
  }

  get currentBranch(): SkillTreeBranch | null {
    return this.branches[this.branchIdx] ?? null;
  }

  get prevBranch(): SkillTreeBranch | null {
    if (this.branches.length < 2) return null;
    const idx = (this.branchIdx - 1 + this.branches.length) % this.branches.length;
    return this.branches[idx];
  }

  get nextBranch(): SkillTreeBranch | null {
    if (this.branches.length < 2) return null;
    return this.branches[(this.branchIdx + 1) % this.branches.length];
  }

  get unlockedInBranch(): number {
    const branch = this.currentBranch;
    if (!branch) return 0;
    return branch.nodes.filter((n) => this.unlockedNodeIds.includes(n.id)).length;
  }

  get detailNode(): SkillTreeNode | null {
    const branch = this.currentBranch;
    if (!branch) return null;
    const id = this.selectedNode?.nodeId ?? this.hoveredNode?.nodeId;
    if (!id) return null;
    return branch.nodes.find((n) => n.id === id) ?? null;
  }

  get detailStatus(): SkillTreeNodeStatus | null {
    const node = this.detailNode;
    if (!node) return null;
    if (this.unlockedNodeIds.includes(node.id)) return 'unlocked';
    return node.prereqIds.every((p) => this.unlockedNodeIds.includes(p)) ? 'available' : 'locked';
  }

  get detailIsHoverOnly(): boolean {
    return !this.selectedNode && !!this.hoveredNode;
  }

  prereqInfo(node: SkillTreeNode) {
    const branch = this.currentBranch;
    if (!branch) return [];
    return node.prereqIds.map((id) => {
      const target = branch.nodes.find((n) => n.id === id);
      return {
        id,
        name: target?.name ?? id,
        met: this.unlockedNodeIds.includes(id),
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
    if (this.branches.length < 2) return;
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
    const branch = this.currentBranch;
    if (!branch) return;
    if (this.unlockedNodeIds.includes(node.id)) return;
    const prereqsMet = node.prereqIds.every((p) => this.unlockedNodeIds.includes(p));
    if (!prereqsMet) {
      this.audio.denied();
      return;
    }
    this.audio.unlock();
    this.scene?.unlockNode(branch.id, node.id);
    this.nodeUnlocked.emit({ branchId: branch.id, nodeId: node.id, xp: node.xp });
  }

  trackByBranch(_: number, b: SkillTreeBranch) { return b.id; }
  trackByNode(_: number, n: SkillTreeNode) { return n.id; }
  trackByPrereq(_: number, p: { id: string }) { return p.id; }
  trackByQuest(_: number, q: SkillTreeQuest) { return q.id; }

  get branchQuests(): SkillTreeQuest[] {
    const branchId = this.currentBranch?.id;
    return branchId ? this.quests.filter((q) => q.branchId === branchId) : [];
  }

  get otherQuests(): SkillTreeQuest[] {
    const branchId = this.currentBranch?.id;
    return branchId ? this.quests.filter((q) => q.branchId !== branchId) : this.quests;
  }

  get openQuestCount(): number {
    return this.quests.filter((q) => !q.done).length;
  }

  branchSubtitle(branchId: string): string {
    return this.branches.find((b) => b.id === branchId)?.subtitle ?? '';
  }

  toggleQuest(quest: SkillTreeQuest): void {
    quest.done = !quest.done;
    if (quest.done) {
      this.audio.complete();
    } else {
      this.audio.click();
    }
    this.persistQuests();
  }

  addQuest(): void {
    const text = this.newQuestText.trim();
    if (!text || !this.newQuestBranchId) return;
    this.quests = [
      {
        id: 'q-' + Math.random().toString(36).slice(2, 10),
        branchId: this.newQuestBranchId,
        text,
        xp: 20,
        done: false,
      },
      ...this.quests,
    ];
    this.newQuestText = '';
    this.persistQuests();
  }

  private loadQuests(): SkillTreeQuest[] {
    try {
      const raw = localStorage.getItem(QUESTS_STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) return [];
      return parsed
        .filter((q) => q && typeof q.id === 'string' && typeof q.branchId === 'string' && typeof q.text === 'string')
        .map((q) => ({ id: q.id, branchId: q.branchId, text: q.text, xp: Number(q.xp) || 20, done: !!q.done }));
    } catch {
      return [];
    }
  }

  private persistQuests(): void {
    try {
      localStorage.setItem(QUESTS_STORAGE_KEY, JSON.stringify(this.quests));
    } catch {
      // ignore quota / private mode
    }
  }

  private startScene(): void {
    this.zone.runOutsideAngular(() => {
      this.scene = new SkillTreeScene();
      this.scene.init(this.canvasRef.nativeElement, this.branches, this.unlockedNodeIds);
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
    });
  }

  private restartScene(): void {
    this.scene?.destroy();
    this.scene = null;
    if (this.branches.length) {
      this.startScene();
    }
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
}
