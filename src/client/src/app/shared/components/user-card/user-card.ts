import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-card.html',
  styleUrl: './user-card.css'
})
export class UserCard implements OnInit {
  @Input() nombre?: string;
  @Input() rol?: string;
  @Input() showLogout = true;

  userName = '';
  userRole = '';

  constructor(private readonly router: Router) {}

  ngOnInit(): void {
    this.loadUserData();
  }

  loadUserData(): void {
    if (this.nombre) {
      this.userName = this.nombre;
    }
    if (this.rol) {
      this.userRole = this.formatRole(this.rol);
    }

    if (typeof localStorage !== 'undefined' && (!this.userName || !this.userRole)) {
      const sessionStr = localStorage.getItem('session');
      if (sessionStr) {
        try {
          const session = JSON.parse(sessionStr);

          if (!this.userName) {
            if (session?.empleado?.nombre) {
              const apellido = session.empleado.apellido ? ` ${session.empleado.apellido}` : '';
              this.userName = `${session.empleado.nombre}${apellido}`;
            } else if (session?.cliente?.nombre) {
              const apellido = session.cliente.apellido ? ` ${session.cliente.apellido}` : '';
              this.userName = `${session.cliente.nombre}${apellido}`;
            } else if (session?.email) {
              const handle = session.email.split('@')[0];
              this.userName = handle.charAt(0).toUpperCase() + handle.slice(1);
            } else {
              this.userName = 'Usuario';
            }
          }

          if (!this.userRole && session?.rol) {
            this.userRole = this.formatRole(session.rol);
          }
        } catch (e) {
          console.error('Error parsing user session in UserCard', e);
        }
      }
    }

    if (!this.userName) this.userName = 'Usuario';
    if (!this.userRole) this.userRole = 'Empleado';
  }

  private formatRole(rolStr: string): string {
    const lower = rolStr.trim().toLowerCase();
    if (lower === 'empleado') return 'Empleado';
    if (lower === 'admin' || lower === 'administrador') return 'Administrador';
    if (lower === 'cliente') return 'Cliente';
    return rolStr.charAt(0).toUpperCase() + rolStr.slice(1);
  }

  logout(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('token');
      localStorage.removeItem('session');
    }
    this.router.navigate(['/login']);
  }
}
