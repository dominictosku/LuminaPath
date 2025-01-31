/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { CookieInterceptorService } from './CookieInterceptor.service';

describe('Service: CookieInterceptor', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CookieInterceptorService]
    });
  });

  it('should ...', inject([CookieInterceptorService], (service: CookieInterceptorService) => {
    expect(service).toBeTruthy();
  }));
});
