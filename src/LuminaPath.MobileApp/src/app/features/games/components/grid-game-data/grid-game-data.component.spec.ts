import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '@ionic/angular';
import { of } from 'rxjs';

import { GridGameDataComponent } from './grid-game-data.component';
import { GameService } from 'src/app/features/games/services/game.service';
import { IonicFunctionsService } from 'src/app/shared/services/ionic-functions.service';

describe('GridGameDataComponent', () => {
  let component: GridGameDataComponent;
  let fixture: ComponentFixture<GridGameDataComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [GridGameDataComponent, IonicModule.forRoot()],
      providers: [
        {
          provide: GameService,
          useValue: { getAll: jasmine.createSpy('getAll').and.returnValue(of({ data: [] })) },
        },
        {
          provide: IonicFunctionsService,
          useValue: { openModal: jasmine.createSpy('openModal') },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GridGameDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
