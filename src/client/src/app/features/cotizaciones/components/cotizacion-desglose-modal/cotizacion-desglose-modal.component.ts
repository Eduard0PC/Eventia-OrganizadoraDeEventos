import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Cotizacion } from '@core/models/cotizacion.models';
import { CotizacionesService } from '@features/cotizaciones/services/cotizaciones.service';
import { ProveedorService } from '@features/proveedores/services/proveedor.service';
import { Proveedor } from '@core/models/proveedor.model';

export interface ViabilidadResultado {
  disponible: boolean;
  cantidad: number;
  nombres: string;
}

@Component({
  selector: 'app-cotizacion-desglose-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cotizacion-desglose-modal.component.html'
})
export class CotizacionDesgloseModalComponent implements OnChanges, OnInit {
  @Input() show: boolean = false;
  @Input() cotizacion: Cotizacion | null = null;
  @Input() folio: string | null = null;
  @Input() canManage: boolean = false;

  @Output() onClose = new EventEmitter<void>();
  @Output() onAprobar = new EventEmitter<Cotizacion>();
  @Output() onRechazar = new EventEmitter<{ cotizacion: Cotizacion; motivo: string }>();
  @Output() onAjustar = new EventEmitter<Cotizacion>();

  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly proveedorService = inject(ProveedorService);
  private readonly cdr = inject(ChangeDetectorRef);

  activeCotizacion: Cotizacion | null = null;
  loading: boolean = false;
  errorMessage: string | null = null;

  proveedores: Proveedor[] = [];
  cargandoProveedores: boolean = false;

  mostrandoFormularioRechazo: boolean = false;
  motivoRechazo: string = '';
  procesandoAccion: boolean = false;

  ngOnInit(): void {
    this.cargarProveedores();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['cotizacion'] || changes['folio'] || changes['show']) {
      if (this.show) {
        this.mostrandoFormularioRechazo = false;
        this.motivoRechazo = '';
        this.procesandoAccion = false;

        if (this.proveedores.length === 0) {
          this.cargarProveedores();
        }

        if (this.cotizacion) {
          this.activeCotizacion = this.cotizacion;
          this.errorMessage = null;
          this.loading = false;
          this.checkAndLoadServiciosDefecto();
        } else if (this.folio) {
          this.loadCotizacionByFolio();
        }
      } else {
        this.activeCotizacion = null;
        this.errorMessage = null;
        this.loading = false;
        this.mostrandoFormularioRechazo = false;
      }
    }
    this.cdr.detectChanges();
  }

  cargarProveedores(): void {
    this.cargandoProveedores = true;
    this.proveedorService.getProveedores(undefined, false).subscribe({
      next: (lista) => {
        this.proveedores = lista || [];
        this.cargandoProveedores = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar proveedores para viabilidad:', err);
        this.proveedores = [];
        this.cargandoProveedores = false;
        this.cdr.detectChanges();
      }
    });
  }

  getViabilidad(categoriaProveedor?: string | null): ViabilidadResultado {
    if (!categoriaProveedor || !categoriaProveedor.trim()) {
      return { disponible: true, cantidad: 0, nombres: '' };
    }
    const normalizar = (texto: string) =>
      texto.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim();

    const catBuscada = normalizar(categoriaProveedor);
    const encontrados = this.proveedores.filter(p =>
      p.activo !== false && normalizar(p.categoriaNombre || '') === catBuscada
    );

    return {
      disponible: encontrados.length > 0,
      cantidad: encontrados.length,
      nombres: encontrados.map(p => p.nombre).slice(0, 2).join(', ')
    };
  }

  get totalServiciosSinProveedor(): number {
    let sinProveedor = 0;
    for (const def of this.serviciosDefecto) {
      if (def.categoriaProveedor && !this.getViabilidad(def.categoriaProveedor).disponible) {
        sinProveedor++;
      }
    }
    for (const extra of this.serviciosExtra) {
      if (extra.categoriaProveedor && !this.getViabilidad(extra.categoriaProveedor).disponible) {
        sinProveedor++;
      }
    }
    return sinProveedor;
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
          this.checkAndLoadServiciosDefecto();
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

  solicitarAprobacion(): void {
    if (!this.activeCotizacion || this.procesandoAccion) return;
    this.onAprobar.emit(this.activeCotizacion);
  }

  iniciarRechazo(): void {
    this.mostrandoFormularioRechazo = true;
    this.motivoRechazo = '';
  }

  cancelarRechazo(): void {
    this.mostrandoFormularioRechazo = false;
    this.motivoRechazo = '';
  }

  confirmarRechazo(): void {
    if (!this.activeCotizacion || this.procesandoAccion) return;
    this.onRechazar.emit({
      cotizacion: this.activeCotizacion,
      motivo: this.motivoRechazo.trim()
    });
  }

  solicitarAjuste(): void {
    if (!this.activeCotizacion || this.procesandoAccion) return;
    this.onAjustar.emit(this.activeCotizacion);
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
      case 'modificada':
        return 'bg-purple-50 text-purple-700 border-purple-200';
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

  get serviciosDefecto(): import('@core/models/cotizacion.models').ServicioDefectoItem[] {
    return this.activeCotizacion?.serviciosPorDefecto || [];
  }

  private checkAndLoadServiciosDefecto(): void {
    if (this.activeCotizacion && (!this.activeCotizacion.serviciosPorDefecto || this.activeCotizacion.serviciosPorDefecto.length === 0) && this.activeCotizacion.categoriaEvento) {
      this.cotizacionesService.getServiciosPorDefecto(this.activeCotizacion.categoriaEvento).subscribe({
        next: (defs) => {
          if (defs && defs.length > 0 && this.activeCotizacion) {
            this.activeCotizacion.serviciosPorDefecto = defs;
            this.cdr.detectChanges();
          }
        }
      });
    }
  }
}
