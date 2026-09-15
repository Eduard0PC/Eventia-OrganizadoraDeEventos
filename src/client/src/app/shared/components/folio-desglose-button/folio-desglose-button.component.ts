import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-folio-desglose-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      (click)="onClick($event)"
      class="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl bg-gray-50 hover:bg-red-50/80 text-gray-800 hover:text-red-700 border border-gray-200 hover:border-red-300 shadow-2xs hover:shadow-xs transition-all duration-200 cursor-pointer group focus:outline-none focus:ring-2 focus:ring-red-500/30"
      [title]="'Ver desglose completo de la cotización ' + folio"
    >
      <div class="w-6 h-6 rounded-lg bg-gray-200 group-hover:bg-red-100 text-gray-600 group-hover:text-red-600 flex items-center justify-center transition-colors">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
      </div>

      <div class="flex items-center gap-1.5 text-xs">
        <span class="font-black font-mono tracking-tight text-gray-900 group-hover:text-red-700">
          {{ folio }}
        </span>
        <span class="inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-bold bg-white group-hover:bg-red-600 text-gray-600 group-hover:text-white border border-gray-200 group-hover:border-red-600 transition-colors shadow-2xs">
          Ver Desglose
        </span>
      </div>
    </button>
  `
})
export class FolioDesgloseButtonComponent {
  @Input({ required: true }) folio: string = '';
  @Output() onDesgloseClick = new EventEmitter<string>();

  onClick(event: Event): void {
    event.stopPropagation();
    if (this.folio) {
      this.onDesgloseClick.emit(this.folio);
    }
  }
}
