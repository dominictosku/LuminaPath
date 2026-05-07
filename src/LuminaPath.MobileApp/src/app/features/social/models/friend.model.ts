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
