export enum ReminderStatus {
  Pending = 0,
  Sent = 1,
  Failed = 2,
}

export interface ReminderDto {
  id: number;
  userId: number;
  eventId: number | null;
  documentId: number | null;
  reminderDate: string;
  sentAt: string | null;
  status: ReminderStatus;
}
