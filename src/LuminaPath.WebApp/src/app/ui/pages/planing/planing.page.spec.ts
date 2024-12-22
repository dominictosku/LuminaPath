import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PlaningPage } from './planing.page';

describe('PlaningPage', () => {
  let component: PlaningPage;
  let fixture: ComponentFixture<PlaningPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(PlaningPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
