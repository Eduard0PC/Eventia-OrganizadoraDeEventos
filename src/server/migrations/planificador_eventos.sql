-- =============================================================================
-- MIGRACIÓN: Módulo Planificador de Eventos (Proveedores, Locaciones, Margen y Checklist)
-- =============================================================================
-- 1. Agrega columna capacidad_maxima a proveedores (para filtrar Locaciones/Salones según el aforo del evento)
-- 2. Crea tabla evento_proveedores para asignar y bloquear proveedores por evento/fecha y registrar costos acordados
-- 3. Crea tabla evento_tareas para el checklist libre por evento
-- 4. Semilla inicial de Locaciones con distintas capacidades si no existen
-- =============================================================================

BEGIN;

-- 1. Capacidad máxima en proveedores (usado en categoría 'Locación') ------------
ALTER TABLE public.proveedores
  ADD COLUMN IF NOT EXISTS capacidad_maxima INTEGER NULL;

COMMENT ON COLUMN public.proveedores.capacidad_maxima IS
  'Capacidad máxima de personas (aforo) para proveedores de tipo Locación / Salón.';

-- 2. Tabla de asignaciones de proveedores por evento contratado -----------------
CREATE TABLE IF NOT EXISTS public.evento_proveedores (
    id SERIAL PRIMARY KEY,
    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
    proveedor_id INTEGER NOT NULL REFERENCES public.proveedores(id) ON DELETE RESTRICT,
    catalogo_servicio_id INTEGER NULL REFERENCES public.catalogo_servicios(id) ON DELETE SET NULL,
    clave_requerimiento VARCHAR(100) NOT NULL,
    nombre_requerimiento VARCHAR(200) NOT NULL,
    categoria_proveedor VARCHAR(100) NOT NULL,
    es_locacion BOOLEAN NOT NULL DEFAULT FALSE,
    costo_acordado NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    notas TEXT NULL,
    confirmado BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_evento_requerimiento UNIQUE (evento_contratado_id, clave_requerimiento),
    CONSTRAINT chk_costo_acordado_no_negativo CHECK (costo_acordado >= 0)
);

CREATE INDEX IF NOT EXISTS idx_evento_proveedores_evento ON public.evento_proveedores(evento_contratado_id);
CREATE INDEX IF NOT EXISTS idx_evento_proveedores_proveedor ON public.evento_proveedores(proveedor_id);

-- 3. Tabla de tareas libres del checklist por evento contratado -----------------
CREATE TABLE IF NOT EXISTS public.evento_tareas (
    id SERIAL PRIMARY KEY,
    evento_contratado_id INTEGER NOT NULL REFERENCES public.eventos_contratados(id) ON DELETE CASCADE,
    titulo VARCHAR(250) NOT NULL,
    completada BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_evento_tareas_evento ON public.evento_tareas(evento_contratado_id);

-- 4. Asegurar categoría 'Locación' y actualizar capacidad_maxima en locaciones --
INSERT INTO public.categorias_proveedores (nombre, descripcion, activo, created_at, updated_at)
SELECT 'Locación', 'Salones, jardines, haciendas y espacios para eventos', true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM public.categorias_proveedores WHERE LOWER(nombre) IN ('locación', 'locacion')
);

UPDATE public.proveedores p
SET capacidad_maxima = 250
FROM public.categorias_proveedores c
WHERE p.categoria_id = c.id
  AND LOWER(c.nombre) IN ('locación', 'locacion')
  AND p.capacidad_maxima IS NULL;

-- Insertar locaciones de ejemplo con distintas capacidades si aún no hay suficientes
INSERT INTO public.proveedores (categoria_id, nombre, contacto, email, telefono, direccion, rating, precio_promedio, capacidad_maxima, activo, created_at, updated_at)
SELECT c.id, v.nombre, v.contacto, v.email, v.telefono, v.direccion, v.rating, v.precio_promedio, v.capacidad_maxima, true, NOW(), NOW()
FROM public.categorias_proveedores c
CROSS JOIN (VALUES
    ('Jardín Botánico Los Olivos (Boutique)', 'Arq. Sofía Méndez', 'reservas@jardinlosolivos.mx', '81 2345 1101', 'Carretera Nacional Km 25', 5, '$$', 100),
    ('Hacienda San Gabriel de las Palmas', 'Lic. Roberto Villarreal', 'eventos@haciendasangabriel.mx', '81 8890 2202', 'Valle Alto, Monterrey', 5, '$$$', 250),
    ('Gran Salón Imperial & Centro de Convenciones', 'Lic. Elena Garza', 'contacto@gransalonimperial.mx', '81 9901 3303', 'San Pedro Garza García', 5, '$$$$', 600)
) AS v(nombre, contacto, email, telefono, direccion, rating, precio_promedio, capacidad_maxima)
WHERE LOWER(c.nombre) IN ('locación', 'locacion')
  AND NOT EXISTS (
      SELECT 1 FROM public.proveedores p WHERE LOWER(p.nombre) = LOWER(v.nombre)
  );

COMMIT;

