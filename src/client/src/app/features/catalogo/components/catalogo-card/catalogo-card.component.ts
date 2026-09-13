import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CatalogoEventoItem } from '../../models/catalogo.models';

@Component({
  selector: 'app-catalogo-card',
  standalone: true,
  imports: [CommonModule],
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
          <span class="absolute bottom-3 left-3 bg-black/60 text-white backdrop-blur-xs text-xs px-2.5 py-1 rounded-lg">
            {{ item.duracionHoras }} horas
          </span>
        </div>
        <div class="p-6 space-y-3">
          <span class="text-xs font-bold text-red-500 uppercase tracking-wider">{{ item.categoria || 'Sociales' }}</span>
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
        <div class="flex gap-1">
          <button
            type="button"
            (click)="onEdit.emit(item)"
            class="text-xs bg-gray-100 hover:bg-gray-200 text-gray-800 px-3 py-1.5 rounded-lg font-semibold transition cursor-pointer"
          >
            Editar
          </button>
          <button
            type="button"
            (click)="onDelete.emit(item.id)"
            class="text-xs bg-red-50 hover:bg-red-100 text-red-600 px-3 py-1.5 rounded-lg font-semibold transition cursor-pointer"
          >
            Eliminar
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
