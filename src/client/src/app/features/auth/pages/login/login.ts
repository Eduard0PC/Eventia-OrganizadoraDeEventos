import { Component, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthLayout } from '@shared/components/auth-layout/auth-layout';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@features/auth/services/auth.service';
import { finalize, TimeoutError } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [AuthLayout, RouterLink, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  submit(): void {
    if (!this.email || !this.password || this.loading) {
      this.error = 'Ingresa tu correo y contraseña.';
      return;
    }

    this.loading = true;
    this.error = '';

    this.authService.login({ email: this.email, password: this.password }).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      }),
    ).subscribe({
      next: (session) => {
        const role = session?.rol?.toLowerCase();
        if (role === 'empleado' || role === 'admin') {
          this.router.navigateByUrl('/empleado');
        } else {
          this.router.navigateByUrl('/home');
        }
      },
      error: (err) => {
        if (err?.status === 401) {
          this.error = err?.error?.error || 'Correo o contraseña incorrectos.';
        } else if (err?.error?.error) {
          this.error = err.error.error;
        } else if (
          err?.name === 'TimeoutError' ||
          err instanceof TimeoutError ||
          err?.status === 0 ||
          (err?.status && err.status >= 500)
        ) {
          this.error = 'imposible accesar en este momento, intentalo mas tarde';
        } else {
          this.error = 'imposible accesar en este momento, intentalo mas tarde';
        }
        this.cdr.detectChanges();
      },
    });
  }
}
