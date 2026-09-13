import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface ToastMessage {
  id: number;
  tipo: 'success' | 'error' | 'info';
  texto: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toasts$ = new BehaviorSubject<ToastMessage[]>([]);
  public toasts = this.toasts$.asObservable();

  showSuccess(texto: string, durationMs = 3500): void {
    this.addToast('success', texto, durationMs);
  }

  showError(texto: string, durationMs = 4500): void {
    this.addToast('error', texto, durationMs);
  }

  showInfo(texto: string, durationMs = 3500): void {
    this.addToast('info', texto, durationMs);
  }

  private addToast(tipo: 'success' | 'error' | 'info', texto: string, durationMs: number): void {
    const id = Date.now() + Math.random();
    const newToast: ToastMessage = { id, tipo, texto };
    const current = this.toasts$.value;
    this.toasts$.next([...current, newToast]);

    setTimeout(() => {
      this.remove(id);
    }, durationMs);
  }

  remove(id: number): void {
    const current = this.toasts$.value;
    this.toasts$.next(current.filter((t) => t.id !== id));
  }
}
