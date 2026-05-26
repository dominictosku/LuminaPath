import { Component, DestroyRef, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonIcon } from '@ionic/angular/standalone';
import { catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import {
  GlobalSearchResult,
  GlobalSearchService,
} from 'src/app/shared/services/global-search.service';

@Component({
  selector: 'app-global-search',
  templateUrl: './global-search.component.html',
  styleUrls: ['./global-search.component.scss'],
  imports: [FormsModule, IonIcon],
})
export class GlobalSearchComponent {
  private readonly searchService = inject(GlobalSearchService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly query$ = new Subject<string>();

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  readonly isOpen = this.searchService.isOpen;

  query = '';
  results: GlobalSearchResult[] = [];
  isLoading = false;
  errorMessage = '';

  constructor() {
    this.query$
      .pipe(
        debounceTime(140),
        distinctUntilChanged(),
        tap((query) => {
          this.errorMessage = '';
          this.results = query.length < 2 ? [] : this.results;
          this.isLoading = query.length >= 2;
        }),
        switchMap((query) => {
          if (query.length < 2) {
            this.isLoading = false;
            return of([]);
          }

          return this.searchService.search(query).pipe(
            catchError(() => {
              this.errorMessage = 'Search is unavailable.';
              return of([]);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((results) => {
        this.results = results;
        this.isLoading = false;
      });

    effect(() => {
      if (this.isOpen()) {
        window.setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
        return;
      }

      this.query = '';
      this.results = [];
      this.errorMessage = '';
      this.isLoading = false;
    });
  }

  @HostListener('document:keydown', ['$event'])
  handleKeydown(event: KeyboardEvent): void {
    const key = event.key.toLowerCase();
    if ((event.metaKey || event.ctrlKey) && key === 'k') {
      event.preventDefault();
      this.searchService.open();
      return;
    }

    if (key === 'escape' && this.isOpen()) {
      event.preventDefault();
      this.close();
    }
  }

  onQueryChange(value: string): void {
    this.query = value;
    this.query$.next(value.trim());
  }

  close(): void {
    this.searchService.close();
  }

  async select(result: GlobalSearchResult): Promise<void> {
    this.close();
    await this.router.navigateByUrl(result.route);
  }

  labelFor(kind: string): string {
    return kind === 'note'
      ? 'Note'
      : kind === 'quest'
        ? 'Quest'
        : 'Library';
  }

  trackByResult(_: number, result: GlobalSearchResult): string {
    return `${result.kind}:${result.route}:${result.title}`;
  }
}
