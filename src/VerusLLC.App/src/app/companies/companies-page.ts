import { Component, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { CompanyService } from './company.service';
import { ApiFailure, CompanyResponse, CompanySearchFilters } from './company.models';

export const NAME_MIN_LENGTH = 3;

export function absoluteWebUrl(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '').trim();

  if (!value) {
    return null;
  }

  let parsed: URL;

  try {
    parsed = new URL(value);
  } catch {
    return { webUrl: true };
  }

  const isWeb = parsed.protocol === 'http:' || parsed.protocol === 'https:';

  return isWeb && parsed.hostname.includes('.') ? null : { webUrl: true };
}

type ListState = 'loading' | 'ready' | 'empty' | 'error';

@Component({
  selector: 'app-companies-page',
  imports: [ReactiveFormsModule],
  templateUrl: './companies-page.html',
  styleUrl: './companies-page.css',
})
export class CompaniesPage {
  private readonly companyService = inject(CompanyService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly nameMinLength = NAME_MIN_LENGTH;

  protected readonly companies = signal<CompanyResponse[]>([]);
  protected readonly listState = signal<ListState>('loading');
  protected readonly listError = signal<ApiFailure | null>(null);

  protected readonly submitting = signal(false);
  protected readonly createError = signal<ApiFailure | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly hiddenByFilters = signal(false);

  protected readonly appliedFilters = signal<CompanySearchFilters>({});

  protected readonly hasActiveFilters = computed(() => {
    const filters = this.appliedFilters();

    return Boolean(filters.name || filters.domain || filters.search);
  });

  protected readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(NAME_MIN_LENGTH)]],
    websiteUrl: ['', [Validators.required, absoluteWebUrl]],
  });

  protected readonly filterForm = this.formBuilder.nonNullable.group({
    name: [''],
    domain: [''],
    search: [''],
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.listState.set('loading');
    this.listError.set(null);

    this.companyService.search(this.appliedFilters()).subscribe({
      next: (companies) => {
        this.companies.set(companies);
        this.listState.set(companies.length === 0 ? 'empty' : 'ready');
      },
      error: (failure: ApiFailure) => {
        this.companies.set([]);
        this.listError.set(failure);
        this.listState.set('error');
      },
    });
  }

  protected applyFilters(): void {
    this.appliedFilters.set({ ...this.filterForm.getRawValue() });
    this.hiddenByFilters.set(false);
    this.load();
  }

  protected clearFilters(): void {
    this.filterForm.reset({ name: '', domain: '', search: '' });
    this.appliedFilters.set({});
    this.hiddenByFilters.set(false);
    this.load();
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.successMessage.set(null);
    this.createError.set(null);
    this.hiddenByFilters.set(false);

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const request = this.createForm.getRawValue();

    this.submitting.set(true);
    this.createForm.disable({ emitEvent: false });

    this.companyService.create(request).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.createForm.enable({ emitEvent: false });
        this.createForm.reset({ name: '', websiteUrl: '' });
        this.successMessage.set(`${created.name} was registered.`);
        this.hiddenByFilters.set(this.hasActiveFilters());
        this.load();
      },
      error: (failure: ApiFailure) => {
        this.submitting.set(false);
        this.createForm.enable({ emitEvent: false });
        this.createError.set(failure);
      },
    });
  }

  protected fieldErrorsFor(field: string): string[] {
    const errors = this.createError()?.fieldErrors ?? {};
    const key = Object.keys(errors).find((candidate) => candidate.toLowerCase() === field.toLowerCase());

    return key ? errors[key] : [];
  }

  protected initialsOf(name: string): string {
    const words = name.split(/\s+/).filter(Boolean);
    const letters = words.slice(0, 2).map((word) => word[0] ?? '');

    return letters.join('').toUpperCase() || '?';
  }

  protected hostOf(websiteUrl: string): string {
    try {
      return new URL(websiteUrl).host.replace(/^www\./, '');
    } catch {
      return websiteUrl;
    }
  }

  protected get name() {
    return this.createForm.controls.name;
  }

  protected get websiteUrl() {
    return this.createForm.controls.websiteUrl;
  }
}
