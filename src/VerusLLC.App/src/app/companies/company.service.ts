import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_BASE_URL } from '../core/api.config';
import {
  ApiFailure,
  CompanyResponse,
  CompanySearchFilters,
  CreateCompanyRequest,
  ProblemDetails,
} from './company.models';

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get endpoint(): string {
    return `${this.baseUrl}/companies`;
  }

  search(filters: CompanySearchFilters = {}): Observable<CompanyResponse[]> {
    let params = new HttpParams();

    for (const key of ['name', 'domain', 'search'] as const) {
      const value = filters[key]?.trim();

      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http
      .get<CompanyResponse[]>(this.endpoint, { params })
      .pipe(catchError((error) => throwError(() => toApiFailure(error))));
  }

  getById(id: string): Observable<CompanyResponse> {
    return this.http
      .get<CompanyResponse>(`${this.endpoint}/${encodeURIComponent(id)}`)
      .pipe(catchError((error) => throwError(() => toApiFailure(error))));
  }

  create(request: CreateCompanyRequest): Observable<CompanyResponse> {
    return this.http
      .post<CompanyResponse>(this.endpoint, request)
      .pipe(catchError((error) => throwError(() => toApiFailure(error))));
  }
}

export function toApiFailure(error: unknown): ApiFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return {
      message: 'Unexpected error. Please try again.',
      fieldErrors: {},
      status: null,
      correlationId: null,
    };
  }

  if (error.status === 0) {
    return {
      message: 'Cannot reach the API. Check that the backend is running.',
      fieldErrors: {},
      status: 0,
      correlationId: null,
    };
  }

  const problem = (error.error ?? {}) as ProblemDetails;
  const fieldErrors = problem.errors ?? {};
  const fieldMessages = Object.values(fieldErrors).flat();

  const message =
    problem.detail?.trim() ||
    fieldMessages[0] ||
    problem.title?.trim() ||
    `Request failed with status ${error.status}.`;

  return {
    message,
    fieldErrors,
    status: error.status,
    correlationId: problem.correlationId ?? null,
  };
}
