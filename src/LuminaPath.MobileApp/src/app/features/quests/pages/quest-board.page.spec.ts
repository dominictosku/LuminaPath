import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { QuestBoardPage } from './quest-board.page';
import { QuestBoardService, QuestBoardState } from '../services/quest-board.service';
import { MyGameService } from 'src/app/features/my-games/services/my-game.service';

function makeBoard(overrides: Partial<QuestBoardState> = {}): QuestBoardState {
  return {
    xp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    lastCompletionDate: null,
    quests: [],
    skills: [],
    achievements: [],
    ...overrides,
  };
}

describe('QuestBoardPage', () => {
  let fixture: ComponentFixture<QuestBoardPage>;
  let questBoardService: jasmine.SpyObj<QuestBoardService>;
  let myGameService: jasmine.SpyObj<MyGameService>;

  beforeEach(() => {
    localStorage.clear();

    questBoardService = jasmine.createSpyObj<QuestBoardService>('QuestBoardService', ['getBoard']);
    myGameService = jasmine.createSpyObj<MyGameService>('MyGameService', ['getAll']);
    questBoardService.getBoard.and.resolveTo(makeBoard());
    myGameService.getAll.and.returnValue(of({ data: [], pageIndex: 1, totalPages: 1 } as any));

    TestBed.configureTestingModule({
      imports: [QuestBoardPage],
      providers: [
        { provide: QuestBoardService, useValue: questBoardService },
        { provide: MyGameService, useValue: myGameService },
        // ?questId= deep-link subscription needs a queryParamMap to resolve.
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({})) } },
      ],
    });

    fixture = TestBed.createComponent(QuestBoardPage);
  });

  it('creates the shell and loads the board on view enter', () => {
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
    // The board loads on ionViewWillEnter (not ngOnInit), so it refreshes
    // every time the user returns to the kept-alive page.
    expect(questBoardService.getBoard).not.toHaveBeenCalled();

    fixture.componentInstance.ionViewWillEnter();
    expect(questBoardService.getBoard).toHaveBeenCalled();
  });
});
