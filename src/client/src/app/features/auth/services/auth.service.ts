import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap, timeout } from 'rxjs';
import { environment } from '@env/environment';
import { LoginRequest, LoginResponse } from '@core/models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      timeout(10000),
      tap((session) => localStorage.setItem('session', JSON.stringify(session))),
    );
  }
}
