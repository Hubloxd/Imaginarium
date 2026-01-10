import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Pobierz token
  const token = authService.getAccessToken();
  
  // Dodaj token do nagłówka jeśli istnieje
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Jeśli błąd 401 (Unauthorized), spróbuj odświeżyć token
      if (error.status === 401 && token) {
        return authService.refreshToken().pipe(
          switchMap((response) => {
            // Ponów żądanie z nowym tokenem
            const clonedReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${response.access}`
              }
            });
            return next(clonedReq);
          }),
          catchError((refreshError) => {
            // Jeśli odświeżanie tokenu nie powiodło się, wyloguj użytkownika
            authService.logout().subscribe();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};