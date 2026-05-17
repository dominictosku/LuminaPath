import { Game, MyGame } from '../models/games.model';
import { GameStatus } from '../../library/models/library-status.model';
import {
  estimatedHoursOfGame,
  gameStatusOf,
  playedHoursOfGame,
  progressRatioOfGame,
  remainingHoursOfGame,
} from './game-library-metrics';

function makeGame(overrides: Partial<Game> = {}): Game {
  return Object.assign(new Game(), overrides);
}

function makeMyGame(overrides: Partial<MyGame> = {}): MyGame {
  return Object.assign(new MyGame(1), overrides);
}

describe('game library metrics domain', () => {
  it('combines manual and tracked hours for owned games', () => {
    const game = makeGame({
      playtime: 30,
      myGames: makeMyGame({
        status: GameStatus.Playing,
        timeSpend: 8,
        myGameInfo: { id: 1, myGameId: 1, trackedHours: 4, firstPlayed: null, lastPlayed: null },
      }),
    });

    expect(gameStatusOf(game)).toBe(GameStatus.Playing);
    expect(estimatedHoursOfGame(game)).toBe(30);
    expect(playedHoursOfGame(game)).toBe(12);
    expect(remainingHoursOfGame(game)).toBe(18);
    expect(progressRatioOfGame(game)).toBe(0.4);
  });

  it('treats completed games without estimates as complete progress', () => {
    const game = makeGame({
      playtime: 0,
      myGames: makeMyGame({ status: GameStatus.Completed }),
    });

    expect(progressRatioOfGame(game)).toBe(1);
  });
});
