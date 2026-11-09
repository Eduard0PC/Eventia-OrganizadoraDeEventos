import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  AfterViewInit,
  ElementRef,
  ViewChild,
  inject,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventosService } from '@features/eventos/services/eventos.service';
import { ContratoDocumento } from '@core/models/evento.models';

@Component({
  selector: 'app-contrato-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './contrato-modal.component.html',
  styleUrl: './contrato-modal.component.css'
})
export class ContratoModalComponent implements OnChanges, AfterViewInit {
  @Input() show = false;
  @Input() eventoId: number | null = null;
  @Input() contratoId: number | null = null;

  @Output() onClose = new EventEmitter<void>();

  @ViewChild('dialogEl') dialogRef?: ElementRef<HTMLDialogElement>;
  @ViewChild('contratoSheet') contratoSheetRef?: ElementRef<HTMLElement>;

  private readonly eventosService = inject(EventosService);
  private readonly cdr = inject(ChangeDetectorRef);

  contrato: ContratoDocumento | null = null;
  cargando = false;
  error: string | null = null;

  private fallbackInitialized = false;

  ngAfterViewInit(): void {
    this.setupClosedByFallback();
    this.syncDialogState();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['show'] || changes['eventoId'] || changes['contratoId']) {
      this.syncDialogState();
      if (this.show && (this.eventoId || this.contratoId)) {
        this.cargarContrato();
      } else if (!this.show) {
        this.contrato = null;
        this.error = null;
        this.cargando = false;
      }
    }
  }

  private syncDialogState(): void {
    const dialog = this.dialogRef?.nativeElement;
    if (!dialog) return;

    if (this.show && !dialog.open) {
      dialog.showModal();
    } else if (!this.show && dialog.open) {
      dialog.close();
    }
  }

  private setupClosedByFallback(): void {
    if (this.fallbackInitialized || typeof window === 'undefined') return;
    const dialog = this.dialogRef?.nativeElement;
    if (!dialog) return;

    this.fallbackInitialized = true;
    if (!('closedBy' in HTMLDialogElement.prototype)) {
      const outside = (e: PointerEvent, dlg: HTMLDialogElement) => {
        const r = dlg.getBoundingClientRect();
        return e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom;
      };
      let pressedOutside = false;
      dialog.addEventListener('pointerdown', (e: PointerEvent) => {
        pressedOutside = outside(e, dialog);
      });
      dialog.addEventListener('pointerup', (e: PointerEvent) => {
        if (pressedOutside && outside(e, dialog)) {
          dialog.close();
        }
      });
    }
  }

  cargarContrato(): void {
    if (!this.eventoId && !this.contratoId) return;

    this.cargando = true;
    this.error = null;
    this.cdr.detectChanges();

    const request$ = this.eventoId
      ? this.eventosService.getContratoByEventoId(this.eventoId)
      : this.eventosService.getContratoByContratoId(this.contratoId!);

    request$.subscribe({
      next: (data) => {
        this.contrato = data;
        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar el documento del contrato:', err);
        this.error = err?.error?.error || 'No fue posible recuperar la información del contrato desde la base de datos.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  cerrarModal(): void {
    const dialog = this.dialogRef?.nativeElement;
    if (dialog && dialog.open) {
      dialog.close();
    } else {
      this.onClose.emit();
    }
  }

  onNativeDialogClose(): void {
    const dialog = this.dialogRef?.nativeElement;
    if (dialog) {
      dialog.returnValue = '';
    }
    if (this.show) {
      this.onClose.emit();
    }
  }

  imprimirContrato(): void {
    if (!this.contrato) return;

    const sheetEl = this.contratoSheetRef?.nativeElement;
    if (!sheetEl) {
      window.print();
      return;
    }

    const ventanaImpresion = window.open('', '_blank', 'width=960,height=800');
    if (!ventanaImpresion) {
      window.print();
      return;
    }

    const tituloArchivo = `Contrato_${this.contrato.folioContrato}_Eventia`;
    const contenidoHtml = sheetEl.innerHTML;

    ventanaImpresion.document.open();
    ventanaImpresion.document.write(`<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>${tituloArchivo}</title>
  <script src="https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4"></script>
  <style>
    @page {
      size: letter;
      margin: 14mm 15mm;
    }
    body {
      font-family: 'Georgia', 'Times New Roman', Times, serif;
      color: #111827;
      background: #ffffff;
      line-height: 1.55;
      -webkit-print-color-adjust: exact !important;
      print-color-adjust: exact !important;
      margin: 0;
      padding: 0;
    }
    .contrato-sans {
      font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }
    .sello-oficial {
      border: 2px double #991b1b;
      color: #991b1b;
      transform: rotate(-3deg);
    }
    table, tr, section {
      page-break-inside: avoid;
    }
  </style>
</head>
<body class="bg-white text-gray-900">
  <div class="max-w-4xl mx-auto">
    ${contenidoHtml}
  </div>
  <script>
    window.addEventListener('load', function() {
      setTimeout(function() {
        window.focus();
        window.print();
      }, 350);
    });
  </script>
</body>
</html>`);
    ventanaImpresion.document.close();
  }

  getEstatusContratoLabel(estatus: string): string {
    const map: Record<string, string> = {
      activo: 'Vigente / Activo',
      en_proceso: 'En Ejecución Financiera',
      completado: 'Liquidado y Cumplido',
      cancelado: 'Rescindido / Cancelado'
    };
    return map[(estatus || '').toLowerCase()] || estatus;
  }

  formatearFechaLarga(fecha?: string | null): string {
    if (!fecha) return 'Por confirmar';
    const clean = fecha.includes('T') ? fecha : `${fecha}T12:00:00`;
    return new Date(clean).toLocaleDateString('es-MX', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatearFechaCorta(fecha?: string | null): string {
    if (!fecha) return 'Por definir';
    const clean = fecha.includes('T') ? fecha : `${fecha}T12:00:00`;
    return new Date(clean).toLocaleDateString('es-MX', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  montoEnLetras(monto: number): string {
    const entero = Math.floor(Math.abs(monto || 0));
    const centavos = Math.round((Math.abs(monto || 0) - entero) * 100);
    const centavosStr = `${centavos.toString().padStart(2, '0')}/100 M.N.`;
    return `${this.convertirEnteroALetras(entero)} PESOS ${centavosStr}`;
  }

  private convertirEnteroALetras(n: number): string {
    if (n === 0) return 'CERO';
    const unidades = ['', 'UN', 'DOS', 'TRES', 'CUATRO', 'CINCO', 'SEIS', 'SIETE', 'OCHO', 'NUEVE'];
    const especiales = ['DIEZ', 'ONCE', 'DOCE', 'TRECE', 'CATORCE', 'QUINCE', 'DIECISÉIS', 'DIECISIETE', 'DIECIOCHO', 'DIECINUEVE'];
    const decenas = ['', 'DIEZ', 'VEINTE', 'TREINTA', 'CUARENTA', 'CINCUENTA', 'SESENTA', 'SETENTA', 'OCHENTA', 'NOVENTA'];
    const centenas = ['', 'CIENTO', 'DOSCIENTOS', 'TRESCIENTOS', 'CUATROCIENTOS', 'QUINIENTOS', 'SEISCIENTOS', 'SETECIENTOS', 'OCHOCIENTOS', 'NOVECIENTOS'];

    const convertirMenorMil = (num: number): string => {
      if (num === 0) return '';
      if (num === 100) return 'CIEN';
      const c = Math.floor(num / 100);
      const resto = num % 100;
      let texto = c > 0 ? centenas[c] + ' ' : '';

      if (resto > 0) {
        if (resto < 10) {
          texto += unidades[resto];
        } else if (resto < 20) {
          texto += especiales[resto - 10];
        } else if (resto === 20) {
          texto += 'VEINTE';
        } else if (resto < 30) {
          texto += 'VEINTI' + unidades[resto - 20];
        } else {
          const d = Math.floor(resto / 10);
          const u = resto % 10;
          texto += decenas[d] + (u > 0 ? ' Y ' + unidades[u] : '');
        }
      }
      return texto.trim();
    };

    if (n >= 1000000) {
      const millones = Math.floor(n / 1000000);
      const restoMillon = n % 1000000;
      const prefijoMillon = millones === 1 ? 'UN MILLÓN' : `${this.convertirEnteroALetras(millones)} MILLONES`;
      return (prefijoMillon + (restoMillon > 0 ? ' ' + this.convertirEnteroALetras(restoMillon) : '')).trim();
    }

    if (n >= 1000) {
      const miles = Math.floor(n / 1000);
      const restoMil = n % 1000;
      const prefijoMil = miles === 1 ? 'MIL' : `${convertirMenorMil(miles)} MIL`;
      return (prefijoMil + (restoMil > 0 ? ' ' + convertirMenorMil(restoMil) : '')).trim();
    }

    return convertirMenorMil(n);
  }
}

