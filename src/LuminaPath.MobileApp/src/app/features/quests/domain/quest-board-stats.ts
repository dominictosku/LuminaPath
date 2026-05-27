import { addDays, startOfDay } from 'src/app/shared/utils/date-helpers';

import { Quest, QuestSkill } from '../services/quest-board.service';

export const QUEST_XP_PER_LEVEL = 200;

export type QuestBoardStats = {
  level: number;
  title: string;
  xpIntoLevel: number;
  xpProgress: number;
  completedQuestCount: number;
  activeQuestCount: number;
  unlockedNodeCount: number;
  todayCount: number;
  overdueCount: number;
};

export function buildQuestBoardStats(
  xp: number,
  quests: readonly Quest[],
  skills: readonly QuestSkill[],
  now = new Date(),
): QuestBoardStats {
  const today = startOfDay(now);
  const tomorrow = addDays(today, 1);
  const level = Math.floor(xp / QUEST_XP_PER_LEVEL) + 1;
  const xpIntoLevel = xp % QUEST_XP_PER_LEVEL;

  return {
    level,
    title: titleForQuestLevel(level),
    xpIntoLevel,
    xpProgress: xpIntoLevel / QUEST_XP_PER_LEVEL,
    completedQuestCount: quests.filter((quest) => quest.completed).length,
    activeQuestCount: quests.filter((quest) => !quest.completed).length,
    unlockedNodeCount: skills.reduce((sum, skill) => sum + skill.unlockedNodes.length, 0),
    todayCount: quests.filter((quest) => {
      if (quest.completed || !quest.dueDate) return false;
      const due = new Date(quest.dueDate);
      return due >= today && due < tomorrow;
    }).length,
    overdueCount: quests.filter((quest) => {
      if (quest.completed || !quest.dueDate) return false;
      return new Date(quest.dueDate) < today;
    }).length,
  };
}

export function titleForQuestLevel(level: number): string {
  if (level >= 15) return 'Legend';
  if (level >= 10) return 'Master';
  if (level >= 6) return 'Adept';
  if (level >= 3) return 'Apprentice';
  return 'Initiate';
}
