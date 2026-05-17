export enum GameStatus {
  OnHold = 0,
  Planned = 1,
  Playing = 2,
  StoryComplete = 3,
  Completed = 4,
  MainGame = 5,
}

export enum WatchStatus {
  OnHold = 0,
  Planned = 1,
  Watching = 2,
  Completed = 3,
  Dropped = 4,
}

export const GAME_STATUS_LABELS: Record<GameStatus, string> = {
  [GameStatus.OnHold]: 'On hold',
  [GameStatus.Planned]: 'Planned',
  [GameStatus.Playing]: 'Playing',
  [GameStatus.StoryComplete]: 'Story complete',
  [GameStatus.Completed]: 'Completed',
  [GameStatus.MainGame]: 'Main game',
};

export function gameStatusLabel(status: number, fallback = 'Catalog'): string {
  return GAME_STATUS_LABELS[status as GameStatus] ?? fallback;
}

export function isGameBacklogStatus(status: number): boolean {
  return status === GameStatus.Planned
    || status === GameStatus.MainGame
    || status === GameStatus.OnHold;
}
