import { Component, Input, Output, EventEmitter, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  PlanificacionEvento,
  RequerimientoProveedor,
  EventoTarea,
} from '../../models/planificador.models';
import { PlanificadorService } from '../../services/planificador.service';
import { ToastService } from '@shared/components/toast/toast.service';
import { AsignarProveedorModalComponent } from '../asignar-proveedor-modal/asignar-proveedor-modal.component';

@Component({
  selector: 'app-planificador-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, AsignarProveedorModalComponent],
  templateUrl: './planificador-panel.component.html',
})
export class PlanificadorPanelComponent implements OnInit {
  private readonly planificadorService = inject(PlanificadorService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() searchQuery = '';
  @Output() onPlanificacionesChange = new EventEmitter<PlanificacionEvento[]>();

  planificaciones: PlanificacionEvento[] = [];
  cargando = false;
  filterStatus = 'Todos';

  // Modal de detalle operativo del evento seleccionado
  selectedEventoDetalle: PlanificacionEvento | null = null;
  detalleActiveTab: 'proveedores' | 'checklist' = 'proveedores';

  // Estado por tarjeta para el input de nueva tarea del checklist libre
  nuevaTareaPorEvento: Record<number, string> = {};
  guardandoTareaPorEvento: Record<number, boolean> = {};

  // Modal de asignación de proveedor / locación
  showAsignarModal = false;
  selectedEventoModal: PlanificacionEvento | null = null;
  selectedRequerimientoModal: RequerimientoProveedor | null = null;

  ngOnInit(): void {
    this.cargarPlanificaciones();
  }

  cargarPlanificaciones(): void {
    this.cargando = true;
    this.planificadorService.getPlanificaciones().subscribe({
      next: (data) => {
        this.planificaciones = (data || []).map((ev) => {
          this.recalcularProgresoLocal(ev);
          return ev;
        });
        if (this.selectedEventoDetalle) {
          const updated = this.planificaciones.find(
            (p) => p.eventoContratadoId === this.selectedEventoDetalle?.eventoContratadoId
          );
          this.selectedEventoDetalle = updated || null;
        }
        this.cargando = false;
        this.onPlanificacionesChange.emit(this.planificaciones);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar planificaciones:', err);
        this.cargando = false;
        this.cdr.detectChanges();
      },
    });
  }

  get filteredPlanificaciones(): PlanificacionEvento[] {
    const q = (this.searchQuery || '').trim().toLowerCase();
    return this.planificaciones.filter((p) => {
      const matchesSearch =
        !q ||
        p.nombreEvento.toLowerCase().includes(q) ||
        p.clienteNombre.toLowerCase().includes(q) ||
        p.folioContrato.toLowerCase().includes(q) ||
        p.lugar.toLowerCase().includes(q);

      const etiqueta = this.obtenerEtiquetaEstatus(p);
      const matchesStatus = this.filterStatus === 'Todos' || etiqueta === this.filterStatus;
      return matchesSearch && matchesStatus;
    });
  }

  // Métricas financieras y operativas globales
  get totalIngresosContratos(): number {
    return this.planificaciones.reduce((acc, p) => acc + (p.totalContrato || 0), 0);
  }

  get totalCostosProveedores(): number {
    return this.planificaciones.reduce((acc, p) => acc + (p.costoTotalProveedores || 0), 0);
  }

  get totalMargenGanancia(): number {
    return this.totalIngresosContratos - this.totalCostosProveedores;
  }

  get porcentajeMargenGlobal(): number {
    if (this.totalIngresosContratos <= 0) return 0;
    return Math.round((this.totalMargenGanancia / this.totalIngresosContratos) * 1000) / 10;
  }

  get totalRequerimientosConfirmados(): number {
    return this.planificaciones.reduce((acc, p) => acc + (p.requerimientosCubiertos || 0), 0);
  }

  get totalRequerimientosGlobales(): number {
    return this.planificaciones.reduce((acc, p) => acc + (p.totalRequerimientos || 0), 0);
  }

  obtenerEtiquetaEstatus(plan: PlanificacionEvento): 'En preparación' | 'Confirmado' | 'En ejecución' | 'Finalizado' {
    const totalReq = plan.totalRequerimientos || 0;
    const reqCubiertos = plan.requerimientosCubiertos || 0;
    const totalTareas = (plan.checklist || []).length;
    const tareasCompletadas = (plan.checklist || []).filter((t) => t.completada).length;

    const todosReqCubiertos = totalReq > 0 && reqCubiertos >= totalReq;
    const todasTareasCompletadas = totalTareas > 0 && tareasCompletadas >= totalTareas;
    const hayTareasCompletadas = tareasCompletadas > 0;

    const hoyStr = new Date().toISOString().slice(0, 10);
    const fechaEvStr = (plan.fechaEvento || '').slice(0, 10);

    if (todosReqCubiertos && (todasTareasCompletadas || (fechaEvStr && fechaEvStr < hoyStr))) {
      return 'Finalizado';
    }
    if (
      (fechaEvStr && fechaEvStr === hoyStr) ||
      (todosReqCubiertos && hayTareasCompletadas) ||
      (reqCubiertos > 0 && hayTareasCompletadas)
    ) {
      return 'En ejecución';
    }
    if (todosReqCubiertos) {
      return 'Confirmado';
    }
    return 'En preparación';
  }

  getStatusBadgeClass(plan: PlanificacionEvento): string {
    const label = this.obtenerEtiquetaEstatus(plan);
    switch (label) {
      case 'Finalizado':
        return 'bg-emerald-100 text-emerald-700 border-emerald-200';
      case 'Confirmado':
        return 'bg-blue-100 text-blue-700 border-blue-200';
      case 'En ejecución':
        return 'bg-purple-100 text-purple-700 border-purple-200';
      default:
        return 'bg-amber-100 text-amber-700 border-amber-200';
    }
  }

  getMargenBadgeClass(porcentaje: number): string {
    if (porcentaje >= 30) return 'bg-emerald-100 text-emerald-800 border-emerald-200';
    if (porcentaje >= 15) return 'bg-amber-100 text-amber-800 border-amber-200';
    return 'bg-red-100 text-red-800 border-amber-200';
  }

  abrirDetalleEvento(evento: PlanificacionEvento): void {
    this.selectedEventoDetalle = evento;
    this.detalleActiveTab = 'proveedores';
    this.cdr.detectChanges();
  }

  cerrarDetalleEvento(): void {
    this.selectedEventoDetalle = null;
    this.cdr.detectChanges();
  }

  abrirModalAsignar(evento: PlanificacionEvento, req: RequerimientoProveedor): void {
    this.selectedEventoModal = evento;
    this.selectedRequerimientoModal = req;
    this.showAsignarModal = true;
  }

  cerrarModalAsignar(): void {
    this.showAsignarModal = false;
    this.selectedEventoModal = null;
    this.selectedRequerimientoModal = null;
  }

  onProveedorAsignado(planActualizado: PlanificacionEvento): void {
    this.recalcularProgresoLocal(planActualizado);
    this.reemplazarPlanificacionEnLista(planActualizado);
    this.cerrarModalAsignar();
  }

  removerProveedor(evento: PlanificacionEvento, req: RequerimientoProveedor): void {
    if (!req.asignacion) return;
    const provNombre = req.asignacion.proveedorNombre;
    if (!confirm(`¿Deseas liberar al proveedor "${provNombre}" de este evento? Su fecha (${evento.fechaEvento}) volverá a quedar disponible.`)) {
      return;
    }

    this.planificadorService
      .removerAsignacion(evento.eventoContratadoId, req.asignacion.id)
      .subscribe({
        next: (planActualizado) => {
          this.toastService.showSuccess(`Proveedor "${provNombre}" liberado correctamente.`);
          this.recalcularProgresoLocal(planActualizado);
          this.reemplazarPlanificacionEnLista(planActualizado);
        },
        error: (err) => {
          const msg = err?.error?.error || 'Error al liberar el proveedor.';
          this.toastService.showError(msg);
        },
      });
  }

  agregarTareaChecklist(evento: PlanificacionEvento): void {
    const id = evento.eventoContratadoId;
    const titulo = (this.nuevaTareaPorEvento[id] || '').trim();
    if (!titulo || this.guardandoTareaPorEvento[id]) return;

    this.guardandoTareaPorEvento[id] = true;
    this.planificadorService.crearTarea(id, { titulo }).subscribe({
      next: (nuevaTarea) => {
        evento.checklist.push(nuevaTarea);
        this.nuevaTareaPorEvento[id] = '';
        this.guardandoTareaPorEvento[id] = false;
        this.recalcularProgresoLocal(evento);
        this.onPlanificacionesChange.emit(this.planificaciones);
        this.cdr.detectChanges();
      },
      error: () => {
        this.guardandoTareaPorEvento[id] = false;
        this.toastService.showError('No se pudo agregar la tarea al checklist.');
      },
    });
  }

  toggleTareaChecklist(evento: PlanificacionEvento, tarea: EventoTarea): void {
    this.planificadorService.toggleTarea(evento.eventoContratadoId, tarea.id).subscribe({
      next: (actualizada) => {
        tarea.completada = actualizada.completada;
        this.recalcularProgresoLocal(evento);
        this.onPlanificacionesChange.emit(this.planificaciones);
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.showError('Error al actualizar el estado de la tarea.');
      },
    });
  }

  eliminarTareaChecklist(evento: PlanificacionEvento, tarea: EventoTarea): void {
    this.planificadorService.eliminarTarea(evento.eventoContratadoId, tarea.id).subscribe({
      next: () => {
        evento.checklist = evento.checklist.filter((t) => t.id !== tarea.id);
        this.recalcularProgresoLocal(evento);
        this.onPlanificacionesChange.emit(this.planificaciones);
        this.cdr.detectChanges();
      },
      error: () => {
        this.toastService.showError('No se pudo eliminar la tarea.');
      },
    });
  }

  private reemplazarPlanificacionEnLista(actualizado: PlanificacionEvento): void {
    const idx = this.planificaciones.findIndex(
      (p) => p.eventoContratadoId === actualizado.eventoContratadoId
    );
    if (idx !== -1) {
      this.planificaciones[idx] = actualizado;
    }
    if (
      this.selectedEventoDetalle &&
      this.selectedEventoDetalle.eventoContratadoId === actualizado.eventoContratadoId
    ) {
      this.selectedEventoDetalle = actualizado;
    }
    this.onPlanificacionesChange.emit(this.planificaciones);
    this.cdr.detectChanges();
  }

  private recalcularProgresoLocal(evento: PlanificacionEvento): void {
    const total = (evento.totalRequerimientos || 0) + (evento.checklist?.length || 0);
    const hechos =
      (evento.requerimientosCubiertos || 0) +
      (evento.checklist || []).filter((t) => t.completada).length;

    const etiqueta = this.obtenerEtiquetaEstatus(evento);
    if (etiqueta === 'Finalizado') {
      evento.estatusEvento = 'completado';
      evento.progreso = 100;
    } else if (etiqueta === 'En ejecución') {
      evento.estatusEvento = 'en_curso';
      evento.progreso = total > 0 ? Math.round((hechos / total) * 100) : 0;
    } else {
      evento.estatusEvento = 'programado';
      evento.progreso = total > 0 ? Math.round((hechos / total) * 100) : 0;
    }
  }
}

