import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { Identity } from './identity/identity';

describe('App header', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  async function render(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows nothing about a person before one is chosen', async () => {
    expect((await render()).querySelector('.person')).toBeNull();
  });

  it('shows the name and hides Report for a non-manager', async () => {
    TestBed.inject(Identity).select({ id: 'soren', name: 'Søren Ærø', isOfficeManager: false });

    const header = await render();

    expect(header.querySelector('.person')?.textContent).toContain('Søren Ærø');
    expect(header.textContent).toContain('Switch person');
    expect(header.textContent).not.toContain('Report');
  });

  it('shows Report for the office manager', async () => {
    TestBed.inject(Identity).select({ id: 'anna', name: 'Anna Jensen', isOfficeManager: true });

    expect((await render()).textContent).toContain('Report');
  });
});
