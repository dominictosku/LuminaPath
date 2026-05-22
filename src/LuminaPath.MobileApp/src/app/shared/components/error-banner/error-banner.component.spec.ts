import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular/standalone';

import { ErrorReporter } from '../../services/error-reporter.service';
import { ErrorBannerComponent } from './error-banner.component';

describe('ErrorBannerComponent', () => {
  let fixture: ComponentFixture<ErrorBannerComponent>;
  let component: ErrorBannerComponent;
  let reporter: ErrorReporter;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ErrorBannerComponent],
      providers: [provideIonicAngular()],
    });

    fixture = TestBed.createComponent(ErrorBannerComponent);
    component = fixture.componentInstance;
    reporter = TestBed.inject(ErrorReporter);
  });

  it('renders nothing when there is no current error', () => {
    fixture.detectChanges();
    expect(component.visible()).toBeFalse();
    expect(fixture.nativeElement.querySelector('.error-banner')).toBeNull();
  });

  it('renders the message and recovery controls when an error is reported', () => {
    reporter.report(new Error('Lost connection'));
    fixture.detectChanges();
    expect(component.visible()).toBeTrue();
    const banner: HTMLElement = fixture.nativeElement.querySelector('.error-banner');
    expect(banner).not.toBeNull();
    expect(banner.textContent).toContain('Lost connection');
    expect(banner.querySelector('.error-banner__action')).not.toBeNull();
    expect(banner.querySelector('.error-banner__close')).not.toBeNull();
  });

  it('dismiss() clears the reporter and hides the banner', () => {
    reporter.report(new Error('boom'));
    fixture.detectChanges();
    component.dismiss();
    fixture.detectChanges();
    expect(reporter.current()).toBeNull();
    expect(fixture.nativeElement.querySelector('.error-banner')).toBeNull();
  });
});
