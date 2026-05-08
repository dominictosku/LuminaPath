import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '@ionic/angular';
import { of } from 'rxjs';

import { GridComponent } from './grid.component';
import { GameService } from 'src/app/features/games/services/game.service';
import { IonicFunctionsService } from 'src/app/shared/services/ionic-functions.service';

describe('GridComponent', () => {
  let component: GridComponent;
  let fixture: ComponentFixture<GridComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [GridComponent, IonicModule.forRoot()],
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

    fixture = TestBed.createComponent(GridComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
