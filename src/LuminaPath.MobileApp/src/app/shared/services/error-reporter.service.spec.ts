import { TestBed } from '@angular/core/testing';

import { ErrorReporter } from './error-reporter.service';

describe('ErrorReporter', () => {
  let reporter: ErrorReporter;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    reporter = TestBed.inject(ErrorReporter);
  });

  it('starts empty', () => {
    expect(reporter.current()).toBeNull();
  });

  it('captures Error.message preferred over the fallback', () => {
    reporter.report(new Error('Database is down'));
    const record = reporter.current();
    expect(record?.message).toBe('Database is down');
    expect(record?.source).toBeInstanceOf(Error);
    expect(record?.reportedAt).toBeGreaterThan(0);
  });

  it('falls back to the HTTP-shape extractor for backend errors', () => {
    reporter.report({ error: { errorMessage: ['Field A required', 'Field B too long'] } });
    expect(reporter.current()?.message).toBe('Field A required Field B too long');
  });

  it('uses a friendly fallback when the value is opaque', () => {
    reporter.report({ random: 'shape' });
    expect(reporter.current()?.message).toBe('Something went wrong.');
  });

  it('replaces older errors instead of stacking them', () => {
    reporter.report(new Error('first'));
    reporter.report(new Error('second'));
    expect(reporter.current()?.message).toBe('second');
  });

  it('dismiss clears the slot', () => {
    reporter.report(new Error('boom'));
    reporter.dismiss();
    expect(reporter.current()).toBeNull();
  });
});
