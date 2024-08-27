import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { GoBackComponent } from './go-back.component';

describe('GoBackComponent', () => {
  let component: GoBackComponent;
  let fixture: ComponentFixture<GoBackComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [GoBackComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GoBackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
