import { TestBed } from '@angular/core/testing';

import { IonicFunctionsService } from './ionic-functions.service';

describe('IonicFunctionsService', () => {
  let service: IonicFunctionsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(IonicFunctionsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
