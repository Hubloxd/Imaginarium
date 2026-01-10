import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Użyj metody checkAuth, która upewni się, że sesja jest wczytana
  if (authService.checkAuth()) {
    return true;
  }

  // Jeśli użytkownik nie jest zalogowany, przekieruj do logowania
  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const loginGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Jeśli użytkownik jest już zalogowany, przekieruj do strony głównej
  if (authService.checkAuth()) {
    router.navigate(['/']);
    return false;
  }

  return true;
};