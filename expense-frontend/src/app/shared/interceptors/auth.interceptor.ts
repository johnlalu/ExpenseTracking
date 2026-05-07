import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Interceptor for JWT token authentication.
 * Attaches token to requests and handles token refresh on 401.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;

  constructor(private authService: AuthService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Don't attach token to auth endpoints
    if (req.url.includes('/api/auth/')) {
      return next.handle(req);
    }

    const token = this.authService.getAccessToken();
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(req).pipe(
      catchError(error => {
        if (error instanceof HttpErrorResponse && error.status === 401 && !this.isRefreshing) {
          this.isRefreshing = true;
          const refreshToken = this.authService.getRefreshToken();

          if (refreshToken) {
            return this.authService.refreshToken().pipe(
              switchMap(response => {
                this.isRefreshing = false;
                const newToken = response.accessToken;
                if (newToken) {
                  req = req.clone({
                    setHeaders: {
                      Authorization: `Bearer ${newToken}`
                    }
                  });
                  return next.handle(req);
                }
                return throwError(() => error);
              }),
              catchError(err => {
                this.isRefreshing = false;
                this.authService.logout();
                return throwError(() => err);
              })
            );
          }

          this.authService.logout();
        }

        return throwError(() => error);
      })
    );
  }
}
