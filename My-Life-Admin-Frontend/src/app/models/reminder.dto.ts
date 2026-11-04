export enum ReminderStatus {
  Pending = 0,
  Sent = 1,
  Failed = 2,
  Skipped = 3,
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

export interface CreateReminderDto {
  eventId: number | null;
  documentId: number | null;
  reminderDate: string;
}

export interface UpdateReminderDto {
  eventId: number | null;
  documentId: number | null;
  reminderDate: string;
}
