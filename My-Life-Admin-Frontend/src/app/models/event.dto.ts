export interface EventDto {
  id: number;
  userId: number;
  documentId: number | null;
  title: string;
  description: string | null;
  category: string;
  eventDate: string;
  createdAt: string;
}

export interface CreateEventDto {
  documentId: number | null;
  title: string;
  description: string | null;
  category: string;
  eventDate: string;
}

export type UpdateEventDto = CreateEventDto;
