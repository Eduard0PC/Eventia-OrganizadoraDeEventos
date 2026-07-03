import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NavbarClient } from '../../components/navbar-client/navbar-client';
import { environment } from '../../../environments/environment';

export interface CatalogoEvento {
  id: number;
  nombre: string;
  descripcion: string | null;
  precioBase: number;
  duracionHoras: number;
  activo: boolean;
  imagen?: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, NavbarClient, FormsModule],
  templateUrl: './home.html'
})
export class Home implements OnInit {
  private readonly http = inject(HttpClient);
  eventos: CatalogoEvento[] = [];

  selectedEvento: CatalogoEvento | null = null;
  showDetailsModal = false;
  showQuoteModal = false;

  fechaEvento = '';
  cantidadInvitados: number | null = null;
  horasAdicionales = 0;
  cateringSelected = false;
  musicaSelected = false;
  decoracionSelected = false;
  notasEspeciales = '';
  cotizacionEnviada = false;

  ngOnInit(): void {
    this.http.get<CatalogoEvento[]>(`${environment.apiUrl}/api/catalogo-eventos`).subscribe({
      next: (data) => {
        this.eventos = data.map(evento => ({
          ...evento,
          imagen: this.getEventImage(evento.nombre)
        }));
      },
      error: (err) => {
        console.error('Error fetching events:', err);
      }
    });
  }

  openDetails(evento: CatalogoEvento): void {
    this.selectedEvento = evento;
    this.showDetailsModal = true;
    this.cotizacionEnviada = false;
  }

  closeDetails(): void {
    this.showDetailsModal = false;
    this.selectedEvento = null;
  }

  openQuote(): void {
    this.showDetailsModal = false;
    this.showQuoteModal = true;
    this.cotizacionEnviada = false;
  
    this.fechaEvento = '';
    this.cantidadInvitados = null;
    this.horasAdicionales = 0;
    this.cateringSelected = false;
    this.musicaSelected = false;
    this.decoracionSelected = false;
    this.notasEspeciales = '';
  }

  closeQuote(): void {
    this.showQuoteModal = false;
    this.selectedEvento = null;
  }

  getEstimatedTotal(): number {
    if (!this.selectedEvento) return 0;
    let total = Number(this.selectedEvento.precioBase);
    
    total += (this.horasAdicionales || 0) * 1000;

    if (this.cateringSelected) {
      total += (this.cantidadInvitados || 0) * 250;
    }
    if (this.musicaSelected) {
      total += 5000;
    }
    if (this.decoracionSelected) {
      total += 3000;
    }

    return total;
  }

  enviarCotizacion(): void {
    this.cotizacionEnviada = true;
    console.log('Cotización simulada enviada con los siguientes datos:', {
      evento: this.selectedEvento,
      fecha: this.fechaEvento,
      invitados: this.cantidadInvitados,
      horasAdicionales: this.horasAdicionales,
      servicios: {
        catering: this.cateringSelected,
        musica: this.musicaSelected,
        decoracion: this.decoracionSelected
      },
      notas: this.notasEspeciales,
      totalEstimado: this.getEstimatedTotal()
    });
  }

  private getEventImage(nombre: string): string {
    const normalized = nombre.toLowerCase();
    if (normalized.includes('premium')) {
      return 'https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=600&auto=format&fit=crop&q=80';
    } else if (normalized.includes('boda')) {
      return 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80';
    } else if (normalized.includes('xv') || normalized.includes('quince') || normalized.includes('15')) {
      return 'https://blogger.googleusercontent.com/img/b/R29vZ2xl/AVvXsEiyRMTv3s0xnDo4619Uh5SBivxzn30mxEqgRRb0Y03suoB99LkglTlmLiEfwk6nlgLEF70Vxearg6R1y9-rXO0effTzdoQKEWd_gdKaUWYfWguZIY9dZ09j_LeN0eyIxgGjKCgKNGq3zRDS/s640/quincea%25C3%25B1era1.jpg';
    } else if (normalized.includes('corporativo') || normalized.includes('conferencia') || normalized.includes('empresa')) {
      return 'https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=600&auto=format&fit=crop&q=80';
    } else if (normalized.includes('gradua')) {
      return 'https://media.istockphoto.com/id/1480277406/es/foto/la-graduaci%C3%B3n-el-grupo-y-la-vista-trasera-de-los-estudiantes-celebran-el-%C3%A9xito-educativo.jpg?s=612x612&w=0&k=20&c=MX839Ue5i3yXjWLp6Ak-dSHn8OMlwqBLT34QfTKxSfM=';
    } 
    return 'https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=600&auto=format&fit=crop&q=80';
  }
}