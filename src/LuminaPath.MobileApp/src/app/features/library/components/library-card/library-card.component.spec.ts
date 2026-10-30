import { TestBed } from '@angular/core/testing';
import { LibraryCardComponent } from './library-card.component';
import { MEDIA_MODE_OPTIONS } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../../models/media-item.model';

const item: MediaItem = { id: 1, name: 'A story', kind: 'games', image: null, genre: '', description: '', releaseDate: null, libraryEntry: null };

describe('LibraryCardComponent interactions', () => {
  it('opens details from the poster and tracking from the separate action', () => {
    const fixture = TestBed.createComponent(LibraryCardComponent);
    fixture.componentRef.setInput('item', item);
    fixture.componentRef.setInput('mediaMode', MEDIA_MODE_OPTIONS[0]);
    const edit = spyOn(fixture.componentInstance.edit, 'emit');
    const details = spyOn(fixture.componentInstance.details, 'emit');
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.poster-button').click();
    expect(details).toHaveBeenCalledOnceWith(item);
    expect(edit).not.toHaveBeenCalled();
    fixture.nativeElement.querySelector('.card-action').click();
    expect(edit).toHaveBeenCalledOnceWith(item);
  });

  it('selects owned items without opening either the editor or the detail route during bulk selection', () => {
    const fixture = TestBed.createComponent(LibraryCardComponent);
    fixture.componentRef.setInput('item', item);
    fixture.componentRef.setInput('mediaMode', MEDIA_MODE_OPTIONS[0]);
    fixture.componentRef.setInput('selectionMode', true);
    fixture.componentRef.setInput('selectable', true);
    const selected = spyOn(fixture.componentInstance.selectionChange, 'emit');
    const edit = spyOn(fixture.componentInstance.edit, 'emit');
    const details = spyOn(fixture.componentInstance.details, 'emit');
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.poster-button').click();
    expect(selected).toHaveBeenCalledOnceWith(item);
    expect(edit).not.toHaveBeenCalled();
    expect(details).not.toHaveBeenCalled();
  });
});
