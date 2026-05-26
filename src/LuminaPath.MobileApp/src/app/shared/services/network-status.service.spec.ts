import { TestBed } from '@angular/core/testing';
import { NetworkStatusService } from './network-status.service';

describe('NetworkStatusService', () => {
  let originalOnLineDescriptor: PropertyDescriptor | undefined;
  let onLineValue: boolean;

  beforeEach(() => {
    originalOnLineDescriptor = Object.getOwnPropertyDescriptor(window.navigator, 'onLine');
    onLineValue = true;
    Object.defineProperty(window.navigator, 'onLine', {
      configurable: true,
      get: () => onLineValue,
    });
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    if (originalOnLineDescriptor) {
      Object.defineProperty(window.navigator, 'onLine', originalOnLineDescriptor);
    }
  });

  it('seeds the signal from navigator.onLine at construction time', () => {
    onLineValue = false;
    const service = TestBed.inject(NetworkStatusService);
    expect(service.isOnline()).toBeFalse();
  });

  it('flips to false when the window fires an offline event', () => {
    const service = TestBed.inject(NetworkStatusService);
    expect(service.isOnline()).toBeTrue();
    window.dispatchEvent(new Event('offline'));
    expect(service.isOnline()).toBeFalse();
  });

  it('flips back to true on an online event', () => {
    onLineValue = false;
    const service = TestBed.inject(NetworkStatusService);
    expect(service.isOnline()).toBeFalse();
    window.dispatchEvent(new Event('online'));
    expect(service.isOnline()).toBeTrue();
  });
});
