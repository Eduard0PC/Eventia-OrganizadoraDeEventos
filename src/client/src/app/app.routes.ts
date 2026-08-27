import { Routes } from '@angular/router';
import { Home } from '@features/home/pages/home/home';
import { Login } from '@features/auth/pages/login/login';
import { Register } from '@features/auth/pages/register/register';
import { MisEventos } from '@features/eventos/pages/mis-eventos/mis-eventos';
import { Cotizaciones } from '@features/cotizaciones/pages/cotizaciones/cotizaciones';
import { Pagos } from '@features/pagos/pages/pagos/pagos';
import { Empleado } from '@features/empleado/pages/empleado/empleado';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'home', component: Home },
  { path: 'mis-eventos', component: MisEventos },
  { path: 'cotizaciones', component: Cotizaciones },
  { path: 'pagos', component: Pagos },
  { path: 'empleado', component: Empleado },
  { path: '**', redirectTo: 'login' },
];
