import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar-client.html'
})
export class NavbarClient {

  nombreUsuario = '';

  constructor(private router: Router) {
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.cliente?.nombre) {
          this.nombreUsuario = session.cliente.nombre;
        } else if (session?.email) {
          this.nombreUsuario = session.email;
        }
      } catch (e) {
        console.error('Error parsing session', e);
      }
    }
    if (!this.nombreUsuario) {
      this.nombreUsuario = 'Usuario';
    }
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('session');
    this.router.navigate(['/login']);
  }

}