import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Cotizacion } from '@core/models/cotizacion.models';
import { CotizacionesService } from '@features/cotizaciones/services/cotizaciones.service';

@Component({
  selector: 'app-cotizacion-desglose-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cotizacion-desglose-modal.component.html'
})
export class CotizacionDesgloseModalComponent implements OnChanges {
  @Input() show: boolean = false;
  @Input() cotizacion: Cotizacion | null = null;
  @Input() folio: string | null = null;

  @Output() onClose = new EventEmitter<void>();

  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly cdr = inject(ChangeDetectorRef);

  activeCotizacion: Cotizacion | null = null;
  loading: boolean = false;
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['cotizacion'] || changes['folio'] || changes['show']) {
      if (this.show) {
        if (this.cotizacion) {
          this.activeCotizacion = this.cotizacion;
          this.errorMessage = null;
          this.loading = false;
        } else if (this.folio) {
          this.loadCotizacionByFolio();
        }
      } else {
        this.activeCotizacion = null;
        this.errorMessage = null;
        this.loading = false;
      }
    }
    this.cdr.detectChanges();
  }

  loadCotizacionByFolio(): void {
    if (!this.folio) return;
    this.loading = true;
    this.errorMessage = null;
    this.cdr.detectChanges();

    this.cotizacionesService.getCotizaciones().subscribe({
      next: (list) => {
        const found = list.find(c => c.folio.toLowerCase() === this.folio?.toLowerCase());
        if (found) {
          this.activeCotizacion = found;
          this.loading = false;
        } else {
          this.errorMessage = 'No se encontró la cotización con el folio "' + this.folio + '".';
          this.loading = false;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al obtener cotización por folio:', err);
        this.errorMessage = 'No se pudo obtener el desglose de la cotización.';
        this.loading = false;
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
      case 'aceptada':
      case 'confirmado':
      case 'completado':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'enviada':
      case 'en preparación':
      case 'proceso':
        return 'bg-blue-50 text-blue-700 border-blue-200';
      case 'borrador':
      case 'pendiente':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'rechazada':
      case 'cancelado':
        return 'bg-red-50 text-red-700 border-red-200';
      default:
        return 'bg-gray-50 text-gray-700 border-gray-200';
    }
  }

  get serviciosExtra(): import('@core/models/cotizacion.models').CotizacionItem[] {
    return (this.activeCotizacion?.items || []).filter(i => i.esServicioExtra);
  }

  get totalServiciosExtra(): number {
    return this.serviciosExtra.reduce((acc, i) => acc + (i.monto || 0), 0);
  }
}
