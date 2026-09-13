import { Component, Input, Output, EventEmitter, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogoEventoItem, CrearCatalogoEventoRequest, ActualizarCatalogoEventoRequest } from '../../models/catalogo.models';
import { CatalogoService } from '../../services/catalogo.service';
import { ToastService } from '@shared/components/toast/toast.service';

export interface SaveCatalogEventPayload {
  mode: 'create' | 'edit';
  id?: number;
  data: CrearCatalogoEventoRequest | ActualizarCatalogoEventoRequest;
  itemLocal: CatalogoEventoItem;
}

@Component({
  selector: 'app-catalogo-form-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="show" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fade-in">
      <div class="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl space-y-5 border border-gray-100 max-h-[90vh] overflow-y-auto">
        <div class="flex justify-between items-center border-b border-gray-100 pb-4">
          <div>
            <h3 class="text-xl font-bold text-gray-900">
              {{ mode === 'create' ? 'Crear Nuevo Evento / Paquete' : 'Editar Evento / Paquete' }}
            </h3>
            <p class="text-xs text-gray-500">
              {{ mode === 'create' ? 'Se guardará directamente en la base de datos Supabase' : 'ID: ' + itemToEdit?.id }}
            </p>
          </div>
          <button
            type="button"
            (click)="closeModal()"
            [disabled]="guardando"
            class="text-gray-400 hover:text-gray-600 font-bold text-xl cursor-pointer"
          >
            ✕
          </button>
        </div>

        <!-- Alerta Banner de Éxito pre-cierre -->
        <div
          *ngIf="successMessage"
          class="p-4 rounded-2xl bg-emerald-50 border border-emerald-200 text-emerald-800 flex items-center gap-3 animate-fade-in"
        >
          <div>
            <p class="text-sm font-bold">{{ successMessage }}</p>
            <p class="text-xs text-emerald-600">Cerrando ventana de diálogo...</p>
          </div>
        </div>

        <div *ngIf="!successMessage" class="space-y-4">
          <div>
            <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Nombre del Evento *</label>
            <input
              type="text"
              [(ngModel)]="formData.nombre"
              placeholder="ej. Boda Elegante Premium"
              class="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500/50"
            />
          </div>

          <div>
            <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Descripción</label>
            <textarea
              [(ngModel)]="formData.descripcion"
              rows="3"
              placeholder="Descripción detallada de servicios incluidos..."
              class="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500/50"
            ></textarea>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Precio Base (MXN) *</label>
              <input
                type="number"
                [(ngModel)]="formData.precioBase"
                min="0"
                class="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500/50"
              />
            </div>
            <div>
              <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Duración (Horas) *</label>
              <input
                type="number"
                [(ngModel)]="formData.duracionHoras"
                min="1"
                class="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500/50"
              />
            </div>
          </div>

          <div>
            <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-1">Imagen del Evento</label>
            <input
              type="file"
              accept="image/*"
              (change)="onFileSelected($event)"
              class="w-full text-xs text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0 file:text-xs file:font-semibold file:bg-red-50 file:text-red-700 hover:file:bg-red-100 cursor-pointer"
            />
            <div *ngIf="imagePreviewUrl" class="mt-3 relative h-36 rounded-2xl overflow-hidden border border-gray-200">
              <img [src]="imagePreviewUrl" class="w-full h-full object-cover" />
            </div>
          </div>

          <div *ngIf="mode === 'edit'" class="flex items-center gap-2">
            <input type="checkbox" id="modalActivo" [(ngModel)]="formData.activo" class="rounded text-red-600 focus:ring-red-500" />
            <label for="modalActivo" class="text-sm font-semibold text-gray-700">Evento Activo en Catálogo</label>
          </div>
        </div>

        <div *ngIf="!successMessage" class="pt-4 border-t border-gray-100 flex justify-end gap-3">
          <button
            type="button"
            (click)="closeModal()"
            [disabled]="guardando"
            class="px-5 py-2.5 rounded-xl border border-gray-200 text-gray-600 font-bold text-xs hover:bg-gray-50 transition cursor-pointer"
          >
            Cancelar
          </button>
          <button
            type="button"
            (click)="save()"
            [disabled]="guardando"
            class="px-5 py-2.5 rounded-xl bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white font-bold text-xs shadow-md transition flex items-center gap-2 cursor-pointer"
          >
            <span *ngIf="guardando" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ guardando ? 'Guardando...' : (mode === 'create' ? 'Guardar Evento' : 'Actualizar Evento') }}</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [],
})
export class CatalogoFormModalComponent implements OnInit {
  @Input() show = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() itemToEdit: CatalogoEventoItem | null = null;

  @Output() onClose = new EventEmitter<void>();
  @Output() onSavedSuccess = new EventEmitter<void>();

  private readonly catalogoService = inject(CatalogoService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  formData = {
    nombre: '',
    descripcion: '',
    precioBase: 0,
    duracionHoras: 4,
    activo: true,
  };

  selectedImageFile: File | null = null;
  imagePreviewUrl: string | null = null;
  guardando = false;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.resetForm();
  }

  ngOnChanges(): void {
    this.resetForm();
  }

  private resetForm(): void {
    this.guardando = false;
    this.successMessage = null;
    this.selectedImageFile = null;

    if (this.mode === 'edit' && this.itemToEdit) {
      this.formData = {
        nombre: this.itemToEdit.nombre || '',
        descripcion: this.itemToEdit.descripcion || '',
        precioBase: this.itemToEdit.precioBase || 0,
        duracionHoras: this.itemToEdit.duracionHoras || 4,
        activo: this.itemToEdit.activo ?? true,
      };
      this.imagePreviewUrl = this.itemToEdit.imagen || this.itemToEdit.imagenUrl || null;
    } else {
      this.formData = {
        nombre: '',
        descripcion: '',
        precioBase: 0,
        duracionHoras: 4,
        activo: true,
      };
      this.imagePreviewUrl = null;
    }
  }

  onFileSelected(evt: any): void {
    const file = evt.target.files?.[0];
    if (file) {
      this.selectedImageFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreviewUrl = reader.result as string;
        this.cdr.detectChanges();
      };
      reader.readAsDataURL(file);
    }
  }

  closeModal(): void {
    if (this.guardando) return;
    this.show = false;
    this.successMessage = null;
    this.onClose.emit();
  }

  save(): void {
    if (!this.formData.nombre || this.formData.precioBase < 0) {
      alert('Por favor especifica un nombre válido y precio mayor o igual a 0.');
      return;
    }

    this.guardando = true;
    this.cdr.detectChanges();

    if (this.selectedImageFile) {
      this.catalogoService.subirImagenEvento(this.selectedImageFile).subscribe({
        next: (uploadRes) => {
          this.executeSave(uploadRes.imageUrl);
        },
        error: (err) => {
          console.error('Error al subir imagen:', err);
          this.executeSave(this.mode === 'edit' ? this.itemToEdit?.imagenUrl || null : null);
        },
      });
    } else {
      this.executeSave(this.mode === 'edit' ? this.itemToEdit?.imagenUrl || null : null);
    }
  }

  private executeSave(imagenUrl: string | null): void {
    const payload = {
      nombre: this.formData.nombre,
      descripcion: this.formData.descripcion || null,
      precioBase: Number(this.formData.precioBase) || 0,
      duracionHoras: Math.max(1, Number(this.formData.duracionHoras) || 1),
      activo: this.formData.activo,
      imagenUrl: imagenUrl,
    };

    if (this.mode === 'create') {
      this.catalogoService.crearCatalogoEvento(payload).subscribe({
        next: () => {
          this.handleSuccess('¡Evento creado con éxito!');
        },
        error: (err) => {
          console.error('Error al crear evento:', err);
          this.guardando = false;
          this.toastService.showError('Error al crear el evento en el servidor.');
          this.cdr.detectChanges();
        },
      });
    } else if (this.mode === 'edit' && this.itemToEdit) {
      this.catalogoService.actualizarCatalogoEvento(this.itemToEdit.id, payload).subscribe({
        next: () => {
          this.handleSuccess('¡Evento actualizado con éxito!');
        },
        error: (err) => {
          console.error('Error al actualizar evento:', err);
          this.guardando = false;
          this.toastService.showError('Error al actualizar el evento en el servidor.');
          this.cdr.detectChanges();
        },
      });
    }
  }

  private handleSuccess(mensaje: string): void {
    this.guardando = false;
    this.successMessage = mensaje;
    this.toastService.showSuccess(mensaje);
    this.cdr.detectChanges();

    // Pequeño retardo de 900ms para mostrar la alerta visual antes de cerrar el modal
    setTimeout(() => {
      this.show = false;
      this.successMessage = null;
      this.onSavedSuccess.emit();
      this.cdr.detectChanges();
    }, 900);
  }
}
