import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Identity } from './identity';
import { identityInterceptor } from './identity.interceptor';

describe('identityInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let identity: Identity;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([identityInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    identity = TestBed.inject(Identity);
    router = TestBed.inject(Router);
  });

  afterEach(() => controller.verify());

  it('adds the X-Employee-Id header for the chosen person', () => {
    identity.select({ id: 'anna', name: 'Anna Jensen', isOfficeManager: true });

    http.get('/api/me').subscribe();

    const request = controller.expectOne('/api/me');
    expect(request.request.headers.get('X-Employee-Id')).toBe('anna');
    request.flush({});
  });

  it('sends no header before anyone is chosen', () => {
    http.get('/api/employees').subscribe();

    const request = controller.expectOne('/api/employees');
    expect(request.request.headers.has('X-Employee-Id')).toBe(false);
    request.flush([]);
  });

  it('forgets the person and navigates to who on a 401', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    identity.select({ id: 'ghost', name: 'Ghost', isOfficeManager: false });

    http.get('/api/me').subscribe({ error: () => undefined });
    controller.expectOne('/api/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).toHaveBeenCalledWith(['who']);
    expect(identity.current()).toBeNull();
    expect(localStorage.getItem('lunch.employee')).toBeNull();
  });
});
