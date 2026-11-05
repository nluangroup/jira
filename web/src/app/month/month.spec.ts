import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Month } from './month';

describe('Month', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date('2026-11-03T11:00:00Z'));
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    vi.useRealTimers();
  });

  async function load(lunchDays: string[] = [], month = '2026-11'): Promise<ComponentFixture<Month>> {
    const fixture = TestBed.createComponent(Month);
    await settle(fixture);
    httpTesting.expectOne(`/api/me/months/${month}`).flush({ month, lunchDays, beverages: 0 });
    await settle(fixture);
    return fixture;
  }

  async function settle(fixture: ComponentFixture<Month>): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function root(fixture: ComponentFixture<Month>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function day(fixture: ComponentFixture<Month>, date: string): HTMLButtonElement {
    return root(fixture).querySelector<HTMLButtonElement>(`button[data-date="${date}"]`)!;
  }

  it('renders_monday_first_grid', async () => {
    const fixture = await load();
    const el = root(fixture);

    expect(el.querySelector('h1')?.textContent).toContain('November 2026');
    const weekdays = Array.from(el.querySelectorAll('.weekday')).map((w) => w.textContent?.trim());
    expect(weekdays).toEqual(['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']);
    expect(el.querySelectorAll('.blank').length).toBe(6);
    expect(el.querySelectorAll('button[data-date]').length).toBe(30);
    expect(el.querySelector('.grid')!.children[7 + 6].textContent?.trim()).toBe('1');
  });

  it('shows_ticked_days_from_api', async () => {
    const fixture = await load(['2026-11-02']);

    expect(day(fixture, '2026-11-02').getAttribute('aria-pressed')).toBe('true');
    expect(day(fixture, '2026-11-03').getAttribute('aria-pressed')).toBe('false');
  });

  it('tick_calls_put_and_marks_day', async () => {
    const fixture = await load();

    day(fixture, '2026-11-03').click();
    httpTesting.expectOne({ method: 'PUT', url: '/api/me/lunch-days/2026-11-03' }).flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);

    expect(day(fixture, '2026-11-03').getAttribute('aria-pressed')).toBe('true');
  });

  it('untick_calls_delete', async () => {
    const fixture = await load(['2026-11-03']);

    day(fixture, '2026-11-03').click();
    httpTesting.expectOne({ method: 'DELETE', url: '/api/me/lunch-days/2026-11-03' }).flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);

    expect(day(fixture, '2026-11-03').getAttribute('aria-pressed')).toBe('false');
  });

  it('weekend_and_future_days_are_disabled', async () => {
    const fixture = await load();

    expect(day(fixture, '2026-11-01').disabled).toBe(true);
    expect(day(fixture, '2026-11-07').disabled).toBe(true);
    expect(day(fixture, '2026-11-04').disabled).toBe(true);
    expect(day(fixture, '2026-11-02').disabled).toBe(false);
    expect(day(fixture, '2026-11-03').disabled).toBe(false);
  });

  it('previous_and_next_month_reload', async () => {
    const fixture = await load();
    const buttons = () => root(fixture).querySelectorAll<HTMLButtonElement>('button.nav');

    buttons()[0].click();
    httpTesting.expectOne('/api/me/months/2026-10').flush({ month: '2026-10', lunchDays: [], beverages: 0 });
    await settle(fixture);
    expect(root(fixture).querySelector('h1')?.textContent).toContain('October 2026');
    expect(day(fixture, '2026-10-30').disabled).toBe(false);

    buttons()[1].click();
    httpTesting.expectOne('/api/me/months/2026-11').flush({ month: '2026-11', lunchDays: [], beverages: 0 });
    await settle(fixture);
    buttons()[1].click();
    httpTesting.expectOne('/api/me/months/2026-12').flush({ month: '2026-12', lunchDays: [], beverages: 0 });
    await settle(fixture);
    expect(root(fixture).querySelector('h1')?.textContent).toContain('December 2026');
    expect(day(fixture, '2026-12-01').disabled).toBe(true);
  });

  it('api_error_shows_message', async () => {
    const fixture = TestBed.createComponent(Month);
    await settle(fixture);
    httpTesting.expectOne('/api/me/months/2026-11').flush(null, { status: 500, statusText: 'Server Error' });
    await settle(fixture);

    expect(root(fixture).querySelector('[role="alert"]')?.textContent).toContain('Could not load your month.');
  });
});
