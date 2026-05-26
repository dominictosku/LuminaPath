import { completionCardFileName } from './completion-card.service';

describe('completionCardFileName', () => {
  it('creates a safe png file name from the title', () => {
    expect(completionCardFileName('Hades II: The Return!')).toBe('hades-ii-the-return-completion-card.png');
  });

  it('falls back when the title has no safe characters', () => {
    expect(completionCardFileName('---')).toBe('completion-completion-card.png');
  });
});
