import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  PlanificacionEvento,
  RequerimientoProveedor,
  ProveedorDisponible,
  AsignarProveedorEventoRequest,
} from '../../models/planificador.models';
import { PlanificadorService } from '../../services/planificador.service';
import { ToastService } from '@shared/components/toast/toast.service';
import { ProveedorServicio } from '@core/models/proveedor.model';

@Component({
  selector: 'app-asignar-proveedor-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './asignar-proveedor-modal.component.html',
})
export class AsignarProveedorModalComponent implements OnChanges {
  private readonly planificadorService = inject(PlanificadorService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() show = false;
  @Input() evento: PlanificacionEvento | null = null;
  @Input() requerimiento: RequerimientoProveedor | null = null;

  @Output() onClose = new EventEmitter<void>();
  @Output() onAssigned = new EventEmitter<PlanificacionEvento>();

  proveedoresDisponibles: ProveedorDisponible[] = [];
  cargando = false;
  guardando = false;

  selectedProveedor: ProveedorDisponible | null = null;
  selectedServicioId: number | null = null;
  costoAcordado = 0;
  notas = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (this.show && this.evento && this.requerimiento && (changes['show'] || changes['requerimiento'])) {
      this.cargarDisponibles();
    }
  }

  cargarDisponibles(): void {
    if (!this.evento || !this.requerimiento) return;

    this.cargando = true;
    this.proveedoresDisponibles = [];
    this.selectedProveedor = null;
    this.selectedServicioId = null;
    this.costoAcordado = this.requerimiento.asignacion?.costoAcordado ?? 0;
    this.notas = this.requerimiento.asignacion?.notas ?? '';

    this.planificadorService
      .getProveedoresDisponibles(
        this.evento.eventoContratadoId,
        this.requerimiento.categoriaProveedor,
        this.requerimiento.esLocacion
      )
      .subscribe({
        next: (list) => {
          this.proveedoresDisponibles = list || [];
          if (this.proveedoresDisponibles.length > 0) {
            this.seleccionarProveedor(this.proveedoresDisponibles[0]);
          }
          this.cargando = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error al cargar proveedores disponibles:', err);
          this.toastService.showError('No se pudieron consultar los proveedores disponibles.');
          this.cargando = false;
          this.cdr.detectChanges();
        },
      });
  }

  seleccionarProveedor(prov: ProveedorDisponible): void {
    this.selectedProveedor = prov;
    if (prov.servicios && prov.servicios.length > 0) {
      this.seleccionarServicioProveedor(prov.servicios[0]);
    } else {
      this.selectedServicioId = null;
      this.costoAcordado = prov.costoSugerido || 0;
    }
  }

  seleccionarServicioProveedor(srv: ProveedorServicio): void {
    this.selectedServicioId = srv.id;
    this.costoAcordado = srv.costo;
  }

  confirmarAsignacion(): void {
    if (!this.evento || !this.requerimiento || !this.selectedProveedor || this.guardando) {
      return;
    }

    if (this.costoAcordado < 0) {
      this.toastService.showError('El costo acordado no puede ser negativo.');
      return;
    }

    this.guardando = true;

    const payload: AsignarProveedorEventoRequest = {
      claveRequerimiento: this.requerimiento.claveRequerimiento,
      nombreRequerimiento: this.requerimiento.nombreRequerimiento,
      categoriaProveedor: this.requerimiento.categoriaProveedor,
      proveedorId: this.selectedProveedor.id,
      costoAcordado: Number(this.costoAcordado) || 0,
      catalogoServicioId: this.requerimiento.catalogoServicioId ?? null,
      esLocacion: this.requerimiento.esLocacion,
      notas: this.notas.trim() || null,
    };

    this.planificadorService.asignarProveedor(this.evento.eventoContratadoId, payload).subscribe({
      next: (planActualizado) => {
        this.guardando = false;
        this.toastService.showSuccess(
          `Proveedor "${this.selectedProveedor?.nombre}" confirmado y bloqueado para el ${this.evento?.fechaEvento}.`
        );
        this.onAssigned.emit(planActualizado);
      },
      error: (err) => {
        this.guardando = false;
        const msg = err?.error?.error || 'No se pudo asignar el proveedor en esta fecha.';
        this.toastService.showError(msg);
        this.cdr.detectChanges();
      },
    });
  }

  cerrar(): void {
    if (this.guardando) return;
    this.onClose.emit();
  }
}

