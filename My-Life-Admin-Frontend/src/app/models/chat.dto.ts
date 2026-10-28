export interface SendChatMessageDto {
  message: string;
}

export interface ChatMessageDto {
  id: number;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

export interface ChatReplyDto {
  userMessage: ChatMessageDto;
  reply: ChatMessageDto;
}
