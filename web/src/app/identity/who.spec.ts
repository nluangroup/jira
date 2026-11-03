import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Identity } from './identity';
import { Who } from './who';

const employees = [
  { id: 'anna', name: 'Anna Jensen', isOfficeManager: true },
  { id: 'soren', name: 'Søren Ærø', isOfficeManager: false },
];

describe('Who', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('lists the employees from the API', async () => {
    const fixture = TestBed.createComponent(Who);
    TestBed.inject(HttpTestingController).expectOne('/api/employees').flush(employees);
    await fixture.whenStable();

    const buttons = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    ).map((button) => button.textContent?.trim());
    expect(buttons).toEqual(['Anna Jensen', 'Søren Ærø']);
  });

  it('stores the choice and goes to the month page', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(Who);
    TestBed.inject(HttpTestingController).expectOne('/api/employees').flush(employees);
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelectorAll('button')[1].click();

    expect(TestBed.inject(Identity).current()).toEqual(employees[1]);
    expect(JSON.parse(localStorage.getItem('lunch.employee')!)).toEqual(employees[1]);
    expect(navigate).toHaveBeenCalledWith(['month']);
  });
});
