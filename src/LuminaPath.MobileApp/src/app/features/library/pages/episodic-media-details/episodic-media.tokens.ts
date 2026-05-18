import { InjectionToken } from '@angular/core';
import { EpisodicMediaAdapter, EpisodicMediaConfig } from './episodic-media.types';

export const EPISODIC_MEDIA_CONFIG = new InjectionToken<EpisodicMediaConfig>('EPISODIC_MEDIA_CONFIG');
export const EPISODIC_MEDIA_ADAPTER = new InjectionToken<EpisodicMediaAdapter>('EPISODIC_MEDIA_ADAPTER');
