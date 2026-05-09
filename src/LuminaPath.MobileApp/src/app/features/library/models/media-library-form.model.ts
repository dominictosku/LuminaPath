export type MediaLibraryForm = {
  status: number;
  timeSpend: number | null;
  rating: number | null;
  startDate: string;
  endDate: string;
  currentEpisode: number | null;
};

export type MediaStatusOption = {
  label: string;
  value: number;
};
