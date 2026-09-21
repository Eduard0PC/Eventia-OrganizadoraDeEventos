import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Proveedor } from '@core/models/proveedor.model';
import { IconEditComponent, IconTrashComponent, IconEyeComponent } from '@shared/components/icons';

@Component({
  selector: 'app-proveedor-card',
  standalone: true,
  imports: [CommonModule, IconEditComponent, IconTrashComponent, IconEyeComponent],
  templateUrl: './proveedor-card.html'
})
export class ProveedorCardComponent {
  @Input({ required: true }) proveedor!: Proveedor;
  @Input() esEmpleado = false;

  @Output() verServicios = new EventEmitter<Proveedor>();
  @Output() editarProveedor = new EventEmitter<Proveedor>();
  @Output() eliminarProveedor = new EventEmitter<Proveedor>();

  get stars(): number[] {
    return Array(Math.min(5, Math.max(1, this.proveedor.rating || 5))).fill(0);
  }

  get emptyStars(): number[] {
    const starCount = Math.min(5, Math.max(1, this.proveedor.rating || 5));
    return Array(5 - starCount).fill(0);
  }

  onVerServicios(): void {
    this.verServicios.emit(this.proveedor);
  }

  onEditar(): void {
    this.editarProveedor.emit(this.proveedor);
  }

  onEliminar(): void {
    this.eliminarProveedor.emit(this.proveedor);
  }

  formatearSitioWeb(url?: string): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    return `https://${url}`;
  }
}
