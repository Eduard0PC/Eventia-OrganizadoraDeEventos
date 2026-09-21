import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CatalogoEventoItem } from '../../models/catalogo.models';
import { IconEditComponent, IconTrashComponent } from '@shared/components/icons';

@Component({
  selector: 'app-catalogo-card',
  standalone: true,
  imports: [CommonModule, IconEditComponent, IconTrashComponent],
  template: `
    <div class="bg-white rounded-3xl overflow-hidden shadow-md hover:shadow-xl transition border border-gray-100 flex flex-col justify-between h-full">
      <div>
        <div class="h-48 relative overflow-hidden bg-gray-900">
          <img
            [src]="item.imagen"
            [alt]="item.nombre"
            class="w-full h-full object-cover opacity-90 hover:scale-105 transition duration-500"
          />
          <span class="absolute top-3 right-3 bg-red-600 text-white text-xs font-bold px-3 py-1 rounded-full shadow-md">
            \${{ item.precioBase | number:'1.0-0' }} MXN
          </span>
          <span class="absolute bottom-3 left-3 bg-black/60 text-white backdrop-blur-xs text-xs px-2.5 py-1 rounded-lg flex items-center gap-1.5 font-medium">
            <span>{{ item.duracionHoras }} hrs</span>
            <span>•</span>
            <span>Capacidad: {{ item.capacidadPersonas }} personas</span>
          </span>
        </div>
        <div class="p-6 space-y-3">
          <span class="text-xs font-bold text-red-500 uppercase tracking-wider">{{ item.categoria }}</span>
          <h4 class="text-xl font-bold text-gray-900">{{ item.nombre }}</h4>
          <p class="text-xs text-gray-500 leading-relaxed">{{ item.descripcion || 'Sin descripción' }}</p>
        </div>
      </div>

      <div class="p-6 pt-0 border-t border-gray-100 mt-4 flex items-center justify-between gap-2">
        <span
          [ngClass]="item.activo ? 'text-emerald-600 bg-emerald-50' : 'text-gray-500 bg-gray-100'"
          class="px-2.5 py-1 rounded-full text-xs font-bold"
        >
          {{ item.activo ? '● Activo' : '○ Inactivo' }}
        </span>
        <div class="flex items-center gap-1.5">
          <button
            type="button"
            (click)="onEdit.emit(item)"
            class="text-xs bg-blue-600/90 hover:bg-blue-600 text-white px-3 py-2 rounded-xl font-semibold transition cursor-pointer flex items-center gap-1 shadow-xs"
          >
            <app-icon-edit cssClass="w-4 h-4"></app-icon-edit>
            <span>Editar</span>
          </button>
          <button
            type="button"
            (click)="onDelete.emit(item.id)"
            class="text-xs bg-red-900/80 hover:bg-red-800 text-red-200 border border-red-700/50 px-3 py-2 rounded-xl font-semibold transition cursor-pointer flex items-center gap-1 shadow-xs"
          >
            <app-icon-trash cssClass="w-4 h-4 text-red-400"></app-icon-trash>
            <span>Eliminar</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [],
})
export class CatalogoCardComponent {
  @Input({ required: true }) item!: CatalogoEventoItem;
  @Output() onEdit = new EventEmitter<CatalogoEventoItem>();
  @Output() onDelete = new EventEmitter<number>();
}
