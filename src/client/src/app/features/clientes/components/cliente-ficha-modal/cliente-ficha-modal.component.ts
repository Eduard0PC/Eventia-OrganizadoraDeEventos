import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClienteSummary, ClienteDetalle } from '../../models/cliente.models';
import { ClientesService } from '../../services/clientes.service';
import { FolioDesgloseButtonComponent } from '@shared/components/folio-desglose-button/folio-desglose-button.component';

export type ClienteFichaTab = 'pagos' | 'servicios' | 'eventos';

@Component({
  selector: 'app-cliente-ficha-modal',
  standalone: true,
  imports: [CommonModule, FolioDesgloseButtonComponent],
  templateUrl: './cliente-ficha-modal.component.html'
})
export class ClienteFichaModalComponent implements OnChanges {
  @Input() show: boolean = false;
  @Input() clienteId: number | null = null;
  @Input() clienteSummary: ClienteSummary | null = null;

  @Output() onClose = new EventEmitter<void>();
  @Output() onVerDesgloseFolio = new EventEmitter<string>();

  private readonly clientesService = inject(ClientesService);
  private readonly cdr = inject(ChangeDetectorRef);

  clienteFicha: ClienteDetalle | null = null;
  loadingFicha: boolean = false;
  errorMessage: string | null = null;
  fichaActiveTab: ClienteFichaTab = 'pagos';

  get displayName(): string {
    if (this.clienteFicha?.nombreCompleto) return this.clienteFicha.nombreCompleto;
    if (this.clienteSummary?.nombreCompleto) return this.clienteSummary.nombreCompleto;
    if (this.clienteSummary?.nombre) return `${this.clienteSummary.nombre} ${this.clienteSummary.apellido || ''}`;
    return 'Cargando cliente...';
  }

  get displayInitial(): string {
    const name = this.displayName;
    if (!name || name === 'Cargando cliente...') return 'C';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) {
      return `${parts[0].charAt(0)}${parts[1].charAt(0)}`.toUpperCase();
    }
    return name.charAt(0).toUpperCase();
  }

  get displayStatus(): string {
    return this.clienteFicha?.estatusLabel || this.clienteSummary?.estatusLabel || '';
  }

  get displayEmail(): string {
    return this.clienteFicha?.email || this.clienteSummary?.email || '-';
  }

  get displayPhone(): string {
    return this.clienteFicha?.telefono || this.clienteSummary?.telefono || 'Sin teléfono';
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.show) {
      if (this.clienteId !== null && this.clienteId !== undefined) {
        this.fichaActiveTab = 'pagos';
        this.loadFicha();
      }
    } else {
      this.clienteFicha = null;
      this.loadingFicha = false;
      this.errorMessage = null;
    }
    this.cdr.detectChanges();
  }

  loadFicha(): void {
    const id = Number(this.clienteId);
    if (!id || isNaN(id)) {
      this.loadingFicha = false;
      this.errorMessage = 'ID de cliente no válido.';
      this.cdr.detectChanges();
      return;
    }

    this.loadingFicha = true;
    this.errorMessage = null;
    this.clienteFicha = null;
    this.cdr.detectChanges();

    this.clientesService.getFichaCliente(id).subscribe({
      next: (ficha) => {
        this.loadingFicha = false;
        if (ficha) {
          this.clienteFicha = ficha;
        } else {
          this.errorMessage = 'No se encontraron datos para este cliente.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar la ficha del cliente:', err);
        this.loadingFicha = false;
        this.errorMessage = 'No se pudo obtener la información detallada del cliente.';
        this.cdr.detectChanges();
      }
    });
  }

  closeModal(): void {
    this.onClose.emit();
  }

  getStatusBadgeClass(estatus: string): string {
    const st = (estatus || '').toLowerCase();
    switch (st) {
      case 'activo':
      case 'completado':
      case 'confirmado':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'prospecto':
      case 'en preparación':
      case 'pendiente':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'inactivo':
      case 'rechazada':
      case 'cancelado':
        return 'bg-red-50 text-red-700 border-red-200';
      default:
        return 'bg-gray-50 text-gray-700 border-gray-200';
    }
  }
}
