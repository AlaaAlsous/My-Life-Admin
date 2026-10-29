export interface DocumentDto {
  id: number;
  title: string;
  fileName: string;
  description: string | null;
  category: string | null;
  expiryDate: string | null;
  uploadedAt: string;
}

export interface CreateDocumentDto {
  title: string;
  description?: string | null;
  category?: string | null;
  expiryDate?: string | null;
  file: File;
}

export interface UpdateDocumentDto {
  title: string;
  description: string | null;
  category: string | null;
  expiryDate: string | null;
}

export interface DocumentAnalysisDto {
  title: string | null;
  category: string | null;
  expiryDate: string | null;
  description: string | null;
}

