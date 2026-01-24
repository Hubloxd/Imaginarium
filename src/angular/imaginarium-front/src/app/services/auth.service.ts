import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environment/environment';

export interface User {
  id: string;
  email: string;
  username: string;
  createdAt?: string;
  lastLoginAt?: string;
}

export interface AuthResponse {
  id: string;
  email: string;
  username: string;
  accessToken: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/api/accounts`;
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly USER_KEY = 'user';

  currentUser = signal<User | null>(null);
  isAuthenticated = signal<boolean>(false);

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.loadUserFromStorage();
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, {
      email,
      password
    }).pipe(
      tap(response => {
        this.setToken(response.accessToken);
        this.setUser({
          id: response.id,
          email: response.email,
          username: response.username
        });
      })
    );
  }

  register(data: {
    email: string;
    username: string;
    password: string;
  }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(response => {
        this.setToken(response.accessToken);
        this.setUser({
          id: response.id,
          email: response.email,
          username: response.username
        });
      })
    );
  }

  logout(): void {
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
    this.isAuthenticated.set(true);
    console.log('Token zapisany w localStorage');
  }

  private setUser(user: User): void {
    const userJson = JSON.stringify(user);
    localStorage.setItem(this.USER_KEY, userJson);
    this.currentUser.set(user);
    console.log('Dane użytkownika zapisane w localStorage:', user);
  }

  private loadUserFromStorage(): void {
    const token = this.getAccessToken();
    const userStr = localStorage.getItem(this.USER_KEY);
    
    console.log('Wczytywanie sesji z localStorage:', { hasToken: !!token, hasUser: !!userStr });
    
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr);
        
        // Sprawdź czy token nie wygasł (podstawowa weryfikacja)
        if (this.isTokenValid(token)) {
          console.log('Token jest ważny, przywracanie sesji użytkownika:', user);
          this.currentUser.set(user);
          this.isAuthenticated.set(true);
        } else {
          console.log('Token wygasł, czyszczenie sesji');
          // Token wygasł, wyczyść sesję
          this.clearAuth();
        }
      } catch (e) {
        console.error('Błąd podczas wczytywania użytkownika z localStorage:', e);
        this.clearAuth();
      }
    } else {
      console.log('Brak tokenu lub danych użytkownika w localStorage');
      // Brak tokenu lub danych użytkownika
      this.isAuthenticated.set(false);
      this.currentUser.set(null);
    }
  }

  private isTokenValid(token: string): boolean {
    try {
      // Dekoduj token JWT (bez weryfikacji podpisu, tylko sprawdzenie exp)
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // Konwersja na milisekundy
      const now = Date.now();
      
      // Sprawdź czy token nie wygasł (z 5 sekundowym marginesem)
      return exp > (now + 5000);
    } catch (e) {
      console.error('Błąd podczas weryfikacji tokenu:', e);
      return false;
    }
  }

  private clearAuth(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
  }

  getUser(): User | null {
    return this.currentUser();
  }

  checkAuth(): boolean {
    const token = this.getAccessToken();
    const isAuth = this.isAuthenticated();
    
    // Jeśli mamy token, ale isAuthenticated jest false, spróbuj wczytać sesję
    if (token && !isAuth) {
      this.loadUserFromStorage();
      return this.isAuthenticated();
    }
    
    return isAuth && !!token;
  }
}