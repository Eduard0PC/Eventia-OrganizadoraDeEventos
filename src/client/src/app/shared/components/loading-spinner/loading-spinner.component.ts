import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      [ngClass]="{
        'py-12 flex flex-col items-center justify-center space-y-3': true,
        'bg-gray-900/60 rounded-3xl border border-gray-800 p-8 shadow-xl text-white': theme === 'dark',
        'bg-white/70 backdrop-blur-sm rounded-3xl border border-gray-100 p-8 shadow-sm text-gray-800': theme === 'light',
        'text-gray-400': theme === 'simple'
      }"
    >
      <div
        [ngClass]="{
          'w-8 h-8 border-4 border-red-600 border-t-transparent rounded-full animate-spin': size === 'md',
          'w-12 h-12 border-4 border-red-600 border-t-transparent rounded-full animate-spin': size === 'lg',
          'w-6 h-6 border-3 border-red-600 border-t-transparent rounded-full animate-spin': size === 'sm'
        }"
      ></div>
      <div class="text-center space-y-0.5">
        <p
          [ngClass]="{
            'text-sm font-semibold': size === 'md' || size === 'lg',
            'text-xs font-semibold': size === 'sm',
            'text-white': theme === 'dark',
            'text-gray-800': theme === 'light',
            'text-gray-300': theme === 'simple'
          }"
        >
          {{ message }}
        </p>
        <p *ngIf="subtext" class="text-xs text-gray-400">
          {{ subtext }}
        </p>
      </div>
    </div>
  `
})
export class LoadingSpinnerComponent {
  @Input() message = 'Cargando desde la base de datos...';
  @Input() subtext?: string;
  @Input() theme: 'dark' | 'light' | 'simple' = 'simple';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
}
