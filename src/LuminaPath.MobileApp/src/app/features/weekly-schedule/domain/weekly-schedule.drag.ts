import type { Quest, QuestPriority } from '../../quests/services/quest-board.service';
import type { LibraryGameOption } from './weekly-schedule.drafts';
import { formatTimeRange, type WeekScheduleBlock } from './weekly-schedule.helpers';

export type DragPayload =
  | { type: 'quest'; questId: number }
  | { type: 'game'; myGameId: number }
  | { type: 'session'; sessionId: number };

export type DragPreview = {
  kind: 'quest' | 'game' | 'session';
  title: string;
  subtitle: string;
  action: string;
  color: string;
  x: number;
  y: number;
};

export type DragPreviewContent = Omit<DragPreview, 'x' | 'y'>;

export function questDragPayload(quest: Pick<Quest, 'id'>): DragPayload {
  return { type: 'quest', questId: quest.id };
}

export function gameDragPayload(game: LibraryGameOption): DragPayload {
  return { type: 'game', myGameId: game.myGameId };
}

export function blockDragPayload(block: WeekScheduleBlock): DragPayload | null {
  if (block.projected) {
    return null;
  }

  if (block.kind === 'quest') {
    const questId = block.sourceId ?? Number(block.id.replace('quest-', ''));
    return Number.isFinite(questId) ? { type: 'quest', questId } : null;
  }

  const sessionId = block.sourceId ?? Number(block.id.replace('session-', ''));
  return Number.isFinite(sessionId) ? { type: 'session', sessionId } : null;
}

export function questDragPreview(quest: Quest, color: string): DragPreviewContent {
  return {
    kind: 'quest',
    title: quest.title,
    subtitle: quest.gameName ?? quest.folderName ?? priorityLabel(quest.priority),
    action: quest.scheduledStartAt ? 'Move quest' : 'Schedule quest',
    color,
  };
}

export function gameDragPreview(game: LibraryGameOption): DragPreviewContent {
  return {
    kind: 'game',
    title: game.gameName,
    subtitle: game.playtime ? `${game.playtime}h estimate` : 'Library game',
    action: 'Plan session',
    color: '#06b6d4',
  };
}

export function blockDragPreview(block: WeekScheduleBlock, action: string): DragPreviewContent {
  return {
    kind: block.kind,
    title: block.title,
    subtitle: `${formatTimeRange(block.startAt, block.endAt)}${block.subtitle ? ` · ${block.subtitle}` : ''}`,
    action,
    color: block.color,
  };
}

export function dragId(payload: DragPayload): string {
  if (payload.type === 'quest') {
    return `quest-${payload.questId}`;
  }

  if (payload.type === 'session') {
    return `session-${payload.sessionId}`;
  }

  return `game-${payload.myGameId}`;
}

export function dragData(payload: DragPayload): string {
  if (payload.type === 'quest') {
    return `quest:${payload.questId}`;
  }

  if (payload.type === 'session') {
    return `session:${payload.sessionId}`;
  }

  return `game:${payload.myGameId}`;
}

export function dragPreviewTransform(preview: DragPreview | null, offset = 18): string {
  if (!preview) {
    return 'translate3d(0, 0, 0)';
  }

  return `translate3d(${preview.x + offset}px, ${preview.y + offset}px, 0)`;
}

export function priorityLabel(priority: QuestPriority): string {
  return priority === 'high'
    ? 'High priority'
    : priority === 'low'
      ? 'Low priority'
      : 'Medium priority';
}
