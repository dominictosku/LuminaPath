import { TestBed } from '@angular/core/testing';

import { ErrorReporter } from './error-reporter.service';
import { GlobalErrorHandler } from './global-error-handler';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;
  let reporter: ErrorReporter;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    handler = TestBed.inject(GlobalErrorHandler);
    reporter = TestBed.inject(ErrorReporter);
    // Silence the deliberate console.error we make on every handled error so
    // the test output stays readable.
    spyOn(console, 'error');
  });

  it('logs to console.error', () => {
    const err = new Error('boom');
    handler.handleError(err);
    expect(console.error).toHaveBeenCalledWith(err);
  });

  it('pushes the error into ErrorReporter so the banner can render', () => {
    handler.handleError(new Error('cache miss'));
    expect(reporter.current()?.message).toBe('cache miss');
  });

  it('does not throw when the reporter itself fails', () => {
    spyOn(reporter, 'report').and.throwError('reporter exploded');
    expect(() => handler.handleError(new Error('original'))).not.toThrow();
  });
});
