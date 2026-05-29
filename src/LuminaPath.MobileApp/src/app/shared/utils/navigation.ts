import { Location } from '@angular/common';
import { Router } from '@angular/router';

/**
 * Go back if there's history to pop, otherwise navigate to a fallback route.
 * Shared by the media detail pages, which are commonly reached via deep links
 * where `location.back()` would leave the app.
 */
export function goBackOrHome(location: Location, router: Router, fallback = '/library'): void {
  if (window.history.length > 1) {
    location.back();
    return;
  }
  void router.navigateByUrl(fallback);
}
