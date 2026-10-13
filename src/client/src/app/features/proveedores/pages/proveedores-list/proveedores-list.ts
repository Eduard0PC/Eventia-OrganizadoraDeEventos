import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NavbarClient } from '@shared/components/navbar-client/navbar-client';
import { ToastComponent } from '@shared/components/toast/toast.component';
import { ToastService } from '@shared/components/toast/toast.service';
import { Proveedor, CategoriaProveedor } from '@core/models/proveedor.model';
import { ProveedorService } from '../../services/proveedor.service';
import { ProveedorCardComponent } from '../../components/proveedor-card/proveedor-card';
import { ProveedorServiciosModalComponent } from '../../components/proveedor-servicios-modal/proveedor-servicios-modal';
import { ProveedorFormModalComponent } from '../../components/proveedor-form-modal/proveedor-form-modal';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-proveedores-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NavbarClient,
    ToastComponent,
    ProveedorCardComponent,
    ProveedorServiciosModalComponent,
    ProveedorFormModalComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './proveedores-list.html'
})
export class ProveedoresListComponent implements OnInit {
  private readonly proveedorService = inject(ProveedorService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  proveedores: Proveedor[] = [];
  categorias: CategoriaProveedor[] = [];
  cargando = true;

  // Filtros
  busqueda = '';
  categoriaSeleccionadaId = 0; // 0 = Todas
  filtroPrecio = 'todos';
  filtroRating = 0; // 0 = Todos
  incluirInactivos = false;

  // Empleado / Admin status
  esEmpleado = false;

  // Modales
  proveedorVerServicios: Proveedor | null = null;
  proveedorEditando: Proveedor | null = null;
  mostrandoFormModal = false;

  ngOnInit(): void {
    this.detectarRolUsuario();
    if (!this.esEmpleado) {
      this.router.navigate(['/home']);
      return;
    }
    this.cargarCategorias();
    this.cargarProveedores();
  }

  detectarRolUsuario(): void {
    if (typeof localStorage !== 'undefined') {
      const sessionStr = localStorage.getItem('session');
      if (sessionStr) {
        try {
          const session = JSON.parse(sessionStr);
          // Si tiene datos de empleado o rol admin
          if (session?.empleado || session?.rol === 'admin' || session?.tipoUsuario === 'empleado') {
            this.esEmpleado = true;
          }
        } catch (e) {
          console.error('Error al parsear sesión:', e);
        }
      }
    }
  }

  cargarCategorias(): void {
    this.proveedorService.getCategorias(false).subscribe({
      next: (data) => {
        this.categorias = data || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar categorías:', err)
    });
  }

  cargarProveedores(): void {
    this.cargando = true;
    this.cdr.detectChanges();

    const catId = this.categoriaSeleccionadaId > 0 ? this.categoriaSeleccionadaId : undefined;

    this.proveedorService.getProveedores(catId, this.incluirInactivos).subscribe({
      next: (data) => {
        this.proveedores = data || [];
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar proveedores:', err);
        this.proveedores = [];
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  seleccionarCategoria(catId: number): void {
    this.categoriaSeleccionadaId = catId;
    this.cargarProveedores();
  }

  get proveedoresFiltrados(): Proveedor[] {
    const termino = this.busqueda.trim().toLowerCase();

    return this.proveedores.filter(prov => {
      // Búsqueda por texto (nombre, contacto, servicios, dirección)
      const coincideTexto = !termino ||
        prov.nombre.toLowerCase().includes(termino) ||
        (prov.contacto && prov.contacto.toLowerCase().includes(termino)) ||
        (prov.direccion && prov.direccion.toLowerCase().includes(termino)) ||
        prov.servicios.some(s => s.nombreServicio.toLowerCase().includes(termino));

      // Filtro Precio
      const coincidePrecio = this.filtroPrecio === 'todos' || prov.precioPromedio === this.filtroPrecio;

      // Filtro Rating
      const coincideRating = this.filtroRating === 0 || prov.rating >= this.filtroRating;

      return coincideTexto && coincidePrecio && coincideRating;
    });
  }

  abrirVerServicios(proveedor: Proveedor): void {
    if (proveedor && !proveedor.servicios) {
      proveedor.servicios = [];
    }
    this.proveedorVerServicios = proveedor;
    this.cdr.detectChanges();
  }

  cerrarVerServicios(): void {
    this.proveedorVerServicios = null;
    this.cdr.detectChanges();
  }

  recargarServiciosProveedor(): void {
    if (!this.proveedorVerServicios) return;
    const id = this.proveedorVerServicios.id;
    this.proveedorService.getProveedorById(id).subscribe({
      next: (prov) => {
        if (prov) {
          if (!prov.servicios) prov.servicios = [];
          this.proveedorVerServicios = prov;
          const idx = this.proveedores.findIndex(p => p.id === prov.id);
          if (idx !== -1) this.proveedores[idx] = prov;
          this.cdr.detectChanges();
        }
      }
    });
  }

  abrirNuevoProveedorForm(): void {
    this.proveedorEditando = null;
    this.mostrandoFormModal = true;
    this.cdr.detectChanges();
  }

  abrirEditarProveedor(proveedor: Proveedor): void {
    this.proveedorEditando = proveedor;
    this.mostrandoFormModal = true;
    this.cdr.detectChanges();
  }

  cerrarFormModal(): void {
    this.mostrandoFormModal = false;
    this.proveedorEditando = null;
    this.cdr.detectChanges();
  }

  onProveedorGuardado(): void {
    this.cerrarFormModal();
    this.cargarProveedores();
  }

  eliminarProveedor(proveedor: Proveedor): void {
    if (!confirm(`¿Estás seguro de eliminar el proveedor "${proveedor.nombre}"?`)) return;

    this.proveedorService.eliminarProveedor(proveedor.id).subscribe({
      next: () => {
        this.toastService.showSuccess('Proveedor eliminado correctamente');
        this.cargarProveedores();
      },
      error: (err) => {
        console.error('Error al eliminar proveedor:', err);
        this.toastService.showError('No se pudo eliminar el proveedor.');
      }
    });
  }
}
