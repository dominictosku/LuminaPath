export type FriendUser = {
  id: string;
  userName: string;
  fullName: string;
  email: string;
};

export type FriendshipStatus = 'Pending' | 'Accepted' | 'Declined';

export type Friendship = {
  id: number;
  status: FriendshipStatus;
  isIncoming: boolean;
  createdAt: string;
  respondedAt: string | null;
  user: FriendUser;
};

export type DirectMessage = {
  id: number;
  senderId: string;
  recipientId: string;
  content: string;
  sentAt: string;
  readAt: string | null;
};

export type FriendProfileStats = {
  games: number;
  animes: number;
  movies: number;
  series: number;
  totalItems: number;
  completedItems: number;
  activeItems: number;
  totalTrackedHours: number;
  averageRating: number | null;
};

export type FriendLibraryItem = {
  kind: 'games' | 'animes' | 'movies' | 'series' | string;
  mediaId: number;
  libraryEntryId: number;
  title: string;
  status: string;
  rating: number | null;
  timeSpend: number | null;
  startDate: string | null;
  endDate: string | null;
  imageUrl: string | null;
};

export type FriendActivityItem = {
  kind: string;
  verb: string;
  occurredAt: string | null;
  item: FriendLibraryItem;
};

export type FriendProfile = {
  user: FriendUser;
  stats: FriendProfileStats;
  nowPlaying: FriendLibraryItem[];
  recentCompletions: FriendLibraryItem[];
  activity: FriendActivityItem[];
};
