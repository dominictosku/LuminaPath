export type ViewMode = 'grid' | 'list';
export type OwnershipFilter = 'all' | 'mine' | 'catalog';
export type ReleaseDateFilter = 'all' | 'released' | 'upcoming' | 'this-year' | 'last-year' | 'custom';
export type SortMode = 'title' | 'release-desc' | 'release-asc' | 'rating-desc' | 'remaining-asc' | 'recently-added' | 'best-finish' | 'tracked-desc';
export type SmartFilter = 'none' | 'short' | 'abandoned' | 'best';

export type LibraryFilterPreset = {
  id: string;
  name: string;
  mediaModeId: string;
  searchTerm: string;
  ownershipFilter: OwnershipFilter;
  statusFilter: string;
  platformFilter: string;
  releaseDateFilter: ReleaseDateFilter;
  releaseDateFrom: string;
  releaseDateTo: string;
  sortMode: SortMode;
  smartFilter: SmartFilter;
};
