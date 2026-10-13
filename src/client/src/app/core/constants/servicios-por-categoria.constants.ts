export interface ServicioOpcional {
  id: string;
  nombre: string;
  descripcion?: string;
  precio: number;
  categoria: 'Bodas' | 'Corporativos' | 'Graduaciones' | 'Sociales' | string;
  categoriaProveedor?: string;
  seleccionado?: boolean;
}

/**
 * Catálogo reutilizable de servicios opcionales / adicionales clasificados por categoría de evento.
 * Se utilizan para renderizar dinámicamente checkboxes en la cotización del cliente
 * y para sugerir proveedores en la bandeja de empleados y el planificador.
 */
export const SERVICIOS_POR_CATEGORIA: Record<string, ServicioOpcional[]> = {
  Bodas: [
    {
      id: 'boda_locacion',
      nombre: 'Renta de Salón / Locación Premium',
      descripcion: 'Espacio exclusivo para ceremonia y recepción (jardín o salón de gala).',
      precio: 10000,
      categoria: 'Bodas',
      categoriaProveedor: 'Locación'
    },
    {
      id: 'boda_foto',
      nombre: 'Fotografía y Video HD Profesional',
      descripcion: 'Cobertura completa de ceremonia civil/religiosa y recepción con video highlight.',
      precio: 6000,
      categoria: 'Bodas',
      categoriaProveedor: 'Fotografía'
    },
    {
      id: 'boda_bar',
      nombre: 'Barra Libre & Coctelería de Autor',
      descripcion: 'Barra móvil con mixólogo profesional y coctelería ilimitada durante el evento.',
      precio: 4500,
      categoria: 'Bodas',
      categoriaProveedor: 'Bebidas'
    },
    {
      id: 'boda_postres',
      nombre: 'Mesa de Postres y Repostería Fina',
      descripcion: 'Variedad de bocadillos dulces, pastel nupcial temático y shot desserts.',
      precio: 3000,
      categoria: 'Bodas',
      categoriaProveedor: 'Repostería'
    },
    {
      id: 'boda_pista',
      nombre: 'Pista de Baile Iluminada Vintage / LED',
      descripcion: 'Pista modular con iluminación interactiva y diseño personalizado.',
      precio: 3500,
      categoria: 'Bodas',
      categoriaProveedor: 'Mobiliario'
    },
    {
      id: 'boda_planner',
      nombre: 'Coordinador / Planner en Sitio',
      descripcion: 'Supervisión logística minuto a minuto de todos los proveedores el día del evento.',
      precio: 4000,
      categoria: 'Bodas',
      categoriaProveedor: 'Staff'
    }
  ],

  Corporativos: [
    {
      id: 'corp_av',
      nombre: 'Pantallas LED y Microfonía Pro Extra',
      descripcion: 'Circuito cerrado, pantallas de alta definición y micrófonos inalámbricos de solapa/mano.',
      precio: 5000,
      categoria: 'Corporativos',
      categoriaProveedor: 'Audiovisual'
    },
    {
      id: 'corp_coffee',
      nombre: 'Coffee Break Continuo & Bocadillos',
      descripcion: 'Estación de café gourmet, té, jugos, fruta fresca y repostería durante las sesiones.',
      precio: 2500,
      categoria: 'Corporativos',
      categoriaProveedor: 'Catering'
    },
    {
      id: 'corp_edecanes',
      nombre: 'Personal de Registro y Recepción (2 pax)',
      descripcion: 'Staff capacitado para control de accesos, entrega de gafetes y atención a ponentes.',
      precio: 2000,
      categoria: 'Corporativos',
      categoriaProveedor: 'Staff'
    },
    {
      id: 'corp_branding',
      nombre: 'Escenografía, Backing de Prensa y Señalética',
      descripcion: 'Estructuras personalizadas con logotipos de la empresa y patrocinadores.',
      precio: 4000,
      categoria: 'Corporativos',
      categoriaProveedor: 'Publicidad'
    },
    {
      id: 'corp_streaming',
      nombre: 'Transmisión en Vivo HD (Streaming)',
      descripcion: 'Transmisión multicanal para asistentes virtuales con soporte técnico en tiempo real.',
      precio: 3500,
      categoria: 'Corporativos',
      categoriaProveedor: 'Audiovisual'
    }
  ],

  Graduaciones: [
    {
      id: 'grad_seguridad',
      nombre: 'Seguridad Privada & Control de Acceso',
      descripcion: 'Filtro de seguridad con brazaletes de control y vigilancia en accesos.',
      precio: 3000,
      categoria: 'Graduaciones',
      categoriaProveedor: 'Seguridad'
    },
    {
      id: 'grad_360',
      nombre: 'Plataforma de Video 360 Spin',
      descripcion: 'Cabina giratoria de video en cámara lenta con descarga instantánea para graduados.',
      precio: 3500,
      categoria: 'Graduaciones',
      categoriaProveedor: 'Entretenimiento'
    },
    {
      id: 'grad_mariachi',
      nombre: 'Mariachi en Vivo para Brindis de Cierre',
      descripcion: 'Presentación de mariachi tradicional para el vals y brindis final (1 hora).',
      precio: 4000,
      categoria: 'Graduaciones',
      categoriaProveedor: 'Música'
    },
    {
      id: 'grad_photo',
      nombre: 'Set Fotográfico con Cuadro del Recuerdo',
      descripcion: 'Escenografía con toga/birrete para fotos individuales y familiares con impresión.',
      precio: 2800,
      categoria: 'Graduaciones',
      categoriaProveedor: 'Fotografía'
    },
    {
      id: 'grad_pirotecnia',
      nombre: 'Pirotecnia Fría para Entrada / Brindis',
      descripcion: 'Detonadores de chispa fría para interiores sin humo ni riesgo de quemaduras.',
      precio: 2500,
      categoria: 'Graduaciones',
      categoriaProveedor: 'Efectos Especiales'
    }
  ],

  Sociales: [
    {
      id: 'soc_letras',
      nombre: 'Letras Gigantes Luminosas',
      descripcion: 'Iniciales o números iluminados (ej. XV, edad o nombre) para sesión de fotos.',
      precio: 1800,
      categoria: 'Sociales',
      categoriaProveedor: 'Decoración'
    },
    {
      id: 'soc_dulces',
      nombre: 'Mesa de Snacks y Dulces Temática',
      descripcion: 'Barra de golosinas, frituras preparadas y mesa dulce decorada según la temática.',
      precio: 2200,
      categoria: 'Sociales',
      categoriaProveedor: 'Snacks'
    },
    {
      id: 'soc_cabina',
      nombre: 'Cabina Fotográfica con Fotos Ilimitadas',
      descripcion: 'Fotos instantáneas con accesorios divertidos (props) y tiras personalizadas.',
      precio: 2800,
      categoria: 'Sociales',
      categoriaProveedor: 'Fotografía'
    },
    {
      id: 'soc_show',
      nombre: 'Show o Animador en Vivo',
      descripcion: 'Animación profesional, show infantil o interactivo según el festejo.',
      precio: 3000,
      categoria: 'Sociales',
      categoriaProveedor: 'Entretenimiento'
    },
    {
      id: 'soc_inflable',
      nombre: 'Inflable Temático o Juegos Recreativos',
      descripcion: 'Juego inflable grande con operador para fiestas infantiles o juveniles.',
      precio: 2000,
      categoria: 'Sociales',
      categoriaProveedor: 'Juegos'
    }
  ]
};

/**
 * Obtiene una copia limpia y desmarcada de los servicios adicionales correspondientes
 * a una categoría de evento dada. Si la categoría no existe o es nula, utiliza 'Sociales' por defecto.
 */
export function obtenerServiciosPorCategoria(categoria?: string | null): ServicioOpcional[] {
  if (!categoria) {
    return clonarServicios(SERVICIOS_POR_CATEGORIA['Sociales'] || []);
  }

  const catNormalizada = categoria.trim().toLowerCase();

  // 1. Coincidencia exacta de llave
  for (const [clave, servicios] of Object.entries(SERVICIOS_POR_CATEGORIA)) {
    if (clave.toLowerCase() === catNormalizada) {
      return clonarServicios(servicios);
    }
  }

  // 2. Coincidencia por palabras clave para nombres compuestos o sinónimos
  if (catNormalizada.includes('boda') || catNormalizada.includes('matrimonio') || catNormalizada.includes('nupcial')) {
    return clonarServicios(SERVICIOS_POR_CATEGORIA['Bodas']);
  }
  if (catNormalizada.includes('corp') || catNormalizada.includes('confer') || catNormalizada.includes('empresa') || catNormalizada.includes('gala')) {
    return clonarServicios(SERVICIOS_POR_CATEGORIA['Corporativos']);
  }
  if (catNormalizada.includes('gradua')) {
    return clonarServicios(SERVICIOS_POR_CATEGORIA['Graduaciones']);
  }

  // 3. Fallback por defecto a Sociales (XV Años, Cumpleaños, etc.)
  return clonarServicios(SERVICIOS_POR_CATEGORIA['Sociales'] || []);
}

function clonarServicios(servicios: ServicioOpcional[]): ServicioOpcional[] {
  return servicios.map(s => ({
    ...s,
    seleccionado: false
  }));
}
