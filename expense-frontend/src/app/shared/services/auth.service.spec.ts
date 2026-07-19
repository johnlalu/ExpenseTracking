import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

function makeFakeJwt(payload: object): string {
  const body = btoa(JSON.stringify(payload)).replace(/=/g, '');
  return `eyJhbGciOiJIUzI1NiJ9.${body}.fake-sig`;
}

const FUTURE_TOKEN = makeFakeJwt({ sub: 'user-1', email: 'test@test.com', exp: 9999999999 });
const EXPIRED_TOKEN = makeFakeJwt({ sub: 'user-1', email: 'test@test.com', exp: 1 });

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/auth`;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
    localStorage.clear();
  });

  describe('refreshToken', () => {
    it('should send the stored refresh token in the request body', () => {
      localStorage.setItem('refreshToken', 'my-refresh-token');

      service.refreshToken().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'my-refresh-token' });
      req.flush({ accessToken: FUTURE_TOKEN, refreshToken: 'new-refresh', expiresIn: 3600 });
    });

    it('should send null when no refresh token is stored', () => {
      service.refreshToken().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/refresh`);
      expect(req.request.body).toEqual({ refreshToken: null });
      req.flush({ accessToken: FUTURE_TOKEN, refreshToken: 'new-refresh', expiresIn: 3600 });
    });

    it('should store new access token in sessionStorage on success', () => {
      service.refreshToken().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/refresh`);
      req.flush({ accessToken: FUTURE_TOKEN, refreshToken: 'new-refresh', expiresIn: 3600 });

      expect(sessionStorage.getItem('accessToken')).toBe(FUTURE_TOKEN);
    });

    it('should store new refresh token in localStorage on success', () => {
      service.refreshToken().subscribe();

      const req = httpMock.expectOne(`${apiUrl}/refresh`);
      req.flush({ accessToken: FUTURE_TOKEN, refreshToken: 'new-refresh-token', expiresIn: 3600 });

      expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no token stored', () => {
      expect(service.isAuthenticated()).toBe(false);
    });

    it('should return true when a valid non-expired token is stored', () => {
      sessionStorage.setItem('accessToken', FUTURE_TOKEN);
      expect(service.isAuthenticated()).toBe(true);
    });

    it('should return false when stored token is expired', () => {
      sessionStorage.setItem('accessToken', EXPIRED_TOKEN);
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('login', () => {
    it('should store tokens on successful login', () => {
      service.login({ email: 'test@test.com', password: 'pass' }).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/login`);
      req.flush({ accessToken: FUTURE_TOKEN, refreshToken: 'rt-123', expiresIn: 3600 });

      expect(sessionStorage.getItem('accessToken')).toBe(FUTURE_TOKEN);
      expect(localStorage.getItem('refreshToken')).toBe('rt-123');
    });
  });

  describe('logout', () => {
    it('should clear stored tokens', () => {
      sessionStorage.setItem('accessToken', FUTURE_TOKEN);
      localStorage.setItem('refreshToken', 'rt-123');

      service.logout();

      expect(sessionStorage.getItem('accessToken')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should clear currentUser', () => {
      service.logout();
      expect(service.getCurrentUser()).toBeNull();
    });
  });
});
