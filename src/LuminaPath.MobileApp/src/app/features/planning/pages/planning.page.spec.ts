import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlanningPage } from './planning.page';

describe('PlanningPage', () => {
  let component: PlanningPage;
  let fixture: ComponentFixture<PlanningPage>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PlanningPage],
    });

    fixture = TestBed.createComponent(PlanningPage);
    component = fixture.componentInstance;
  });

  it('points users to the new planning locations', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(component).toBeTruthy();
    expect(text).toContain('Planning moved to Home');
    expect(text).toContain('Release planning now lives at the bottom of Home');
  });
});
