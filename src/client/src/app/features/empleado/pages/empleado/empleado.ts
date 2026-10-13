import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { environment } from '@env/environment';
import { Cotizacion } from '@core/models/cotizacion.models';
import { CotizacionesService } from '@features/cotizaciones/services/cotizaciones.service';
import { ClientesService } from '@features/clientes/services/clientes.service';
import { ClienteSummary, ClienteDetalle } from '@features/clientes/models/cliente.models';
import { CatalogoService } from '@features/catalogo/services/catalogo.service';
import { ToastComponent } from '@shared/components/toast/toast.component';
import { ToastService } from '@shared/components/toast/toast.service';
import { CatalogoCardComponent } from '@features/catalogo/components/catalogo-card/catalogo-card.component';
import { CatalogoFormModalComponent } from '@features/catalogo/components/catalogo-form-modal/catalogo-form-modal.component';
import { UserCard } from '@shared/components/user-card/user-card';
import { ClienteFichaModalComponent } from '@features/clientes/components/cliente-ficha-modal/cliente-ficha-modal.component';
import { CotizacionDesgloseModalComponent } from '@features/cotizaciones/components/cotizacion-desglose-modal/cotizacion-desglose-modal.component';
import { FolioDesgloseButtonComponent } from '@shared/components/folio-desglose-button/folio-desglose-button.component';
import { Proveedor, CategoriaProveedor } from '@core/models/proveedor.model';
import { ProveedorService } from '@features/proveedores/services/proveedor.service';
import { ProveedorCardComponent } from '@features/proveedores/components/proveedor-card/proveedor-card';
import { ProveedorServiciosModalComponent } from '@features/proveedores/components/proveedor-servicios-modal/proveedor-servicios-modal';
import { ProveedorFormModalComponent } from '@features/proveedores/components/proveedor-form-modal/proveedor-form-modal';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner.component';

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
  descripcion?: string | null;
  precioBase: number;
  duracionHoras: number;
  activo: boolean;
  imagenUrl?: string | null;
  categoria?: string;
  capacidadPersonas?: number;
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
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ToastComponent,
    CatalogoCardComponent,
    CatalogoFormModalComponent,
    UserCard,
    ClienteFichaModalComponent,
    CotizacionDesgloseModalComponent,
    ProveedorCardComponent,
    ProveedorServiciosModalComponent,
    ProveedorFormModalComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './empleado.html',
  styleUrl: './empleado.css',
})
export class Empleado implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly cotizacionesService = inject(CotizacionesService);
  private readonly clientesService = inject(ClientesService);
  private readonly catalogoService = inject(CatalogoService);
  private readonly proveedorService = inject(ProveedorService);
  private readonly toastService = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
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
  clientes: ClienteSummary[] = [];
  planificador: PlanificadorEvento[] = [];
  pagos: PagoItem[] = [];
  proveedores: Proveedor[] = [];
  categoriasProveedores: CategoriaProveedor[] = [];

  // Loading States
  loadingCatalogo = false;
  loadingCotizaciones = false;
  loadingClientes = false;
  loadingProveedores = false;
  loadingFicha = false;

  // Ficha de cliente modal state
  showFichaClienteModal = false;
  selectedClienteId: number | null = null;
  selectedClienteSummary: ClienteSummary | null = null;
  clienteFicha: ClienteDetalle | null = null;
  fichaActiveTab: 'pagos' | 'servicios' | 'eventos' = 'pagos';

  // Desglose cotizacion modal state
  showCotizacionDetailModal = false;
  selectedCotizacionDetail: Cotizacion | null = null;
  selectedCotizacionFolio: string | null = null;

  // Modals Proveedor state
  proveedorVerServicios: Proveedor | null = null;
  proveedorEditando: Proveedor | null = null;
  mostrandoFormModal = false;

  // Modals State
  showNewClientModal = false;
  newClientData: Partial<ClienteItem> & { password?: string; confirmPassword?: string } = {
    nombre: '',
    apellido: '',
    email: '',
    telefono: '',
    password: '',
    confirmPassword: '',
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
  newSupplierData = {
    nombre: '',
    contacto: '',
    categoriaId: 1,
    telefono: '',
    email: '',
    direccion: '',
    sitioWeb: '',
    rfc: '',
    rating: 5,
    precioPromedio: '$$$',
  };

  showNewPaymentModal = false;
  newPaymentData = {
    folioCotizacion: '',
    clienteNombre: '',
    monto: 0,
    metodo: 'Transferencia' as const,
    estatus: 'Completado' as const,
  };

  showNewCatalogModal = false;
  showEditCatalogModal = false;
  newCatalogData = {
    nombre: '',
    descripcion: '',
    precioBase: 0,
    duracionHoras: 4,
    activo: true,
    imagenUrl: '',
  };
  editCatalogData: (CatalogoEventoItem & { imagenUrl?: string | null }) | null = null;
  selectedImageFile: File | null = null;
  imagePreviewUrl: string | null = null;
  guardandoCatalogo = false;
  selectedPlanDetail: PlanificadorEvento | null = null;

  ngOnInit(): void {
    this.checkSession();
    this.loadCatalogo();
    this.loadCotizaciones();
    this.loadClientes();
    this.loadProveedores();
    this.loadCategoriasProveedores();
    this.initMockData();
  }

  loadProveedores(): void {
    this.loadingProveedores = true;
    this.cdr.detectChanges();
    this.proveedorService.getProveedores(undefined, true).subscribe({
      next: (data) => {
        this.proveedores = data || [];
        this.loadingProveedores = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar proveedores:', err);
        this.proveedores = [];
        this.loadingProveedores = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadCategoriasProveedores(): void {
    this.proveedorService.getCategorias(false).subscribe({
      next: (data) => {
        this.categoriasProveedores = data || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error al cargar categorías de proveedores:', err)
    });
  }

  loadClientes(): void {
    this.loadingClientes = true;
    this.clientesService.getClientes().subscribe({
      next: (data) => {
        this.clientes = data;
        this.loadingClientes = false;
      },
      error: (err) => {
        console.error('Error al cargar clientes desde backend:', err);
        this.loadingClientes = false;
      },
    });
  }

  abrirFichaCliente(cliente: number | ClienteSummary): void {
    if (typeof cliente === 'number') {
      this.selectedClienteId = cliente;
      this.selectedClienteSummary = this.clientes.find(c => c.id === cliente) || null;
    } else {
      this.selectedClienteId = cliente.id;
      this.selectedClienteSummary = cliente;
    }
    this.showFichaClienteModal = true;
  }

  cerrarFichaCliente(): void {
    this.showFichaClienteModal = false;
    this.selectedClienteId = null;
    this.selectedClienteSummary = null;
  }

  checkSession(): void {
    const sessionStr = localStorage.getItem('session');
    if (sessionStr) {
      try {
        const session = JSON.parse(sessionStr);
        if (session?.empleado?.nombre) {
          const apellido = session.empleado.apellido ? ` ${session.empleado.apellido}` : '';
          this.empleadoNombre = `${session.empleado.nombre}${apellido}`;
        } else if (session?.email) {
          this.empleadoEmail = session.email;
          const namePart = session.email.split('@')[0];
          this.empleadoNombre = namePart.charAt(0).toUpperCase() + namePart.slice(1);
        }
        if (session?.rol) {
          const lower = session.rol.toLowerCase();
          if (lower === 'empleado') this.empleadoRol = 'Empleado';
          else if (lower === 'admin' || lower === 'administrador') this.empleadoRol = 'Administrador';
          else this.empleadoRol = session.rol.charAt(0).toUpperCase() + session.rol.slice(1);
        }
      } catch (e) {
        console.error('Error parsing session', e);
      }
    }
  }

  setActiveTab(tab: EmployeeTab): void {
    this.activeTab = tab;
    this.sidebarOpen = false; // Auto close mobile sidebar
    if (tab === 'catalogo') {
      this.loadCatalogo();
    } else if (tab === 'proveedores') {
      this.loadProveedores();
      this.loadCategoriasProveedores();
    }
    this.cdr.detectChanges();
  }

  loadCatalogo(): void {
    this.loadingCatalogo = true;
    this.cdr.detectChanges();
    this.catalogoService.getCatalogoEventos(true).subscribe({
      next: (data) => {
        if (data && data.length > 0) {
          this.catalogo = data.map((item) => {
            let img = this.getEventImage(item.nombre);
            if (item.imagenUrl) {
              img = item.imagenUrl.startsWith('http') ? item.imagenUrl : `${environment.apiUrl}${item.imagenUrl}`;
            }
            return {
              ...item,
              categoria: item.categoria,
              imagen: img,
            };
          });
        }
        this.loadingCatalogo = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error al cargar catálogo de la base de datos:', err);
        this.loadingCatalogo = false;
        this.cdr.detectChanges();
      },
    });
  }

  initMockCatalogo(): void {
    if (this.catalogo.length === 0) {
      this.catalogo = [
        {
          id: 1,
          nombre: 'Boda Elegante Premium',
          descripcion: 'Banquete de 3 tiempos, decoración floral deluxe, DJ profesional, luces robóticas y barra libre.',
          precioBase: 45000,
          duracionHoras: 8,
          activo: true,
          categoria: 'Bodas',
          imagen: 'https://images.unsplash.com/photo-1519741497674-611481863552?w=600&auto=format&fit=crop&q=80',
        },
        {
          id: 2,
          nombre: 'Gala Corporativa Full',
          descripcion: 'Escenario, pantallas LED, sistema de audio surround, banquete ejecutivo y maestro de ceremonias.',
          precioBase: 38000,
          duracionHoras: 6,
          activo: true,
          categoria: 'Corporativos',
          imagen: 'https://images.unsplash.com/photo-1511578314322-379afb476865?w=600&auto=format&fit=crop&q=80',
        },
        {
          id: 3,
          nombre: 'Fiesta XV Años Deluxe',
          descripcion: 'Pista de cristal iluminada, show de luces, cabina fotográfica, robot LED y mesas de dulces.',
          precioBase: 34000,
          duracionHoras: 7,
          activo: true,
          categoria: 'Sociales',
          imagen: 'https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=600&auto=format&fit=crop&q=80',
        },
      ];
    }
  }

  onCatalogImageSelected(evt: any): void {
    const file = evt.target.files?.[0];
    if (file) {
      this.selectedImageFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreviewUrl = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  openNewCatalogModal(): void {
    this.guardandoCatalogo = false;
    this.newCatalogData = {
      nombre: '',
      descripcion: '',
      precioBase: 0,
      duracionHoras: 4,
      activo: true,
      imagenUrl: '',
    };
    this.selectedImageFile = null;
    this.imagePreviewUrl = null;
    this.showNewCatalogModal = true;
    this.cdr.detectChanges();
  }

  saveNewCatalogEvent(): void {
    if (!this.newCatalogData.nombre || this.newCatalogData.precioBase < 0) {
      alert('Por favor especifica un nombre válido y precio mayor o igual a 0.');
      return;
    }

    this.guardandoCatalogo = true;
    this.cdr.detectChanges();

    if (this.selectedImageFile) {
      this.catalogoService.subirImagenEvento(this.selectedImageFile).subscribe({
        next: (uploadRes) => {
          this.submitCreateCatalogEvent(uploadRes.imageUrl);
        },
        error: (err) => {
          console.error('Error al subir imagen:', err);
          this.submitCreateCatalogEvent(null);
        }
      });
    } else {
      this.submitCreateCatalogEvent(null);
    }
  }

  private submitCreateCatalogEvent(imagenUrl: string | null): void {
    const request = {
      nombre: this.newCatalogData.nombre,
      descripcion: this.newCatalogData.descripcion || null,
      precioBase: Number(this.newCatalogData.precioBase) || 0,
      duracionHoras: Math.max(1, Number(this.newCatalogData.duracionHoras) || 1),
      activo: this.newCatalogData.activo,
      imagenUrl: imagenUrl
    };

    let img = this.imagePreviewUrl || this.getEventImage(request.nombre);
    if (imagenUrl) {
      img = imagenUrl.startsWith('http') ? imagenUrl : `${environment.apiUrl}${imagenUrl}`;
    }

    const newItemLocal: CatalogoEventoItem = {
      id: Date.now(),
      nombre: request.nombre,
      descripcion: request.descripcion,
      precioBase: request.precioBase,
      duracionHoras: request.duracionHoras,
      activo: request.activo,
      imagenUrl: imagenUrl,
      categoria: this.assignCategory(request.nombre),
      imagen: img,
    };

    // Actualización local inmediata y cierre de modal instantáneo
    this.catalogo = [newItemLocal, ...this.catalogo];
    this.guardandoCatalogo = false;
    this.showNewCatalogModal = false;
    this.selectedImageFile = null;
    this.imagePreviewUrl = null;
    this.cdr.detectChanges();

    this.catalogoService.crearCatalogoEvento(request).subscribe({
      next: (nuevoEvento) => {
        if (nuevoEvento) {
          let realImg = this.getEventImage(nuevoEvento.nombre);
          if (nuevoEvento.imagenUrl) {
            realImg = nuevoEvento.imagenUrl.startsWith('http') ? nuevoEvento.imagenUrl : `${environment.apiUrl}${nuevoEvento.imagenUrl}`;
          }
          const itemFormateado: CatalogoEventoItem = {
            ...nuevoEvento,
            categoria: this.assignCategory(nuevoEvento.nombre),
            imagen: realImg,
          };
          this.catalogo = [itemFormateado, ...this.catalogo.filter(x => x.id !== newItemLocal.id)];
        }
        this.loadCatalogo();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error en backend al crear evento:', err);
        this.loadCatalogo();
        this.cdr.detectChanges();
      }
    });
  }

  openEditCatalogModal(item: CatalogoEventoItem): void {
    this.guardandoCatalogo = false;
    this.editCatalogData = { ...item };
    this.selectedImageFile = null;
    this.imagePreviewUrl = item.imagen || null;
    this.showEditCatalogModal = true;
    this.cdr.detectChanges();
  }

  saveEditCatalogEvent(): void {
    if (!this.editCatalogData || !this.editCatalogData.nombre) return;

    this.guardandoCatalogo = true;
    this.cdr.detectChanges();

    if (this.selectedImageFile) {
      this.catalogoService.subirImagenEvento(this.selectedImageFile).subscribe({
        next: (uploadRes) => {
          this.submitUpdateCatalogEvent(uploadRes.imageUrl);
        },
        error: (err) => {
          console.error('Error al subir imagen:', err);
          this.submitUpdateCatalogEvent(this.editCatalogData?.imagenUrl || null);
        }
      });
    } else {
      this.submitUpdateCatalogEvent(this.editCatalogData.imagenUrl || null);
    }
  }

  private submitUpdateCatalogEvent(imagenUrl: string | null): void {
    if (!this.editCatalogData) return;

    const id = this.editCatalogData.id;
    const request = {
      nombre: this.editCatalogData.nombre,
      descripcion: this.editCatalogData.descripcion || null,
      precioBase: Number(this.editCatalogData.precioBase) || 0,
      duracionHoras: Math.max(1, Number(this.editCatalogData.duracionHoras) || 1),
      activo: this.editCatalogData.activo,
      imagenUrl: imagenUrl
    };

    // Actualización local inmediata
    const index = this.catalogo.findIndex((item) => item.id === id);
    if (index !== -1) {
      let img = this.imagePreviewUrl || this.getEventImage(request.nombre);
      if (imagenUrl) {
        img = imagenUrl.startsWith('http') ? imagenUrl : `${environment.apiUrl}${imagenUrl}`;
      } else if (this.catalogo[index].imagen) {
        img = this.catalogo[index].imagen!;
      }

      this.catalogo[index] = {
        ...this.catalogo[index],
        nombre: request.nombre,
        descripcion: request.descripcion,
        precioBase: request.precioBase,
        duracionHoras: request.duracionHoras,
        activo: request.activo,
        imagenUrl: imagenUrl ?? this.catalogo[index].imagenUrl,
        categoria: this.assignCategory(request.nombre),
        imagen: img,
      };
      this.catalogo = [...this.catalogo];
    }

    // Cierre de modal e interfaz desinterrumpida al instante
    this.guardandoCatalogo = false;
    this.showEditCatalogModal = false;
    this.selectedImageFile = null;
    this.imagePreviewUrl = null;
    this.cdr.detectChanges();

    this.catalogoService.actualizarCatalogoEvento(id, request).subscribe({
      next: () => {
        this.loadCatalogo();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error en backend al actualizar evento:', err);
        this.loadCatalogo();
        this.cdr.detectChanges();
      }
    });
  }

  onCatalogSaved(): void {
    this.showNewCatalogModal = false;
    this.showEditCatalogModal = false;
    this.loadCatalogo();
    this.cdr.detectChanges();
  }

  deleteCatalogEvent(id: number): void {
    if (!confirm('¿Estás seguro de eliminar este paquete/evento del catálogo?')) return;

    this.catalogo = this.catalogo.filter((item) => item.id !== id);
    this.cdr.detectChanges();

    this.catalogoService.eliminarCatalogoEvento(id).subscribe({
      next: () => {
        this.toastService.showError('¡Evento eliminado con éxito!');
        this.loadCatalogo();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error en backend al eliminar evento:', err);
        this.toastService.showError('No se pudo eliminar el evento del catálogo.');
        this.loadCatalogo();
        this.cdr.detectChanges();
      }
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

  get filteredClientes(): ClienteSummary[] {
    return this.clientes.filter((cl) => {
      const full = `${cl.nombre} ${cl.apellido} ${cl.email} ${cl.ultimoEventoContratado}`.toLowerCase();
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

  get categoriasFiltroProveedores(): string[] {
    const list: string[] = [];
    if (this.categoriasProveedores && this.categoriasProveedores.length > 0) {
      for (const cat of this.categoriasProveedores) {
        if (cat.nombre && !list.includes(cat.nombre)) {
          list.push(cat.nombre);
        }
      }
    }
    if (this.proveedores && this.proveedores.length > 0) {
      for (const prov of this.proveedores) {
        if (prov.categoriaNombre && !list.includes(prov.categoriaNombre)) {
          list.push(prov.categoriaNombre);
        }
      }
    }
    return list;
  }

  get filteredProveedores(): Proveedor[] {
    return this.proveedores.filter((pv) => {
      const matchesSearch =
        pv.nombre.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (pv.contacto && pv.contacto.toLowerCase().includes(this.searchQuery.toLowerCase())) ||
        (pv.categoriaNombre && pv.categoriaNombre.toLowerCase().includes(this.searchQuery.toLowerCase()));
      const matchesCat =
        this.proveedoresCategoryFilter === 'Todos' ||
        (pv.categoriaNombre && pv.categoriaNombre.toLowerCase() === this.proveedoresCategoryFilter.toLowerCase()) ||
        pv.categoriaId?.toString() === this.proveedoresCategoryFilter;
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
    if (!this.newClientData.nombre || !this.newClientData.apellido || !this.newClientData.email) {
      alert('Por favor completa el nombre, apellido y correo electrónico.');
      return;
    }
    if (this.newClientData.password && this.newClientData.password !== this.newClientData.confirmPassword) {
      alert('Las contraseñas no coinciden.');
      return;
    }
    this.clientesService.crearCliente({
      nombre: this.newClientData.nombre || '',
      apellido: this.newClientData.apellido || '',
      email: this.newClientData.email || '',
      telefono: this.newClientData.telefono || '',
      password: this.newClientData.password || '',
    }).subscribe({
      next: (nuevoCliente) => {
        this.clientes.unshift(nuevoCliente);
        this.showNewClientModal = false;
        this.newClientData = { nombre: '', apellido: '', email: '', telefono: '', password: '', confirmPassword: '', estatus: 'Activo' };
      },
      error: (err) => {
        console.error('Error al crear cliente en backend:', err);
        alert(err?.error?.error || 'Error al registrar el cliente.');
      },
    });
  }

  saveNewSupplier(): void {
    if (!this.newSupplierData.nombre || !this.newSupplierData.contacto) {
      alert('Por favor completa el nombre de la empresa y la persona de contacto.');
      return;
    }
    this.proveedorService.crearProveedor({
      categoriaId: Number(this.newSupplierData.categoriaId) || 1,
      nombre: this.newSupplierData.nombre,
      contacto: this.newSupplierData.contacto,
      email: this.newSupplierData.email,
      telefono: this.newSupplierData.telefono,
      direccion: this.newSupplierData.direccion,
      sitioWeb: this.newSupplierData.sitioWeb,
      rfc: this.newSupplierData.rfc,
      rating: Number(this.newSupplierData.rating) || 5,
      precioPromedio: this.newSupplierData.precioPromedio || '$$$',
      activo: true,
    }).subscribe({
      next: () => {
        this.toastService.showSuccess('Proveedor guardado correctamente en la base de datos');
        this.showNewSupplierModal = false;
        this.newSupplierData = {
          nombre: '',
          contacto: '',
          categoriaId: 1,
          telefono: '',
          email: '',
          direccion: '',
          sitioWeb: '',
          rfc: '',
          rating: 5,
          precioPromedio: '$$$',
        };
        this.loadProveedores();
      },
      error: (err) => {
        console.error('Error al guardar proveedor:', err);
        this.toastService.showError('Error al guardar el proveedor.');
      },
    });
  }

  abrirVerServicios(proveedor: Proveedor): void {
    if (proveedor && !proveedor.servicios) {
      proveedor.servicios = [];
    }
    this.proveedorVerServicios = proveedor;
    this.cdr.detectChanges();
  }

  cerrarVerServicios(): void {
    this.proveedorVerServicios = null;
    this.cdr.detectChanges();
  }

  recargarServiciosProveedor(): void {
    if (!this.proveedorVerServicios) return;
    const id = this.proveedorVerServicios.id;
    this.proveedorService.getProveedorById(id).subscribe({
      next: (prov) => {
        if (prov) {
          if (!prov.servicios) prov.servicios = [];
          this.proveedorVerServicios = prov;
          const idx = this.proveedores.findIndex(p => p.id === prov.id);
          if (idx !== -1) this.proveedores[idx] = prov;
          this.cdr.detectChanges();
        }
      }
    });
  }

  abrirNuevoProveedorForm(): void {
    this.proveedorEditando = null;
    this.mostrandoFormModal = true;
    this.cdr.detectChanges();
  }

  abrirEditarProveedor(proveedor: Proveedor): void {
    this.proveedorEditando = proveedor;
    this.mostrandoFormModal = true;
    this.cdr.detectChanges();
  }

  cerrarFormModal(): void {
    this.mostrandoFormModal = false;
    this.proveedorEditando = null;
    this.cdr.detectChanges();
  }

  onProveedorGuardado(): void {
    this.cerrarFormModal();
    this.loadProveedores();
  }

  eliminarProveedor(proveedor: Proveedor): void {
    if (!confirm(`¿Estás seguro de eliminar el proveedor "${proveedor.nombre}"?`)) return;

    this.proveedorService.eliminarProveedor(proveedor.id).subscribe({
      next: () => {
        this.toastService.showSuccess('Proveedor eliminado correctamente');
        this.loadProveedores();
      },
      error: (err) => {
        console.error('Error al eliminar proveedor:', err);
        this.toastService.showError('No se pudo eliminar el proveedor.');
      }
    });
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

  verDesgloseCotizacion(cotizacionId: number): void {
    const found = this.cotizaciones.find((c) => c.id === cotizacionId);
    if (found) {
      this.selectedCotizacionDetail = found;
      this.selectedCotizacionFolio = found.folio;
      this.showCotizacionDetailModal = true;
    } else {
      this.cotizacionesService.getCotizacionById(cotizacionId).subscribe({
        next: (cot) => {
          this.selectedCotizacionDetail = cot;
          this.selectedCotizacionFolio = cot.folio;
          this.showCotizacionDetailModal = true;
        },
        error: (err) => {
          console.error('Error al cargar desglose de la cotización:', err);
        },
      });
    }
  }

  verDesgloseCotizacionPorFolio(folio: string): void {
    if (!folio) return;
    this.showFichaClienteModal = false;
    this.selectedCotizacionFolio = folio;
    const found = this.cotizaciones.find((c) => c.folio.toLowerCase() === folio.toLowerCase());
    this.selectedCotizacionDetail = found || null;
    this.showCotizacionDetailModal = true;
  }

  cerrarDesgloseCotizacion(): void {
    this.showCotizacionDetailModal = false;
    this.selectedCotizacionDetail = null;
    this.selectedCotizacionFolio = null;
  }

  logout(): void {
    localStorage.removeItem('session');
    this.router.navigate(['/login']);
  }
}
