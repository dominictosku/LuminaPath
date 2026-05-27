import { TestBed } from '@angular/core/testing';

import { QuestBoardPreferencesService } from './quest-board-preferences.service';

describe('QuestBoardPreferencesService', () => {
  let service: QuestBoardPreferencesService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(QuestBoardPreferencesService);
  });

  it('returns defaults when preferences are missing', () => {
    expect(service.load()).toEqual({
      filter: 'today',
      quickAddType: 'sub',
      quickAddPriority: 'medium',
      quickAddRecurrence: 'none',
      questViewMode: 'cards',
    });
  });

  it('persists and reloads valid preferences', () => {
    service.save({
      filter: 'all',
      quickAddType: 'main',
      quickAddPriority: 'high',
      quickAddRecurrence: 'weekly',
      questViewMode: 'compact',
    });

    expect(service.load()).toEqual({
      filter: 'all',
      quickAddType: 'main',
      quickAddPriority: 'high',
      quickAddRecurrence: 'weekly',
      questViewMode: 'compact',
    });
  });

  it('normalizes legacy overdue filters to today', () => {
    localStorage.setItem('questboard.prefs.v1', JSON.stringify({ filter: 'overdue' }));

    expect(service.load().filter).toBe('today');
  });
});
