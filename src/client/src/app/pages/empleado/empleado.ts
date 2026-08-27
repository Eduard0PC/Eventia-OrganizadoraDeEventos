import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { environment } from '../../../environments/environment';
import { CotizacionesService, Cotizacion } from '../../services/cotizaciones';

export type EmployeeTab =
  | 'inicio'
  | 'catalogo'
  | 'clientes'
  | 'planificador'
  | 'cotizaciones'
  | 'pagos'
  | 'proveedores';

export interface CatalogoEventoItem {
  id: number;
  nombre: string;
  descripcion: string | null;
  precioBase: number;
  duracionHoras: number;
  activo: boolean;
  categoria?: string;
  imagen?: string;
}

export interface ClienteItem {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  telefono: string;
  ciudad: string;
  estatus: 'Activo' | 'Inactivo' | 'Prospecto';
  totalEventos: number;
  fechaRegistro: string;
}

export interface PlanificadorEvento {
  id: number;
  nombreEvento: string;
  clienteNombre: string;
  fecha: string;
  lugar: string;
  invitados: number;
  progreso: number; // 0 to 100
  estatus: 'En preparación' | 'Confirmado' | 'En ejecución' | 'Finalizado';
  encargado: string;
  checklist: { tarea: string; completado: boolean }[];
}

export interface PagoItem {
  id: string;
  folioCotizacion: string;
  clienteNombre: string;
  monto: number;
  metodo: 'Tarjeta' | 'Transferencia' | 'Efectivo';
  fecha: string;
  estatus: 'Completado' | 'Pendiente' | 'Reembolsado';
}

export interface ProveedorItem {
  id: number;
  nombreEmpresa: string;
  contacto: string;
  categoria: 'Catering' | 'Floristería' | 'Música & DJ' | 'Mobiliario' | 'Fotografía' | 'Iluminación';
  telefono: string;
  email: string;
  rating: number; // 1-5
  estatus: 'Activo' | 'En revisión' | 'Inactivo';
  precioPromedio: string;
}

@Component({
  selector: 'app-empleado',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './empleado.html',
  styleUrl: './empleado.css',
})
export class Empleado implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly router = inject(Router);

  // Tab State
  activeTab: EmployeeTab = 'inicio';
  sidebarOpen = false;

  // User Info
  empleadoNombre = 'Carlos Ruiz';
  empleadoRol = 'Coordinador Senior';
  empleadoEmail = 'empleado@eventplanner.com';

  // Search & Filters
  searchQuery = '';
  catalogoFilterCategory = 'Todos';
  cotizacionesFilterStatus = 'Todos';
  planificadorFilterStatus = 'Todos';
  clientesSearch = '';
  proveedoresCategoryFilter = 'Todos';

  // Data Collections
  catalogo: CatalogoEventoItem[] = [];
  cotizaciones: Cotizacion[] = [];
  clientes: ClienteItem[] = [];
  planificador: PlanificadorEvento[] = [];
  pagos: PagoItem[] = [];
  proveedores: ProveedorItem[] = [];

  // Loading States
  loadingCatalogo = false;
  loadingCotizaciones = false;

  // Modals State
  showNewClientModal = false;
  newClientData: Partial<ClienteItem> = {
    nombre: '',
    apellido: '',
    email: '',
    telefono: '',
    ciudad: 'Ciudad de México',
    estatus: 'Activo',
  };

  showNewQuoteModal = false;
  newQuoteData = {
    clienteId: 1,
    catalogoEventoId: 1,
    fechaEvento: '',
    invitados: 50,
    horasAdicionales: 0,
    notas: '',
  };

  showNewSupplierModal = false;
  newSupplierData: Partial<ProveedorItem> = {
    nombreEmpresa: '',
    contacto: '',
    categoria: 'Catering',
    telefono: '',
    email: '',
    rating: 5,
    estatus: 'Activo',
    precioPromedio: '$$$',
  };

  showNewPaymentModal = false;
  newPaymentData: Partial<PagoItem> = {
    folioCotizacion: '',
    clienteNombre: '',
    monto: 0,
    metodo: 'Transferencia',
    estatus: 'Completado',
  };

  selectedCotizacionDetail: Cotizacion | null = null;
  selectedPlanDetail: PlanificadorEvento | null = null;

  ngOnInit(): void {
    this.checkSession();
    this.loadCatalogo();
    this.loadCotizaciones();
    this.initMockData();
  }

  checkSession(): void {
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.email) {
          this.empleadoEmail = session.email;
          const namePart = session.email.split('@')[0];
          this.empleadoNombre = namePart.charAt(0).toUpperCase() + namePart.slice(1);
        }
        if (session?.rol) {
          this.empleadoRol = session.rol === 'empleado' ? 'Coordinador Senior' : session.rol;
        }
      } catch (e) {
        console.error('Error parsing session', e);
      }
    }
  }

  setActiveTab(tab: EmployeeTab): void {
    this.activeTab = tab;
    this.sidebarOpen = false; // Auto close mobile sidebar
  }

  loadCatalogo(): void {
    this.loadingCatalogo = true;
    this.http.get<CatalogoEventoItem[]>(`${environment.apiUrl}/api/catalogo-eventos`).subscribe({
      next: (data) => {
        this.catalogo = data.map((item) => ({
          ...item,
          categoria: this.assignCategory(item.nombre),
          imagen: this.getEventImage(item.nombre),
        }));
        this.loadingCatalogo = false;
      },
      error: () => {
        // Fallback default catalog if backend API isn't live
        this.catalogo = [
          {
            id: 1,
            nombre: 'Boda Elegante Premium',
            descripcion: 'Servicio completo de banquete, decoración floral, iluminación, música en vivo y coordinación total.',
            precioBase: 45000,
            duracionHoras: 8,
            activo: true,
            categoria: 'Bodas',
            imagen: 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80',
          },
          {
            id: 2,
            nombre: 'Gala Corporativa',
            descripcion: 'Montaje de auditorio/escenario, catering ejecutivo, ambientación de marca y sistema audiovisual.',
            precioBase: 38000,
            duracionHoras: 6,
            activo: true,
            categoria: 'Corporativos',
            imagen: 'https://images.unsplash.com/photo-1511578314322-379afb476865?w=600&auto=format&fit=crop&q=80',
          },
          {
            id: 3,
            nombre: 'Fiesta de XV Años',
            descripcion: 'Pista iluminada, DJ profesional, catering juvenil, mesa de dulces gourmet y vals con efectos.',
            precioBase: 32000,
            duracionHoras: 7,
            activo: true,
            categoria: 'Sociales',
            imagen: 'https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=600&auto=format&fit=crop&q=80',
          },
          {
            id: 4,
            nombre: 'Graduación Universitaria',
            descripcion: 'Recepción, cena de 3 tiempos, brindis, sonido e iluminación inteligente y alfombra roja.',
            precioBase: 29000,
            duracionHoras: 6,
            activo: true,
            categoria: 'Graduaciones',
            imagen: 'https://images.unsplash.com/photo-1523580494863-6f3031224c94?w=600&auto=format&fit=crop&q=80',
          },
          {
            id: 5,
            nombre: 'Aniversario & Cóctel Exclusive',
            descripcion: 'Barra de coctelería de autor, bocadillos finos, cuarteto de cuerdas y salas lounge.',
            precioBase: 25000,
            duracionHoras: 5,
            activo: true,
            categoria: 'Cócteles',
            imagen: 'https://images.unsplash.com/photo-1517457373958-b7bdd4587205?w=600&auto=format&fit=crop&q=80',
          },
        ];
        this.loadingCatalogo = false;
      },
    });
  }

  loadCotizaciones(): void {
    this.loadingCotizaciones = true;
    this.cotizacionesService.getCotizaciones().subscribe({
      next: (data) => {
        this.cotizaciones = data;
        this.loadingCotizaciones = false;
      },
      error: () => {
        // Fallback default list for cotizaciones
        this.cotizaciones = [
          {
            id: 101,
            folio: 'COT-2026-4821',
            evento: 'Boda Elegante Premium',
            fechaEvento: '2026-10-15',
            invitados: 180,
            total: 48000,
            descuento: 2000,
            totalFinal: 46000,
            estatus: 'enviada',
            fechaVigencia: '2026-09-15',
            fechaCreacion: '2026-08-20',
            notas: 'Solicitó menú vegetariano para 15 personas.',
            items: [
              { id: 1, descripcion: 'Paquete Boda Elegante Premium', monto: 45000 },
              { id: 2, descripcion: 'Horas adicionales (3 hrs)', monto: 3000 },
            ],
          },
          {
            id: 102,
            folio: 'COT-2026-3912',
            evento: 'Gala Corporativa',
            fechaEvento: '2026-11-05',
            invitados: 250,
            total: 38000,
            descuento: 0,
            totalFinal: 38000,
            estatus: 'aceptada',
            fechaVigencia: '2026-09-01',
            fechaCreacion: '2026-08-15',
            notas: 'Requiere pantalla LED gigante y traducción simultánea.',
            items: [{ id: 3, descripcion: 'Gala Corporativa Full', monto: 38000 }],
          },
          {
            id: 103,
            folio: 'COT-2026-1093',
            evento: 'Fiesta de XV Años',
            fechaEvento: '2026-09-28',
            invitados: 120,
            total: 34000,
            descuento: 1000,
            totalFinal: 33000,
            estatus: 'rechazada',
            fechaVigencia: '2026-08-30',
            fechaCreacion: '2026-08-01',
            notas: 'El cliente eligió otra fecha.',
            items: [{ id: 4, descripcion: 'Fiesta XV Años Deluxe', monto: 34000 }],
          },
        ];
        this.loadingCotizaciones = false;
      },
    });
  }

  initMockData(): void {
    // Clientes
    this.clientes = [
      {
        id: 1,
        nombre: 'Mariana',
        apellido: 'González',
        email: 'mariana.gonzalez@example.com',
        telefono: '+52 55 1234 5678',
        ciudad: 'Ciudad de México',
        estatus: 'Activo',
        totalEventos: 2,
        fechaRegistro: '2026-01-15',
      },
      {
        id: 2,
        nombre: 'Roberto',
        apellido: 'Mendoza',
        email: 'r.mendoza@techcorp.com',
        telefono: '+52 81 9876 5432',
        ciudad: 'Monterrey',
        estatus: 'Activo',
        totalEventos: 4,
        fechaRegistro: '2025-11-20',
      },
      {
        id: 3,
        nombre: 'Sofía',
        apellido: 'Alarcón',
        email: 'sofia.alarcon@gmail.com',
        telefono: '+52 33 4567 8901',
        ciudad: 'Guadalajara',
        estatus: 'Prospecto',
        totalEventos: 0,
        fechaRegistro: '2026-08-02',
      },
      {
        id: 4,
        nombre: 'Alejandro',
        apellido: 'Vargas',
        email: 'avargas@innovate.io',
        telefono: '+52 55 8765 4321',
        ciudad: 'Querétaro',
        estatus: 'Activo',
        totalEventos: 1,
        fechaRegistro: '2026-05-10',
      },
      {
        id: 5,
        nombre: 'Lucía',
        apellido: 'Fernández',
        email: 'lucia.fdez@hotmail.com',
        telefono: '+52 99 2345 6789',
        ciudad: 'Mérida',
        estatus: 'Inactivo',
        totalEventos: 1,
        fechaRegistro: '2025-06-18',
      },
    ];

    // Planificador de Eventos
    this.planificador = [
      {
        id: 1,
        nombreEvento: 'Boda González & Valdés',
        clienteNombre: 'Mariana González',
        fecha: '2026-10-15',
        lugar: 'Hacienda Los Morales',
        invitados: 180,
        progreso: 75,
        estatus: 'En preparación',
        encargado: 'Carlos Ruiz',
        checklist: [
          { tarea: 'Confirmación de Menú y Degustación', completado: true },
          { tarea: 'Diseño de Arreglos Florales', completado: true },
          { tarea: 'Prueba de Sonido e Iluminación', completado: true },
          { tarea: 'Montaje de Toldos y Pista', completado: false },
          { tarea: 'Coordinación con Fotógrafo', completado: false },
        ],
      },
      {
        id: 2,
        nombreEvento: 'Aniversario Anual TechCorp',
        clienteNombre: 'Roberto Mendoza',
        fecha: '2026-11-05',
        lugar: 'Centro de Convenciones Santa Fe',
        invitados: 250,
        progreso: 40,
        estatus: 'Confirmado',
        encargado: 'Andrea Silva',
        checklist: [
          { tarea: 'Reserva de Salón Principal', completado: true },
          { tarea: 'Selección de Menú Ejecutivo', completado: true },
          { tarea: 'Contratación de Presentador', completado: false },
          { tarea: 'Diseño de Renders Audiovisuales', completado: false },
        ],
      },
      {
        id: 3,
        nombreEvento: 'XV Años Valentina',
        clienteNombre: 'Alejandro Vargas',
        fecha: '2026-09-12',
        lugar: 'Jardín Bellavista',
        invitados: 140,
        progreso: 90,
        estatus: 'En ejecución',
        encargado: 'Carlos Ruiz',
        checklist: [
          { tarea: 'Instalación de Pista de Cristal', completado: true },
          { tarea: 'Montaje de Mesa de Dulces', completado: true },
          { tarea: 'Llegada de DJ y Grupo de Baile', completado: true },
          { tarea: 'Bienvenida de Invitados', completado: false },
        ],
      },
      {
        id: 4,
        nombreEvento: 'Gala de Graduación ITESM',
        clienteNombre: 'Lucía Fernández',
        fecha: '2026-07-20',
        lugar: 'Hotel Presidente InterContinental',
        invitados: 300,
        progreso: 100,
        estatus: 'Finalizado',
        encargado: 'David Morales',
        checklist: [
          { tarea: 'Cierre de Contrato', completado: true },
          { tarea: 'Banquete de 3 Tiempos', completado: true },
          { tarea: 'Liquidación a Proveedores', completado: true },
        ],
      },
    ];

    // Pagos
    this.pagos = [
      {
        id: 'PAG-2026-0091',
        folioCotizacion: 'COT-2026-4821',
        clienteNombre: 'Mariana González',
        monto: 23000,
        metodo: 'Transferencia',
        fecha: '2026-08-21',
        estatus: 'Completado',
      },
      {
        id: 'PAG-2026-0088',
        folioCotizacion: 'COT-2026-3912',
        clienteNombre: 'Roberto Mendoza',
        monto: 19000,
        metodo: 'Tarjeta',
        fecha: '2026-08-16',
        estatus: 'Completado',
      },
      {
        id: 'PAG-2026-0075',
        folioCotizacion: 'COT-2026-1093',
        clienteNombre: 'Alejandro Vargas',
        monto: 16500,
        metodo: 'Efectivo',
        fecha: '2026-08-05',
        estatus: 'Completado',
      },
      {
        id: 'PAG-2026-0095',
        folioCotizacion: 'COT-2026-4821',
        clienteNombre: 'Mariana González',
        monto: 23000,
        metodo: 'Transferencia',
        fecha: '2026-09-30',
        estatus: 'Pendiente',
      },
    ];

    // Proveedores
    this.proveedores = [
      {
        id: 1,
        nombreEmpresa: 'Gourmet Catering & Co.',
        contacto: 'Chef Laura Prieto',
        categoria: 'Catering',
        telefono: '+52 55 4433 2211',
        email: 'contacto@gourmetcatering.mx',
        rating: 5,
        estatus: 'Activo',
        precioPromedio: '$$$$',
      },
      {
        id: 2,
        nombreEmpresa: 'Flores & Armonía Floral Design',
        contacto: 'Beatriz Solís',
        categoria: 'Floristería',
        telefono: '+52 55 9988 7766',
        email: 'ventas@floresyarmonia.com',
        rating: 4,
        estatus: 'Activo',
        precioPromedio: '$$$',
      },
      {
        id: 3,
        nombreEmpresa: 'Audiovisuales & Sound Pro',
        contacto: 'Ing. Fernando Ramos',
        categoria: 'Música & DJ',
        telefono: '+52 81 1122 3344',
        email: 'soporte@soundpro.mx',
        rating: 5,
        estatus: 'Activo',
        precioPromedio: '$$$',
      },
      {
        id: 4,
        nombreEmpresa: 'Mobiliario Premier Lounge',
        contacto: 'Santiago Castro',
        categoria: 'Mobiliario',
        telefono: '+52 33 6677 8899',
        email: 'rentas@mobiliariopremier.com',
        rating: 4,
        estatus: 'Activo',
        precioPromedio: '$$',
      },
      {
        id: 5,
        nombreEmpresa: 'Lumina FX Light Show',
        contacto: 'Jorge Rivas',
        categoria: 'Iluminación',
        telefono: '+52 55 7766 5544',
        email: 'info@luminafx.com',
        rating: 4,
        estatus: 'En revisión',
        precioPromedio: '$$$',
      },
    ];
  }

  // Calculated Metrics for Dashboard (Inicio)
  get totalEventosActivos(): number {
    return this.planificador.filter((e) => e.estatus !== 'Finalizado').length;
  }

  get totalCotizacionesPendientes(): number {
    return this.cotizaciones.filter((c) => c.estatus === 'enviada' || (c.estatus as string) === 'pendiente').length;
  }

  get ingresosTotalesMes(): number {
    return this.pagos
      .filter((p) => p.estatus === 'Completado')
      .reduce((acc, curr) => acc + curr.monto, 0);
  }

  get totalClientes(): number {
    return this.clientes.length;
  }

  // Filtered Getters
  get filteredCatalogo(): CatalogoEventoItem[] {
    return this.catalogo.filter((item) => {
      const matchesSearch =
        item.nombre.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (item.descripcion && item.descripcion.toLowerCase().includes(this.searchQuery.toLowerCase()));
      const matchesCat =
        this.catalogoFilterCategory === 'Todos' || item.categoria === this.catalogoFilterCategory;
      return matchesSearch && matchesCat;
    });
  }

  get filteredCotizaciones(): Cotizacion[] {
    return this.cotizaciones.filter((c) => {
      const matchesSearch =
        c.folio.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        c.evento.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesStatus =
        this.cotizacionesFilterStatus === 'Todos' || c.estatus === this.cotizacionesFilterStatus;
      return matchesSearch && matchesStatus;
    });
  }

  get filteredClientes(): ClienteItem[] {
    return this.clientes.filter((cl) => {
      const full = `${cl.nombre} ${cl.apellido} ${cl.email} ${cl.ciudad}`.toLowerCase();
      return full.includes(this.clientesSearch.toLowerCase());
    });
  }

  get filteredPlanificador(): PlanificadorEvento[] {
    return this.planificador.filter((p) => {
      const matchesSearch =
        p.nombreEvento.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        p.clienteNombre.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesStatus =
        this.planificadorFilterStatus === 'Todos' || p.estatus === this.planificadorFilterStatus;
      return matchesSearch && matchesStatus;
    });
  }

  get filteredProveedores(): ProveedorItem[] {
    return this.proveedores.filter((pv) => {
      const matchesSearch =
        pv.nombreEmpresa.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        pv.contacto.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesCat =
        this.proveedoresCategoryFilter === 'Todos' || pv.categoria === this.proveedoresCategoryFilter;
      return matchesSearch && matchesCat;
    });
  }

  // Utility Helpers
  assignCategory(nombre: string): string {
    const lower = nombre.toLowerCase();
    if (lower.includes('boda')) return 'Bodas';
    if (lower.includes('corporativ') || lower.includes('gala')) return 'Corporativos';
    if (lower.includes('xv') || lower.includes('quince')) return 'Sociales';
    if (lower.includes('gradua')) return 'Graduaciones';
    return 'Sociales';
  }

  getEventImage(nombre: string): string {
    const lower = nombre.toLowerCase();
    if (lower.includes('boda'))
      return 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80';
    if (lower.includes('gala') || lower.includes('corporativ'))
      return 'https://images.unsplash.com/photo-1511578314322-379afb476865?w=600&auto=format&fit=crop&q=80';
    if (lower.includes('xv') || lower.includes('fiesta'))
      return 'https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=600&auto=format&fit=crop&q=80';
    if (lower.includes('gradua'))
      return 'https://images.unsplash.com/photo-1523580494863-6f3031224c94?w=600&auto=format&fit=crop&q=80';
    return 'https://images.unsplash.com/photo-1517457373958-b7bdd4587205?w=600&auto=format&fit=crop&q=80';
  }

  getStatusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'aprobada':
      case 'aceptada':
      case 'completado':
      case 'activo':
      case 'finalizado':
        return 'bg-emerald-100 text-emerald-700 border-emerald-300';
      case 'enviada':
      case 'en preparación':
      case 'confirmado':
      case 'en revisión':
        return 'bg-red-100 text-red-600 border-red-200';
      case 'en ejecución':
        return 'bg-amber-100 text-amber-700 border-amber-300';
      case 'rechazada':
      case 'inactivo':
      case 'reembolsado':
        return 'bg-gray-200 text-gray-700 border-gray-300';
      default:
        return 'bg-blue-100 text-blue-700 border-blue-200';
    }
  }

  // Actions
  toggleChecklist(plan: PlanificadorEvento, index: number): void {
    plan.checklist[index].completado = !plan.checklist[index].completado;
    const doneCount = plan.checklist.filter((item) => item.completado).length;
    plan.progreso = Math.round((doneCount / plan.checklist.length) * 100);
  }

  approveCotizacion(cot: Cotizacion): void {
    cot.estatus = 'aceptada';
  }

  rejectCotizacion(cot: Cotizacion): void {
    cot.estatus = 'rechazada';
  }

  saveNewClient(): void {
    if (!this.newClientData.nombre || !this.newClientData.email) return;
    const newId = this.clientes.length + 1;
    const client: ClienteItem = {
      id: newId,
      nombre: this.newClientData.nombre || '',
      apellido: this.newClientData.apellido || '',
      email: this.newClientData.email || '',
      telefono: this.newClientData.telefono || '',
      ciudad: this.newClientData.ciudad || 'Ciudad de México',
      estatus: (this.newClientData.estatus as any) || 'Activo',
      totalEventos: 0,
      fechaRegistro: new Date().toISOString().split('T')[0],
    };
    this.clientes.unshift(client);
    this.showNewClientModal = false;
    this.newClientData = { nombre: '', apellido: '', email: '', telefono: '', ciudad: 'Ciudad de México', estatus: 'Activo' };
  }

  saveNewSupplier(): void {
    if (!this.newSupplierData.nombreEmpresa || !this.newSupplierData.contacto) return;
    const newId = this.proveedores.length + 1;
    const supp: ProveedorItem = {
      id: newId,
      nombreEmpresa: this.newSupplierData.nombreEmpresa || '',
      contacto: this.newSupplierData.contacto || '',
      categoria: (this.newSupplierData.categoria as any) || 'Catering',
      telefono: this.newSupplierData.telefono || '',
      email: this.newSupplierData.email || '',
      rating: this.newSupplierData.rating || 5,
      estatus: 'Activo',
      precioPromedio: this.newSupplierData.precioPromedio || '$$$',
    };
    this.proveedores.unshift(supp);
    this.showNewSupplierModal = false;
    this.newSupplierData = { nombreEmpresa: '', contacto: '', categoria: 'Catering', telefono: '', email: '', rating: 5, estatus: 'Activo', precioPromedio: '$$$' };
  }

  saveNewPayment(): void {
    if (!this.newPaymentData.folioCotizacion || !this.newPaymentData.monto) return;
    const newPayment: PagoItem = {
      id: `PAG-2026-0${100 + this.pagos.length}`,
      folioCotizacion: this.newPaymentData.folioCotizacion || '',
      clienteNombre: this.newPaymentData.clienteNombre || 'Cliente Registrado',
      monto: Number(this.newPaymentData.monto),
      metodo: (this.newPaymentData.metodo as any) || 'Transferencia',
      fecha: new Date().toISOString().split('T')[0],
      estatus: (this.newPaymentData.estatus as any) || 'Completado',
    };
    this.pagos.unshift(newPayment);
    this.showNewPaymentModal = false;
    this.newPaymentData = { folioCotizacion: '', clienteNombre: '', monto: 0, metodo: 'Transferencia', estatus: 'Completado' };
  }

  logout(): void {
    localStorage.removeItem('session');
    this.router.navigate(['/login']);
  }
}
