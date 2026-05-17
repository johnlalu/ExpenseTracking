import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AuthResponse, LoginRequest, RegisterRequest, JwtPayload } from '../models/auth.model';
import { environment } from '../../../environments/environment';

/**
 * Authentication service managing user login/registration and token management.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private currentUserSubject = new BehaviorSubject<JwtPayload | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadStoredToken();
  }

  /**
   * Register a new user.
   */
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /**
   * Login existing user.
   */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /**
   * Refresh access token using refresh token.
   */
  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /**
   * Logout current user.
   */
  logout(): void {
    sessionStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.currentUserSubject.next(null);
  }

  /**
   * Get stored access token.
   */
  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken');
  }

  /**
   * Get refresh token.
   */
  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  /**
   * Check if user is authenticated.
   */
  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    return token !== null && !this.isTokenExpired(token);
  }

  /**
   * Get current user.
   */
  getCurrentUser(): JwtPayload | null {
    return this.currentUserSubject.value;
  }

  /**
   * Store tokens in session/local storage.
   */
  private storeTokens(response: AuthResponse): void {
    if (response.accessToken) {
      sessionStorage.setItem('accessToken', response.accessToken);
      const payload = this.decodeToken(response.accessToken);
      this.currentUserSubject.next(payload);
    }
    if (response.refreshToken) {
      localStorage.setItem('refreshToken', response.refreshToken);
    }
  }

  /**
   * Load stored token from session storage.
   */
  private loadStoredToken(): void {
    const token = this.getAccessToken();
    if (token && !this.isTokenExpired(token)) {
      const payload = this.decodeToken(token);
      this.currentUserSubject.next(payload);
    }
  }

  /**
   * Decode JWT token payload.
   */
  private decodeToken(token: string): JwtPayload {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join(''));
    return JSON.parse(jsonPayload);
  }

  /**
   * Check if token is expired.
   */
  private isTokenExpired(token: string): boolean {
    try {
      const payload = this.decodeToken(token);
      const expirationTime = payload.exp * 1000;
      return Date.now() >= expirationTime;
    } catch {
      return true;
    }
  }
}
