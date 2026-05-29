import { AchievementInfo, Quest, QuestFolder, QuestSkill } from '../services/quest-board.service';
import { QuestViewMode } from '../components/quest-board-toolbar/quest-board-toolbar.component';
import { QuestEditDraft } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import { QuestQuickAddPreset } from '../components/quest-quick-add/quest-quick-add.component';
import { LibraryGame, PageMode } from '../models/quest-board-view.model';
import { QuestFilter } from '../domain/quest-sections.builder';

export type SkillNodeState = 'completed' | 'available' | 'locked';

export interface QuestBoardStateShape {
  // ----- persisted board data -----
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  quests: Quest[];
  skills: QuestSkill[];
  folders: QuestFolder[];
  achievements: AchievementInfo[];
  library: LibraryGame[];

  // ----- load state -----
  isLoading: boolean;
  errorMessage: string;

  // ----- shared view / preference state -----
  mode: PageMode;
  filter: QuestFilter;
  searchQuery: string;
  tagFilter: string | null;
  questViewMode: QuestViewMode;
  quickAddType: Quest['type'];
  quickAddPriority: Quest['priority'];
  quickAddRecurrence: Quest['recurrence'];
  /** New object reference each time → quick-add child re-applies its prefill. */
  quickAddPreset: QuestQuickAddPreset | null;

  // ----- inline edit (shared by the Quests and Overview views) -----
  expandedQuestId: number | null;
  editDraft: QuestEditDraft | null;
  newSubtaskTitle: Record<number, string>;

  // ----- folder (Overview) view state -----
  collapsedFolderIds: ReadonlySet<number>;
  unfiledCollapsed: boolean;
  folderReorderActive: boolean;

  /** Quests pending deletion, kept around so the undo toast can render. */
  pendingDeletes: Quest[];
  /** Deep link target: expand this quest once the board has loaded. */
  focusQuestId: number | null;
}

export const initialState: QuestBoardStateShape = {
  xp: 0,
  currentStreakDays: 0,
  longestStreakDays: 0,
  quests: [],
  skills: [],
  folders: [],
  achievements: [],
  library: [],
  isLoading: true,
  errorMessage: '',
  mode: 'quests',
  filter: 'today',
  searchQuery: '',
  tagFilter: null,
  questViewMode: 'cards',
  quickAddType: 'sub',
  quickAddPriority: 'medium',
  quickAddRecurrence: 'none',
  quickAddPreset: null,
  expandedQuestId: null,
  editDraft: null,
  newSubtaskTitle: {},
  collapsedFolderIds: new Set<number>(),
  unfiledCollapsed: false,
  folderReorderActive: false,
  pendingDeletes: [],
  focusQuestId: null,
};

export const DELETE_GRACE_MS = 5000;

export function nextTemporaryId(): number {
  return -Math.floor(Math.random() * 1_000_000_000);
}
