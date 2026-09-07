import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthLayout } from '@shared/components/auth-layout/auth-layout';
import { AuthService } from '@features/auth/services/auth.service';
import { finalize, TimeoutError } from 'rxjs';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, AuthLayout, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  nombre = '';
  apellido = '';
  telefono = '';
  email = '';
  password = '';
  confirmPassword = '';

  error = '';
  exito = '';
  loading = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.loading) return;

    if (!this.nombre.trim() || !this.apellido.trim() || !this.email.trim() || !this.password) {
      this.error = 'Por favor completa todos los campos obligatorios.';
      return;
    }

    if (this.password.length < 6) {
      this.error = 'La contraseña debe tener al menos 6 caracteres.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error = 'Las contraseñas no coinciden.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.exito = '';

    const registerReq = {
      nombre: this.nombre.trim(),
      apellido: this.apellido.trim(),
      telefono: this.telefono.trim() || null,
      email: this.email.trim(),
      password: this.password,
      rol: 'cliente',
    };

    this.authService.register(registerReq).subscribe({
      next: () => {
        this.exito = '¡Registro exitoso! Iniciando sesión...';
        // Auto login right after registration
        this.authService.login({ email: registerReq.email, password: registerReq.password }).pipe(
          finalize(() => {
            this.loading = false;
          })
        ).subscribe({
          next: () => {
            this.router.navigateByUrl('/home');
          },
          error: () => {
            this.router.navigateByUrl('/login');
          }
        });
      },
      error: (err) => {
        this.loading = false;
        if (err instanceof TimeoutError) {
          this.error = 'El servidor tardó demasiado en responder. Intenta de nuevo.';
          return;
        }

        if (err?.status === 0) {
          this.error = 'No se pudo conectar con el servidor. Verifica que el backend esté iniciado.';
          return;
        }

        if (err?.error?.error) {
          this.error = err.error.error;
          return;
        }

        this.error = 'Ocurrió un error al registrar la cuenta. Por favor reintenta.';
      },
    });
  }
}

