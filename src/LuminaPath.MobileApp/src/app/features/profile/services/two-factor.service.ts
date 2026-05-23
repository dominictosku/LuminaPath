import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiEndpointService } from 'src/app/shared/services/api-endpoint.service';

/**
 * Mirrors ASP.NET Core Identity's `TwoFactorRequest`. All fields are
 * optional — sending `{}` returns the current status (and an auto-
 * generated shared key on the first call). Combine flags carefully:
 * `enable` + `twoFactorCode` enables, `resetSharedKey: true` resets the
 * key AND disables 2FA, etc.
 */
export interface TwoFactorRequest {
  enable?: boolean | null;
  twoFactorCode?: string | null;
  resetSharedKey?: boolean | null;
  resetRecoveryCodes?: boolean | null;
  forgetMachine?: boolean | null;
}

/**
 * Mirrors ASP.NET Core Identity's `TwoFactorResponse`. `recoveryCodes`
 * is non-null only on the response that just generated them; subsequent
 * calls return `null` for security.
 */
export interface TwoFactorResponse {
  sharedKey: string;
  recoveryCodesLeft: number;
  recoveryCodes: string[] | null;
  isTwoFactorEnabled: boolean;
  isMachineRemembered: boolean;
}

@Injectable({ providedIn: 'root' })
export class TwoFactorService {
  private readonly http = inject(HttpClient);
  private readonly apiEndpoint = inject(ApiEndpointService);

  /**
   * Fetches the current 2FA status. Identity exposes this as POST {}
   * — there's no GET. The first call ever for a user auto-generates a
   * shared key so the client can render the QR code.
   */
  load(): Observable<TwoFactorResponse> {
    return this.send({});
  }

  enable(twoFactorCode: string): Observable<TwoFactorResponse> {
    return this.send({ enable: true, twoFactorCode });
  }

  disable(): Observable<TwoFactorResponse> {
    return this.send({ enable: false });
  }

  /**
   * Throws away the current authenticator secret. The server also
   * disables 2FA as a side effect — re-enrollment is required.
   */
  resetSharedKey(): Observable<TwoFactorResponse> {
    return this.send({ resetSharedKey: true });
  }

  /** Generates a fresh set of recovery codes (only when 2FA is enabled). */
  regenerateRecoveryCodes(): Observable<TwoFactorResponse> {
    return this.send({ resetRecoveryCodes: true });
  }

  forgetMachine(): Observable<TwoFactorResponse> {
    return this.send({ forgetMachine: true });
  }

  private send(request: TwoFactorRequest): Observable<TwoFactorResponse> {
    return this.http.post<TwoFactorResponse>(
      this.apiEndpoint.url('manage/2fa'),
      request,
      { withCredentials: true },
    );
  }
}
