import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { Game } from '../../games/models/games.model';
import { ReleaseCalendarComponent } from './release-calendar.component';

function releaseGame(id: number, name: string, daysFromToday: number): Game {
  const releaseDate = new Date();
  releaseDate.setDate(releaseDate.getDate() + daysFromToday);

  const game = new Game(name, '', releaseDate, 'RPG', 2, 30);
  game.id = id;
  return game;
}

describe('ReleaseCalendarComponent', () => {
  let component: ReleaseCalendarComponent;
  let fixture: ComponentFixture<ReleaseCalendarComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ReleaseCalendarComponent],
      providers: [provideIonicAngular()],
    });

    fixture = TestBed.createComponent(ReleaseCalendarComponent);
    component = fixture.componentInstance;
  });

  it('builds sorted upcoming release cards from games', () => {
    const later = releaseGame(1, 'Later Quest', 10);
    const sooner = releaseGame(2, 'Soon Quest', 2);
    const old = releaseGame(3, 'Old Quest', -400);

    fixture.componentRef.setInput('games', [later, old, sooner]);
    fixture.detectChanges();

    expect(component.upcomingReleases.map((game) => game.name)).toEqual(['Soon Quest', 'Later Quest']);
    expect(component.calendarDays.length).toBe(42);
    expect(fixture.nativeElement.textContent).toContain('Soon Quest');
    expect(fixture.nativeElement.textContent).toContain('Later Quest');
  });
});
