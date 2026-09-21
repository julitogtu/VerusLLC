export interface CompanyResponse {
  id: string;
  name: string;
  websiteUrl: string;
}

export interface CreateCompanyRequest {
  name: string;
  websiteUrl: string;
}

export interface CompanySearchFilters {
  name?: string;
  domain?: string;
  search?: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}

export interface ApiFailure {
  message: string;
  fieldErrors: Record<string, string[]>;
  status: number | null;
  correlationId: string | null;
}
