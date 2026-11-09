import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { throwError } from 'rxjs';
import { Login } from './login';
import { AuthService } from '@features/auth/services/auth.service';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let authService: AuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('no debe incluir el link de olvidaste tu contraseña', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('a[routerLink="/forgot-password"]')).toBeNull();
    expect(compiled.textContent).not.toContain('¿Olvidaste tu contraseña?');
  });

  it('debe mostrar mensaje "imposible accesar en este momento, intentalo mas tarde" cuando falla la base de datos o servidor (500)', () => {
    component.email = 'test@ejemplo.com';
    component.password = '123456';
    vi.spyOn(authService, 'login').mockReturnValue(
      throwError(() => ({ status: 500, error: { error: 'imposible accesar en este momento, intentalo mas tarde' } }))
    );

    component.submit();

    expect(component.error).toBe('imposible accesar en este momento, intentalo mas tarde');
    expect(component.loading).toBe(false);
  });

  it('debe mostrar mensaje "imposible accesar en este momento, intentalo mas tarde" cuando no hay conexion (status 0)', () => {
    component.email = 'test@ejemplo.com';
    component.password = '123456';
    vi.spyOn(authService, 'login').mockReturnValue(
      throwError(() => ({ status: 0 }))
    );

    component.submit();

    expect(component.error).toBe('imposible accesar en este momento, intentalo mas tarde');
    expect(component.loading).toBe(false);
  });

  it('debe mostrar mensaje "Correo o contraseña incorrectos." en credenciales invalidas (401)', () => {
    component.email = 'test@ejemplo.com';
    component.password = '123456';
    vi.spyOn(authService, 'login').mockReturnValue(
      throwError(() => ({ status: 401, error: { error: 'Correo o contraseña incorrectos.' } }))
    );

    component.submit();

    expect(component.error).toBe('Correo o contraseña incorrectos.');
    expect(component.loading).toBe(false);
  });
});
