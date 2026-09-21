import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Proveedor, ProveedorServicio, CrearProveedorServicioRequest } from '@core/models/proveedor.model';
import { ProveedorService } from '../../services/proveedor.service';
import { ToastService } from '@shared/components/toast/toast.service';
import { IconEditComponent, IconTrashComponent } from '@shared/components/icons';

@Component({
  selector: 'app-proveedor-servicios-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, IconEditComponent, IconTrashComponent],
  templateUrl: './proveedor-servicios-modal.html'
})
export class ProveedorServiciosModalComponent {
  private readonly proveedorService = inject(ProveedorService);
  private readonly toastService = inject(ToastService);

  @Input({ required: true }) proveedor!: Proveedor;
  @Input() esEmpleado = false;

  @Output() cerrar = new EventEmitter<void>();
  @Output() serviciosActualizados = new EventEmitter<void>();

  // Form para crear/editar servicio
  mostrandoFormServicio = false;
  servicioEditandoId: number | null = null;

  nombreServicio = '';
  descripcionServicio = '';
  costoServicio = 0;
  unidadServicio = 'paquete';
  guardandoServicio = false;

  abrirNuevoServicioForm(): void {
    this.servicioEditandoId = null;
    this.nombreServicio = '';
    this.descripcionServicio = '';
    this.costoServicio = 0;
    this.unidadServicio = 'paquete';
    this.mostrandoFormServicio = true;
  }

  abrirEditarServicio(servicio: ProveedorServicio): void {
    this.servicioEditandoId = servicio.id;
    this.nombreServicio = servicio.nombreServicio;
    this.descripcionServicio = servicio.descripcion || '';
    this.costoServicio = servicio.costo;
    this.unidadServicio = servicio.unidad || 'paquete';
    this.mostrandoFormServicio = true;
  }

  cancelarFormServicio(): void {
    this.mostrandoFormServicio = false;
    this.servicioEditandoId = null;
  }

  guardarServicio(): void {
    if (!this.nombreServicio.trim() || this.costoServicio < 0 || this.guardandoServicio) {
      this.toastService.showError('Ingresa un nombre de servicio y costo válido.');
      return;
    }

    this.guardandoServicio = true;

    const payload: CrearProveedorServicioRequest = {
      nombreServicio: this.nombreServicio.trim(),
      descripcion: this.descripcionServicio.trim() || undefined,
      costo: this.costoServicio,
      unidad: this.unidadServicio.trim() || 'paquete',
      activo: true
    };

    if (this.servicioEditandoId) {
      this.proveedorService.actualizarServicio(this.servicioEditandoId, payload).subscribe({
        next: () => {
          this.guardandoServicio = false;
          this.toastService.showSuccess('Servicio actualizado con éxito');
          this.mostrandoFormServicio = false;
          this.serviciosActualizados.emit();
        },
        error: (err) => {
          this.guardandoServicio = false;
          console.error('Error al actualizar servicio:', err);
          this.toastService.showError('Error al actualizar el servicio.');
        }
      });
    } else {
      this.proveedorService.crearServicio(this.proveedor.id, payload).subscribe({
        next: () => {
          this.guardandoServicio = false;
          this.toastService.showSuccess('Servicio creado con éxito');
          this.mostrandoFormServicio = false;
          this.serviciosActualizados.emit();
        },
        error: (err) => {
          this.guardandoServicio = false;
          console.error('Error al crear servicio:', err);
          this.toastService.showError('Error al crear el servicio.');
        }
      });
    }
  }

  eliminarServicio(servicioId: number): void {
    if (!confirm('¿Estás seguro de eliminar este servicio?')) return;

    this.proveedorService.eliminarServicio(servicioId).subscribe({
      next: () => {
        this.toastService.showSuccess('Servicio eliminado');
        this.serviciosActualizados.emit();
      },
      error: (err) => {
        console.error('Error al eliminar servicio:', err);
        this.toastService.showError('Error al eliminar el servicio.');
      }
    });
  }

  onCerrarModal(): void {
    this.cerrar.emit();
  }
}
