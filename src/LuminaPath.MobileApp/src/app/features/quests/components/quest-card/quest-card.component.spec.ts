import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { Quest } from '../../services/quest-board.service';
import { QuestCardComponent } from './quest-card.component';

function makeQuest(overrides: Partial<Quest> = {}): Quest {
  return {
    id: 1,
    title: 'Sample quest',
    type: 'main',
    priority: 'medium',
    recurrence: 'none',
    tags: [],
    completed: false,
    rewardXp: 0,
    sortOrder: 0,
    subtasks: [],
    ...overrides,
  } as Quest;
}

/** Build a YYYY-MM-DD string in the *local* timezone — toISOString() would
 *  convert to UTC and produce yesterday's date in positive offsets like
 *  Europe/Zurich, which is exactly how the production helpers (re)parse
 *  these strings via `new Date(quest.dueDate)` + startOfDay(). */
function isoDate(offsetDays: number): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

describe('QuestCardComponent', () => {
  let fixture: ComponentFixture<QuestCardComponent>;
  let component: QuestCardComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [QuestCardComponent],
      providers: [provideIonicAngular()],
    });

    fixture = TestBed.createComponent(QuestCardComponent);
    component = fixture.componentInstance;
  });

  describe('dueState', () => {
    it('is "none" when there is no due date', () => {
      fixture.componentRef.setInput('quest', makeQuest());
      expect(component.dueState()).toBe('none');
    });

    it('is "today" when the due date is today', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(0) }));
      expect(component.dueState()).toBe('today');
    });

    it('is "overdue" for past dates', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(-3) }));
      expect(component.dueState()).toBe('overdue');
    });

    it('is "soon" within 3 days and "later" beyond', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(2) }));
      expect(component.dueState()).toBe('soon');
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(10) }));
      expect(component.dueState()).toBe('later');
    });
  });

  describe('dueDateLabel', () => {
    it('renders Today / Tomorrow / Yesterday for adjacent days', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(0) }));
      expect(component.dueDateLabel()).toBe('Today');
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(1) }));
      expect(component.dueDateLabel()).toBe('Tomorrow');
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(-1) }));
      expect(component.dueDateLabel()).toBe('Yesterday');
    });

    it('renders relative buckets up to a week out', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(4) }));
      expect(component.dueDateLabel()).toBe('In 4d');
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(-3) }));
      expect(component.dueDateLabel()).toBe('3d overdue');
    });

    it('is empty when there is no due date or the value is invalid', () => {
      fixture.componentRef.setInput('quest', makeQuest());
      expect(component.dueDateLabel()).toBe('');
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: 'garbage' }));
      expect(component.dueDateLabel()).toBe('');
    });
  });

  describe('subtask helpers', () => {
    it('counts completed subtasks and reports zero progress for an empty list', () => {
      fixture.componentRef.setInput('quest', makeQuest());
      expect(component.subtaskCompletedCount()).toBe(0);
      expect(component.subtaskProgress()).toBe(0);
    });

    it('computes ratio progress against the subtask count', () => {
      fixture.componentRef.setInput(
        'quest',
        makeQuest({
          subtasks: [
            { id: 1, title: 'a', completed: true } as never,
            { id: 2, title: 'b', completed: false } as never,
            { id: 3, title: 'c', completed: true } as never,
            { id: 4, title: 'd', completed: false } as never,
          ],
        }),
      );
      expect(component.subtaskCompletedCount()).toBe(2);
      expect(component.subtaskProgress()).toBeCloseTo(0.5, 5);
    });
  });

  describe('canSchedule* / canMoveToInbox', () => {
    it('completed quests cannot be scheduled or moved', () => {
      fixture.componentRef.setInput(
        'quest',
        makeQuest({ completed: true, dueDate: isoDate(2) }),
      );
      expect(component.canScheduleToday()).toBeFalse();
      expect(component.canScheduleTomorrow()).toBeFalse();
      expect(component.canMoveToInbox()).toBeFalse();
    });

    it('canScheduleToday is false when already due today', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(0) }));
      expect(component.canScheduleToday()).toBeFalse();
    });

    it('canScheduleTomorrow is false when already due tomorrow', () => {
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(1) }));
      expect(component.canScheduleTomorrow()).toBeFalse();
    });

    it('canMoveToInbox requires an existing dueDate', () => {
      fixture.componentRef.setInput('quest', makeQuest());
      expect(component.canMoveToInbox()).toBeFalse();
      fixture.componentRef.setInput('quest', makeQuest({ dueDate: isoDate(3) }));
      expect(component.canMoveToInbox()).toBeTrue();
    });
  });

  describe('output emissions', () => {
    it('toggleComplete forwards the quest', () => {
      const quest = makeQuest();
      fixture.componentRef.setInput('quest', quest);
      const spy = jasmine.createSpy('toggleComplete');
      component.toggleComplete.subscribe(spy);
      fixture.detectChanges();
      const button: HTMLButtonElement = fixture.nativeElement.querySelector('.complete-button');
      button.click();
      expect(spy).toHaveBeenCalledOnceWith(quest);
    });

    it('requestDelete forwards the quest', () => {
      const quest = makeQuest();
      fixture.componentRef.setInput('quest', quest);
      const spy = jasmine.createSpy('requestDelete');
      component.requestDelete.subscribe(spy);
      fixture.detectChanges();
      const del: HTMLButtonElement = fixture.nativeElement.querySelector('.delete-button');
      del.click();
      expect(spy).toHaveBeenCalledOnceWith(quest);
    });
  });
});
