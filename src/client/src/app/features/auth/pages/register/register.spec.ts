import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { throwError } from 'rxjs';
import { Register } from './register';
import { AuthService } from '@features/auth/services/auth.service';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let authService: AuthService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('debe mostrar "imposible accesar en este momento, intentalo mas tarde" y quitar loading cuando falla la base de datos o servidor (500)', () => {
    component.nombre = 'Juan';
    component.apellido = 'Pérez';
    component.email = 'juan@ejemplo.com';
    component.password = '123456';
    component.confirmPassword = '123456';

    vi.spyOn(authService, 'register').mockReturnValue(
      throwError(() => ({ status: 500, error: { error: 'imposible accesar en este momento, intentalo mas tarde' } }))
    );

    component.submit();

    expect(component.error).toBe('imposible accesar en este momento, intentalo mas tarde');
    expect(component.loading).toBe(false);
  });

  it('debe mostrar "imposible accesar en este momento, intentalo mas tarde" y quitar loading cuando no hay conexión (status 0)', () => {
    component.nombre = 'Juan';
    component.apellido = 'Pérez';
    component.email = 'juan@ejemplo.com';
    component.password = '123456';
    component.confirmPassword = '123456';

    vi.spyOn(authService, 'register').mockReturnValue(
      throwError(() => ({ status: 0 }))
    );

    component.submit();

    expect(component.error).toBe('imposible accesar en este momento, intentalo mas tarde');
    expect(component.loading).toBe(false);
  });

  it('debe mostrar el error específico cuando el correo ya existe', () => {
    component.nombre = 'Juan';
    component.apellido = 'Pérez';
    component.email = 'existente@ejemplo.com';
    component.password = '123456';
    component.confirmPassword = '123456';

    vi.spyOn(authService, 'register').mockReturnValue(
      throwError(() => ({ status: 400, error: { error: 'El correo electrónico ya se encuentra registrado.' } }))
    );

    component.submit();

    expect(component.error).toBe('El correo electrónico ya se encuentra registrado.');
    expect(component.loading).toBe(false);
  });
});
