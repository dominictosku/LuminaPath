import type { QuestEditDraft } from '../components/quest-detail-sheet/quest-detail-sheet.component';
import type { Quest, QuestFolder, QuestSkill } from '../services/quest-board.service';
import {
  buildEditedQuest,
  buildOptimisticQuest,
  buildQuestDropListIds,
  buildQuestEditDraft,
  buildQuestUpdateFromDraft,
  collectKnownSectionsFromFolders,
  gameNameFor,
  getSkillNodeState,
  groupFoldersBySection,
  parseQuestTags,
  replaceQuest,
  skillNameFor,
} from './quest-board.store-helpers';

function makeQuest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: 1,
    title: 'Quest',
    notes: null,
    type: 'sub',
    priority: 'medium',
    recurrence: 'none',
    dueDate: null,
    tags: [],
    completed: false,
    rewardXp: 75,
    sortOrder: 0,
    myGameId: null,
    gameName: null,
    skillId: null,
    skillName: null,
    folderId: null,
    folderName: null,
    folderEmoji: null,
    subtasks: [],
    ...overrides,
  };
}

function makeFolder(overrides: Partial<QuestFolder> = {}): QuestFolder {
  return {
    id: 1,
    name: 'Backlog',
    emoji: 'box',
    color: null,
    sectionName: null,
    sortOrder: 0,
    ...overrides,
  };
}

function makeSkill(overrides: Partial<QuestSkill> = {}): QuestSkill {
  return {
    id: 1,
    name: 'Programming',
    icon: 'code-slash-outline',
    color: '#2563eb',
    xp: 0,
    nodes: [],
    unlockedNodes: [],
    ...overrides,
  };
}

describe('quest-board store helpers', () => {
  it('groups folders by section and derives section/drop-list helpers', () => {
    const folders = [
      makeFolder({ id: 1, name: 'Unfiled-like', sectionName: null }),
      makeFolder({ id: 2, name: 'Main', sectionName: 'Games' }),
      makeFolder({ id: 3, name: 'Side', sectionName: 'games' }),
      makeFolder({ id: 4, name: 'Reading', sectionName: 'Study' }),
    ];

    const groups = groupFoldersBySection(folders);

    expect(groups.map((group) => ({ section: group.section, ids: group.folders.map((folder) => folder.id) }))).toEqual([
      { section: null, ids: [1] },
      { section: 'Games', ids: [2, 3] },
      { section: 'Study', ids: [4] },
    ]);
    expect(collectKnownSectionsFromFolders(folders)).toEqual(['Games', 'Study']);
    expect(buildQuestDropListIds(folders)).toEqual([
      'quest-drop-folder-1',
      'quest-drop-folder-2',
      'quest-drop-folder-3',
      'quest-drop-folder-4',
      'quest-drop-unfiled',
    ]);
  });

  it('builds edit drafts and update payloads from quest form state', () => {
    const quest = makeQuest({
      notes: 'Existing',
      dueDate: '2026-01-15T12:00:00.000Z',
      tags: ['release', 'focus'],
      myGameId: 42,
      skillId: 7,
      folderId: 3,
    });

    const draft = buildQuestEditDraft(quest);

    expect(draft).toEqual({
      title: 'Quest',
      notes: 'Existing',
      type: 'sub',
      priority: 'medium',
      recurrence: 'none',
      dueDate: '2026-01-15',
      tags: 'release, focus',
      myGameId: 42,
      skillId: 7,
      folderId: 3,
    });

    const editedDraft: QuestEditDraft = {
      ...draft,
      title: '  Polish flow  ',
      notes: '  Ship it  ',
      dueDate: null,
      tags: 'release, , qa ',
      myGameId: null,
      skillId: 7,
      folderId: null,
    };
    const tags = parseQuestTags(editedDraft.tags);

    expect(buildQuestUpdateFromDraft(editedDraft, tags, 'Ship it')).toEqual({
      title: 'Polish flow',
      notes: 'Ship it',
      type: 'sub',
      priority: 'medium',
      recurrence: 'none',
      dueDate: null,
      clearDueDate: true,
      tags: ['release', 'qa'],
      myGameId: undefined,
      clearMyGame: true,
      skillId: 7,
      clearSkill: false,
      folderId: undefined,
      clearFolder: true,
    });
  });

  it('builds optimistic and edited quest rows with resolved labels', () => {
    const optimistic = buildOptimisticQuest(
      {
        title: 'Beat boss',
        type: 'main',
        priority: 'high',
        recurrence: 'none',
        dueDate: '2026-02-20',
        myGameId: 42,
        skillId: 7,
      },
      -1,
      '2026-02-01T10:00:00.000Z',
      'Hades',
      'Programming',
    );

    expect(optimistic).toEqual(
      jasmine.objectContaining({
        id: -1,
        title: 'Beat boss',
        gameName: 'Hades',
        skillName: 'Programming',
        completed: false,
        createdAt: '2026-02-01T10:00:00.000Z',
      }),
    );

    const folder = makeFolder({ id: 5, name: 'Bosses', emoji: 'flag' });
    const draft: QuestEditDraft = {
      title: '  Beat final boss  ',
      notes: '',
      type: 'main',
      priority: 'high',
      recurrence: 'weekly',
      dueDate: '2026-02-21',
      tags: 'boss',
      myGameId: 42,
      skillId: 7,
      folderId: 5,
    };

    expect(buildEditedQuest(makeQuest(), draft, ['boss'], null, 'Hades', 'Programming', folder)).toEqual(
      jasmine.objectContaining({
        title: 'Beat final boss',
        notes: null,
        recurrence: 'weekly',
        gameName: 'Hades',
        skillName: 'Programming',
        folderName: 'Bosses',
        folderEmoji: 'flag',
      }),
    );
  });

  it('resolves labels, replacements, and skill node state', () => {
    const skills = [makeSkill({ id: 7, name: 'Programming', nodes: ['Basics', 'Loops'], unlockedNodes: [0] })];
    const quests = [makeQuest({ id: 1, title: 'Old' }), makeQuest({ id: 2, title: 'Keep' })];

    expect(gameNameFor([{ myGameId: 42, gameName: 'Hades' }], 42)).toBe('Hades');
    expect(gameNameFor([], 42)).toBe('Unknown game');
    expect(skillNameFor(skills, 7)).toBe('Programming');
    expect(skillNameFor([], 7)).toBe('Skill');
    expect(replaceQuest(quests, 1, makeQuest({ id: 1, title: 'New' })).map((quest) => quest.title)).toEqual([
      'New',
      'Keep',
    ]);
    expect(getSkillNodeState(skills[0], 0)).toBe('completed');
    expect(getSkillNodeState(skills[0], 1)).toBe('available');
    expect(getSkillNodeState(skills[0], 2)).toBe('locked');
  });
});
