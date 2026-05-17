import { Injectable } from '@angular/core';

import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../models/media-item.model';
import {
  LibraryIntelligenceSummary,
  summarizeLibraryIntelligence,
} from '../domain/library-intelligence';

@Injectable({ providedIn: 'root' })
export class LibraryIntelligenceService {
  summarize(items: MediaItem[], mode: MediaModeOption): LibraryIntelligenceSummary {
    return summarizeLibraryIntelligence(items, mode);
  }
}
