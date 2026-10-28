import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { BrowsePage } from './browse.page';
import { BrowseService } from '../services/browse.service';
import { BrowseItem } from '../models/browse.model';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import { MediaLibraryFacade } from '../../library/services/media-library.facade';

const releases: BrowseItem[] = [
  { id: 1, name: 'Zelda', kind: 'games', releaseDate: '2026-10-02', description: '', genre: '', image: null, addedCount: 20 },
  { id: 2, name: 'Celeste', kind: 'games', releaseDate: '2026-10-20', description: '', genre: '', image: null, addedCount: 5 },
];

describe('BrowsePage release controls', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({providers: [
      {provide: BrowseService, useValue: {getGameReleases: () => of(releases), getAnimeReleases: () => of([])}},
      {provide: Router, useValue: {navigate: jasmine.createSpy('navigate')}},
      {provide: MediaLibraryFacade, useValue: {}},
    ]});
    TestBed.overrideComponent(BrowsePage, {set: {template: ''}});
  });

  it('honors the selected anime mode on entry', () => {
    TestBed.inject(MediaModeService).select('animes');
    const fixture = TestBed.createComponent(BrowsePage);
    fixture.detectChanges();
    expect(fixture.componentInstance.selectedKind).toBe('animes');
  });

  it('searches and sorts the selected release period without mutating the feed', () => {
    const fixture = TestBed.createComponent(BrowsePage);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    expect(page.visibleItems.map(item => item.id)).toEqual([1, 2]);
    page.sortMode = 'title';
    expect(page.visibleItems.map(item => item.id)).toEqual([2, 1]);
    page.searchTerm = ' ZEL ';
    expect(page.visibleItems.map(item => item.id)).toEqual([1]);
    expect(releases.map(item => item.id)).toEqual([1, 2]);
    page.previousPeriod();
    expect(page.visibleItems.length).toBe(0);
  });

  it('switches media kinds without resetting the chosen release month', () => {
    const fixture = TestBed.createComponent(BrowsePage);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    page.previousPeriod();
    const month = page.selectedGameMonth.getTime();
    page.selectKindTab('animes');
    fixture.detectChanges();
    page.selectKindTab('games');
    fixture.detectChanges();
    expect(page.selectedGameMonth.getTime()).toBe(month);
  });
});
