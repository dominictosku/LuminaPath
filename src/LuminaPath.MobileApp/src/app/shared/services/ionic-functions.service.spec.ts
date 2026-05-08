import { TestBed } from '@angular/core/testing';
import { ModalController, ToastController } from '@ionic/angular';

import { IonicFunctionsService } from './ionic-functions.service';

describe('IonicFunctionsService', () => {
  let service: IonicFunctionsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ModalController,
          useValue: {
            create: jasmine.createSpy('create'),
            dismiss: jasmine.createSpy('dismiss'),
          },
        },
        {
          provide: ToastController,
          useValue: {
            create: jasmine.createSpy('create'),
          },
        },
      ],
    });
    service = TestBed.inject(IonicFunctionsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
