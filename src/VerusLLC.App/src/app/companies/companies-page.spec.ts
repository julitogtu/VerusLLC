import { FormControl } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { absoluteWebUrl } from './companies-page';
import { toApiFailure } from './company.service';

describe('absoluteWebUrl', () => {
  it.each(['https://www.microsoft.com', 'http://contoso.com', 'https://contoso.com:8443/about'])(
    'accepts %s',
    (value) => {
      expect(absoluteWebUrl(new FormControl(value))).toBeNull();
    },
  );

  it.each(['contoso.com', '/relative', 'ftp://contoso.com', 'https://localhost', 'not a url'])(
    'rejects %s',
    (value) => {
      expect(absoluteWebUrl(new FormControl(value))).toEqual({ webUrl: true });
    },
  );

  it('leaves an empty value to the required validator', () => {
    expect(absoluteWebUrl(new FormControl(''))).toBeNull();
  });
});

describe('toApiFailure', () => {
  it('surfaces the ProblemDetails detail from a domain rejection', () => {
    const response = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'company.name_host.mismatch',
        detail: 'Company name is not related to the host of the website URL.',
        correlationId: 'abc-123',
      },
    });

    const failure = toApiFailure(response);

    expect(failure.message).toBe('Company name is not related to the host of the website URL.');
    expect(failure.correlationId).toBe('abc-123');
    expect(failure.status).toBe(400);
  });

  it('surfaces per-field errors from a ValidationProblemDetails', () => {
    const response = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: { Name: ['The Name field is required.'] },
      },
    });

    const failure = toApiFailure(response);

    expect(failure.fieldErrors['Name']).toEqual(['The Name field is required.']);
    expect(failure.message).toBe('The Name field is required.');
  });

  it('reports an unreachable API distinctly', () => {
    const failure = toApiFailure(new HttpErrorResponse({ status: 0 }));

    expect(failure.status).toBe(0);
    expect(failure.message).toContain('Cannot reach the API');
  });
});
