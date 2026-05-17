import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { MyGamesComponent } from './my-games.component';

describe('MyGamesComponent', () => {
  let component: MyGamesComponent;
  let fixture: ComponentFixture<MyGamesComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [MyGamesComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MyGamesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows manual and third-party tracked hours together', () => {
    component.media.myGames = {
      id: 1,
      rating: null,
      startDate: null,
      endDate: null,
      status: 2,
      timeSpend: 3,
      gameId: 1,
      personalNotes: null,
      game: null,
      myGameInfo: {
        id: 1,
        myGameId: 1,
        trackedHours: 4.25,
        firstPlayed: null,
        lastPlayed: null,
      },
    };

    expect(component.playedHoursLabel(component.media)).toBe('7.3h');
  });
});
