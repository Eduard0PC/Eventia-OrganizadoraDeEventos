import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed top-5 right-5 z-50 flex flex-col gap-3 max-w-sm w-full pointer-events-none">
      <div
        *ngFor="let toast of toastService.toasts | async"
        class="pointer-events-auto flex items-center justify-between p-4 rounded-2xl shadow-xl border backdrop-blur-md transition-all duration-300 transform translate-y-0"
        [ngClass]="{
          'bg-emerald-600 text-white border-emerald-500 shadow-emerald-900/20': toast.tipo === 'success',
          'bg-red-600 text-white border-red-500 shadow-red-900/20': toast.tipo === 'error',
          'bg-gray-800 text-white border-gray-700 shadow-gray-900/20': toast.tipo === 'info'
        }"
      >
        <div class="flex items-center gap-3">
          <p class="text-xs font-bold leading-snug">{{ toast.texto }}</p>
        </div>
        <button
          type="button"
          (click)="toastService.remove(toast.id)"
          class="ml-3 text-white/70 hover:text-white font-bold text-sm cursor-pointer"
        >
          ✕
        </button>
      </div>
    </div>
  `,
  styles: [],
})
export class ToastComponent {
  public readonly toastService = inject(ToastService);
}
