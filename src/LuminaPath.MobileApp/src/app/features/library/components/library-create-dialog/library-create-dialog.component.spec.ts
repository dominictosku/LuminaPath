import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { MEDIA_MODE_OPTIONS } from 'src/app/shared/services/media-mode.service';
import { Platforms } from 'src/app/features/games/models/games.model';

import { LibraryCreateDialogComponent } from './library-create-dialog.component';
import { emptyCreateForm } from './library-create-dialog.model';

describe('LibraryCreateDialogComponent', () => {
  let fixture: ComponentFixture<LibraryCreateDialogComponent>;
  let component: LibraryCreateDialogComponent;

  function configure(modeId: 'games' | 'animes' | 'series' = 'games'): void {
    TestBed.configureTestingModule({
      imports: [LibraryCreateDialogComponent],
      providers: [provideIonicAngular()],
    });
    fixture = TestBed.createComponent(LibraryCreateDialogComponent);
    component = fixture.componentInstance;
    const mode = MEDIA_MODE_OPTIONS.find((option) => option.id === modeId)!;
    fixture.componentRef.setInput('mediaMode', mode);
    fixture.componentRef.setInput('form', emptyCreateForm());
  }

  describe('mode predicates', () => {
    it('flags games mode for platforms / playtime fields', () => {
      configure('games');
      expect(component.isGamesMode()).toBeTrue();
      expect(component.isEpisodeMode()).toBeFalse();
    });

    it('flags episode mode for animes', () => {
      configure('animes');
      expect(component.isGamesMode()).toBeFalse();
      expect(component.isEpisodeMode()).toBeTrue();
    });

    it('flags episode mode for series', () => {
      configure('series');
      expect(component.isEpisodeMode()).toBeTrue();
    });
  });

  describe('canSubmit', () => {
    it('is false when the title is empty / whitespace', () => {
      configure('games');
      expect(component.canSubmit()).toBeFalse();

      fixture.componentRef.setInput('form', { ...emptyCreateForm(), name: '   ' });
      expect(component.canSubmit()).toBeFalse();
    });

    it('is true when a non-empty title is set', () => {
      configure('games');
      fixture.componentRef.setInput('form', { ...emptyCreateForm(), name: 'Hades' });
      expect(component.canSubmit()).toBeTrue();
    });
  });

  describe('togglePlatform', () => {
    it('flips a platform bit on/off in the bitmask', () => {
      configure('games');
      const pc = Platforms.find((p) => p.label === 'PC')!.value;
      const ps5 = Platforms.find((p) => p.label === 'PlayStation 5')!.value;

      component.togglePlatform(pc);
      expect(component.form().platforms).toBe(pc);
      expect(component.isPlatformSelected(pc)).toBeTrue();

      component.togglePlatform(ps5);
      expect(component.form().platforms).toBe(pc | ps5);
      expect(component.isPlatformSelected(ps5)).toBeTrue();

      component.togglePlatform(pc);
      expect(component.form().platforms).toBe(ps5);
      expect(component.isPlatformSelected(pc)).toBeFalse();
    });
  });

  describe('library entry toggle + status', () => {
    it('toggles the createLibraryEntry flag', () => {
      configure('games');
      expect(component.form().createLibraryEntry).toBeFalse();
      component.setCreateLibraryEntry(true);
      expect(component.form().createLibraryEntry).toBeTrue();
      component.setCreateLibraryEntry(false);
      expect(component.form().createLibraryEntry).toBeFalse();
    });

    it('updates only the nested library status without losing other fields', () => {
      configure('animes');
      fixture.componentRef.setInput('form', {
        ...emptyCreateForm(),
        name: 'Frieren',
        createLibraryEntry: true,
        libraryEntry: {
          status: 1,
          timeSpend: 5,
          rating: 8,
          startDate: '2026-05-01',
          endDate: null,
          personalNotes: 'pacing is dreamlike',
          currentEpisode: 3,
        },
      });

      component.selectStatus(2);

      const entry = component.form().libraryEntry;
      expect(entry.status).toBe(2);
      expect(entry.timeSpend).toBe(5);
      expect(entry.rating).toBe(8);
      expect(entry.personalNotes).toBe('pacing is dreamlike');
      expect(entry.currentEpisode).toBe(3);
      expect(component.form().name).toBe('Frieren');
    });
  });
});
