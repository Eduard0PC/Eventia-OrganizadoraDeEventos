import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthLayout } from '../../components/auth-layout/auth-layout';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth';
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
      }),
    ).subscribe({
      next: () => {
        this.router.navigateByUrl('/home');
      },
      error: (err) => {
        if (err instanceof TimeoutError) {
          this.error = 'El servidor tardó demasiado en responder. Intenta de nuevo.';
          return;
        }

        if (err?.status === 0) {
          this.error = 'No se pudo conectar con el servidor. Verifica que el backend esté iniciado.';
          return;
        }

        this.error = 'Correo o contraseña incorrectos.';
      },
    });
  }
}
