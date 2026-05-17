import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { FilterComponent } from './filter.component';

describe('FilterComponent', () => {
  let component: FilterComponent;
  let fixture: ComponentFixture<FilterComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [FilterComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('tracks the selected view mode and emits it', () => {
    spyOn(component.toggleGrid, 'emit');

    component.changeView('list');

    expect(component.viewMode).toBe('list');
    expect(component.toggleGrid.emit).toHaveBeenCalledOnceWith('list');
  });
});
