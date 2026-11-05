import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Report } from './report';

const manager = { id: 'anna', name: 'Anna Jensen', isOfficeManager: true };
const employee = { id: 'soren', name: 'Søren Ærø', isOfficeManager: false };

const report = (month: string) => ({
  month,
  rows: [
    { employeeId: 'anna', name: 'Anna Jensen', lunchDays: 2, beverages: 5 },
    { employeeId: 'soren', name: 'Søren Ærø', lunchDays: 0, beverages: 0 },
  ],
  total: { lunchDays: 2, beverages: 5 },
});

function currentMonth(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Copenhagen',
    year: 'numeric',
    month: '2-digit',
  }).format(new Date());
}

describe('Report', () => {
  let httpTesting: HttpTestingController;

  function setup(user: typeof manager) {
    localStorage.setItem('lunch.employee', JSON.stringify(user));
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    httpTesting = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Report);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => localStorage.clear());
  afterEach(() => httpTesting.verify());

  it('shows_rows_and_total', async () => {
    const fixture = setup(manager);
    const month = currentMonth();
    httpTesting.expectOne(`/api/reports/${month}`).flush(report(month));
    await fixture.whenStable();

    const rows = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')).map(
      (tr) => Array.from(tr.querySelectorAll('th, td')).map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['Anna Jensen', '2', '5'],
      ['Søren Ærø', '0', '0'],
      ['Total', '2', '5'],
    ]);
  });

  it('month_picker_reloads', async () => {
    const fixture = setup(manager);
    httpTesting.expectOne(`/api/reports/${currentMonth()}`).flush(report(currentMonth()));
    await fixture.whenStable();

    const input = (fixture.nativeElement as HTMLElement).querySelector('input[type="month"]') as HTMLInputElement;
    input.value = '2026-03';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    httpTesting.expectOne('/api/reports/2026-03').flush(report('2026-03'));
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr').length).toBe(3);
  });

  it('shows_error_when_load_fails', async () => {
    const fixture = setup(manager);
    httpTesting
      .expectOne(`/api/reports/${currentMonth()}`)
      .flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
  });

  it('export_requests_csv', async () => {
    const createObjectURL = vi.fn().mockReturnValue('blob:csv');
    const revokeObjectURL = vi.fn();
    const original = { createObjectURL: URL.createObjectURL, revokeObjectURL: URL.revokeObjectURL };
    Object.assign(URL, { createObjectURL, revokeObjectURL });
    onTestFinished(() => {
      Object.assign(URL, original);
    });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    const fixture = setup(manager);
    const month = currentMonth();
    httpTesting.expectOne(`/api/reports/${month}`).flush(report(month));
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelector('button')!.click();
    const request = httpTesting.expectOne(`/api/reports/${month}/csv`);
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['x']));
    await fixture.whenStable();

    expect(createObjectURL).toHaveBeenCalled();
    expect(click).toHaveBeenCalled();
    await vi.waitFor(() => expect(revokeObjectURL).toHaveBeenCalledWith('blob:csv'));
    click.mockRestore();
  });

  it('export_failure_shows_message', async () => {
    const fixture = setup(manager);
    const month = currentMonth();
    httpTesting.expectOne(`/api/reports/${month}`).flush(report(month));
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelector('button')!.click();
    httpTesting
      .expectOne(`/api/reports/${month}/csv`)
      .flush(new Blob(['boom']), { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Could not export the CSV.');
  });

  it('non_manager_sees_message', async () => {
    const fixture = setup(employee);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('managers only');
    httpTesting.expectNone((req) => req.url.startsWith('/api/reports'));
  });
});
