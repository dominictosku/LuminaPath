import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PsnTrophyTotals } from '../../services/statistic.service';

/**
 * Per-tier display data the template iterates over so the SVG +
 * gradients + classes stay declarative. `accent` doubles as the
 * gradient mid-stop colour and the count chip border.
 */
interface TierEntry {
  /** Used for the gradient `id` attribute and the data-tier hook. */
  readonly key: 'bronze' | 'silver' | 'gold' | 'platinum';
  readonly label: string;
  /** CSS colour for the upper highlight of the metallic gradient. */
  readonly highlight: string;
  /** CSS colour for the mid-band of the metallic gradient. */
  readonly accent: string;
  /** CSS colour for the lower shadow of the metallic gradient. */
  readonly shadow: string;
  readonly count: number;
}

/**
 * PSN trophy panel — four metallic-tinted trophy cups (bronze, silver,
 * gold, platinum) with the user's earned count under each.
 *
 * Why a hand-rolled SVG rather than an asset / icon library:
 *   - PSN's actual trophy icons are copyrighted; shipping a look-alike
 *     keeps us off the licensing wire.
 *   - Each tier needs its own metallic gradient — easier to author one
 *     SVG template and parameterise the gradient stops than to manage
 *     four PNG variants.
 *   - The platinum tier carries a star, the others don't — trivial via
 *     a conditional render in the template, awkward as separate assets.
 */
@Component({
  selector: 'app-statistic-psn-trophies',
  templateUrl: './statistic-psn-trophies.component.html',
  styleUrls: ['./statistic-psn-trophies.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticPsnTrophiesComponent {
  readonly totals = input.required<PsnTrophyTotals>();

  /**
   * Per-tier rendering data. Order matters — bronze → platinum reads
   * left-to-right as "most common → rarest", matching PSN's own UI.
   * Colour stops were tuned to read as "metal" against the dark surface
   * (low-saturation highlights + saturated mid + dark shadow).
   */
  protected readonly tiers = computed<TierEntry[]>(() => {
    const t = this.totals();
    return [
      {
        key: 'bronze',
        label: 'Bronze',
        highlight: '#f5b988',
        accent: '#cd7f32',
        shadow: '#7a4a1c',
        count: t.bronze,
      },
      {
        key: 'silver',
        label: 'Silver',
        highlight: '#f5f6f9',
        accent: '#c0c5cf',
        shadow: '#7c8493',
        count: t.silver,
      },
      {
        key: 'gold',
        label: 'Gold',
        highlight: '#fff4a3',
        accent: '#f5c43c',
        shadow: '#b88a14',
        count: t.gold,
      },
      {
        key: 'platinum',
        label: 'Platinum',
        // PSN's actual platinum is a distinctive teal-cyan, NOT silver —
        // matches their UI exactly so users recognise the tier instantly.
        highlight: '#cdf0f5',
        accent: '#5cb7c6',
        shadow: '#1b6f7d',
        count: t.platinum,
      },
    ];
  });

  protected readonly hasAny = computed(() => this.totals().total > 0);
}
