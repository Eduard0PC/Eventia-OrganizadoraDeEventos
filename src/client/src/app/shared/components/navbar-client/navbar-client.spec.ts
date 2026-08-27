import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NavbarClient } from './navbar-client';

describe('NavbarClient', () => {
  let component: NavbarClient;
  let fixture: ComponentFixture<NavbarClient>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavbarClient],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarClient);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
