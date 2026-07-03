import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { MisEventos } from './pages/mis-eventos/mis-eventos';
import { Cotizaciones } from './pages/cotizaciones/cotizaciones';
import { Pagos } from './pages/pagos/pagos';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'home', component: Home },
  { path: 'mis-eventos', component: MisEventos },
  { path: 'cotizaciones', component: Cotizaciones },
  { path: 'pagos', component: Pagos },
  { path: '**', redirectTo: 'login' },
];
