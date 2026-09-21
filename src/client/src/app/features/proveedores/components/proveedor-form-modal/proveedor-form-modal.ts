import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Proveedor, CategoriaProveedor, CrearProveedorRequest, ActualizarProveedorRequest } from '@core/models/proveedor.model';
import { ProveedorService } from '../../services/proveedor.service';
import { ToastService } from '@shared/components/toast/toast.service';

@Component({
  selector: 'app-proveedor-form-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './proveedor-form-modal.html'
})
export class ProveedorFormModalComponent implements OnInit {
  private readonly proveedorService = inject(ProveedorService);
  private readonly toastService = inject(ToastService);

  @Input() proveedorEditando: Proveedor | null = null;
  @Input() categorias: CategoriaProveedor[] = [];

  @Output() cerrar = new EventEmitter<void>();
  @Output() guardado = new EventEmitter<void>();

  categoriaId = 0;
  nombre = '';
  contacto = '';
  email = '';
  telefono = '';
  direccion = '';
  sitioWeb = '';
  rfc = '';
  rating = 5;
  precioPromedio = '$$$';
  activo = true;

  guardando = false;

  ngOnInit(): void {
    if (this.categorias.length > 0 && !this.categoriaId) {
      this.categoriaId = this.categorias[0].id;
    }

    if (this.proveedorEditando) {
      this.categoriaId = this.proveedorEditando.categoriaId;
      this.nombre = this.proveedorEditando.nombre;
      this.contacto = this.proveedorEditando.contacto || '';
      this.email = this.proveedorEditando.email || '';
      this.telefono = this.proveedorEditando.telefono || '';
      this.direccion = this.proveedorEditando.direccion || '';
      this.sitioWeb = this.proveedorEditando.sitioWeb || '';
      this.rfc = this.proveedorEditando.rfc || '';
      this.rating = this.proveedorEditando.rating || 5;
      this.precioPromedio = this.proveedorEditando.precioPromedio || '$$$';
      this.activo = this.proveedorEditando.activo ?? true;
    }
  }

  guardar(): void {
    if (!this.nombre.trim() || !this.categoriaId || this.guardando) {
      this.toastService.showError('Por favor ingresa al menos el nombre y la categoría.');
      return;
    }

    this.guardando = true;

    if (this.proveedorEditando) {
      const payload: ActualizarProveedorRequest = {
        categoriaId: Number(this.categoriaId),
        nombre: this.nombre.trim(),
        contacto: this.contacto.trim() || undefined,
        email: this.email.trim() || undefined,
        telefono: this.telefono.trim() || undefined,
        direccion: this.direccion.trim() || undefined,
        sitioWeb: this.sitioWeb.trim() || undefined,
        rfc: this.rfc.trim() || undefined,
        rating: Number(this.rating),
        precioPromedio: this.precioPromedio,
        activo: this.activo
      };

      this.proveedorService.actualizarProveedor(this.proveedorEditando.id, payload).subscribe({
        next: () => {
          this.guardando = false;
          this.toastService.showSuccess('Proveedor actualizado correctamente');
          this.guardado.emit();
        },
        error: (err) => {
          this.guardando = false;
          console.error('Error al actualizar proveedor:', err);
          this.toastService.showError('Error al actualizar proveedor.');
        }
      });
    } else {
      const payload: CrearProveedorRequest = {
        categoriaId: Number(this.categoriaId),
        nombre: this.nombre.trim(),
        contacto: this.contacto.trim() || undefined,
        email: this.email.trim() || undefined,
        telefono: this.telefono.trim() || undefined,
        direccion: this.direccion.trim() || undefined,
        sitioWeb: this.sitioWeb.trim() || undefined,
        rfc: this.rfc.trim() || undefined,
        rating: Number(this.rating),
        precioPromedio: this.precioPromedio,
        activo: true
      };

      this.proveedorService.crearProveedor(payload).subscribe({
        next: () => {
          this.guardando = false;
          this.toastService.showSuccess('Proveedor registrado exitosamente');
          this.guardado.emit();
        },
        error: (err) => {
          this.guardando = false;
          console.error('Error al crear proveedor:', err);
          this.toastService.showError('Error al registrar proveedor.');
        }
      });
    }
  }

  onCerrar(): void {
    this.cerrar.emit();
  }
}
