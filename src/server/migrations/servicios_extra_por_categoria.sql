-- =============================================================================
-- MIGRACIÓN: Servicios extra por categoría de evento
-- -----------------------------------------------------------------------------
-- 1. Vacía cotizaciones / cotizacion_items / catalogo_servicios (datos inconsistentes)
-- 2. catalogo_servicios: + categoria, + categoria_proveedor_id, + visible_cliente
-- 3. categorias_proveedores: + categorías faltantes
-- 4. CHECK de categorías válidas en catalogo_eventos y catalogo_servicios
-- 5. Índice único (cotizacion_id, catalogo_servicio_id) para tipo 'servicio'
-- 6. Renombra recurso_id -> empleado_id (cotizaciones, asignacion_recursos)
-- 7. proveedor_servicios: + catalogo_servicio_id
-- 8. Seed de servicios extra por categoría + servicios internos
-- =============================================================================

BEGIN;

-- 1. Limpieza ----------------------------------------------------------------
-- Se usa DELETE (no TRUNCATE CASCADE) para no vaciar contratos y sus tablas hijas.
DELETE FROM cotizacion_items;
DELETE FROM cotizaciones;
DELETE FROM catalogo_servicios;
ALTER SEQUENCE cotizacion_items_id_seq RESTART WITH 1;
ALTER SEQUENCE cotizaciones_id_seq RESTART WITH 1;
ALTER SEQUENCE catalogo_servicios_id_seq RESTART WITH 1;

-- 2. catalogo_servicios --------------------------------------------------------
ALTER TABLE catalogo_servicios
  ADD COLUMN categoria varchar(50) NULL,
  ADD COLUMN categoria_proveedor_id integer NULL,
  ADD COLUMN visible_cliente boolean NOT NULL DEFAULT true;

ALTER TABLE catalogo_servicios
  ADD CONSTRAINT fk_srv_cat_proveedor FOREIGN KEY (categoria_proveedor_id)
    REFERENCES categorias_proveedores(id) ON DELETE SET NULL;

CREATE INDEX idx_srv_categoria ON catalogo_servicios (categoria) WHERE activo AND visible_cliente;

COMMENT ON COLUMN catalogo_servicios.categoria IS 'Categoría de evento a la que aplica (Bodas, Sociales, Corporativos, Graduaciones). NULL = aplica a todas.';
COMMENT ON COLUMN catalogo_servicios.categoria_proveedor_id IS 'Tipo de proveedor que cubre el servicio (para sugerencias en el planificador).';
COMMENT ON COLUMN catalogo_servicios.visible_cliente IS 'false = servicio interno (invitado/hora adicional), no se muestra como opción al cliente.';

-- 3. categorias_proveedores faltantes ------------------------------------------
INSERT INTO categorias_proveedores (nombre, descripcion, activo, created_at, updated_at)
SELECT v.nombre, v.descripcion, true, now(), now()
FROM (VALUES
  ('Locación',           'Salones, jardines y espacios para eventos'),
  ('Bebidas',            'Barra libre, mixología y bebidas'),
  ('Repostería',         'Pasteles, postres y mesas dulces'),
  ('Mobiliario',         'Renta de mobiliario, pistas y estructuras'),
  ('Staff',              'Personal de apoyo, coordinación y recepción'),
  ('Publicidad',         'Branding, escenografía y señalética'),
  ('Música',             'Músicos y grupos en vivo'),
  ('Efectos Especiales', 'Pirotecnia fría, humo y efectos'),
  ('Snacks',             'Mesas de botanas y golosinas'),
  ('Juegos',             'Inflables y juegos recreativos')
) AS v(nombre, descripcion)
WHERE NOT EXISTS (
  SELECT 1 FROM categorias_proveedores c WHERE lower(c.nombre) = lower(v.nombre)
);

-- 4. Categorías válidas ----------------------------------------------------------
ALTER TABLE catalogo_eventos
  ADD CONSTRAINT chk_evt_categoria
  CHECK (categoria IN ('Bodas', 'Sociales', 'Corporativos', 'Graduaciones'));

ALTER TABLE catalogo_servicios
  ADD CONSTRAINT chk_srv_categoria
  CHECK (categoria IS NULL OR categoria IN ('Bodas', 'Sociales', 'Corporativos', 'Graduaciones'));

-- 5. Un servicio no se repite dentro de la misma cotización -----------------------
CREATE UNIQUE INDEX ux_item_cot_servicio
  ON cotizacion_items (cotizacion_id, catalogo_servicio_id)
  WHERE tipo = 'servicio';

-- 6. recurso_id -> empleado_id ---------------------------------------------------
ALTER TABLE cotizaciones RENAME COLUMN recurso_id TO empleado_id;
ALTER TABLE cotizaciones RENAME CONSTRAINT fk_cot_recurso TO fk_cot_empleado;
ALTER INDEX idx_cotizaciones_recurso RENAME TO idx_cotizaciones_empleado;

ALTER TABLE asignacion_recursos RENAME COLUMN recurso_id TO empleado_id;
ALTER TABLE asignacion_recursos RENAME CONSTRAINT fk_asig_recurso TO fk_asig_empleado;
ALTER INDEX idx_asig_recurso RENAME TO idx_asig_empleado;

ALTER INDEX usuarios_recurso_id_key RENAME TO usuarios_empleado_id_key;

-- 7. proveedor_servicios -> catalogo_servicios -----------------------------------
ALTER TABLE proveedor_servicios
  ADD COLUMN catalogo_servicio_id integer NULL,
  ADD CONSTRAINT fk_ps_catalogo_servicio FOREIGN KEY (catalogo_servicio_id)
    REFERENCES catalogo_servicios(id) ON DELETE SET NULL;

CREATE INDEX idx_ps_catalogo_servicio ON proveedor_servicios (catalogo_servicio_id);

-- 8. Seed de servicios -----------------------------------------------------------
INSERT INTO catalogo_servicios
  (nombre, descripcion, precio_base, unidad, activo, categoria, categoria_proveedor_id, visible_cliente, created_at, updated_at)
SELECT v.nombre, v.descripcion, v.precio, v.unidad, true, v.categoria,
       (SELECT id FROM categorias_proveedores c WHERE lower(c.nombre) = lower(v.cat_prov) LIMIT 1),
       v.visible, now(), now()
FROM (VALUES
  -- Servicios internos (no visibles para el cliente)
  ('Invitado adicional', 'Cargo por persona adicional sobre la capacidad base del paquete.', 150.00, 'persona', NULL, NULL, false),
  ('Hora adicional',     'Cargo por hora adicional sobre la duración base del paquete.',    1000.00, 'hora',   NULL, NULL, false),

  -- Bodas
  ('Renta de Salón / Locación Premium', 'Espacio exclusivo para ceremonia y recepción (jardín o salón de gala).', 10000.00, 'evento', 'Bodas', 'Locación', true),
  ('Fotografía y Video HD Profesional', 'Cobertura completa de ceremonia civil/religiosa y recepción con video highlight.', 6000.00, 'evento', 'Bodas', 'Fotografía', true),
  ('Barra Libre & Coctelería de Autor', 'Barra móvil con mixólogo profesional y coctelería ilimitada durante el evento.', 4500.00, 'evento', 'Bodas', 'Bebidas', true),
  ('Mesa de Postres y Repostería Fina', 'Variedad de bocadillos dulces, pastel nupcial temático y shot desserts.', 3000.00, 'evento', 'Bodas', 'Repostería', true),
  ('Pista de Baile Iluminada Vintage / LED', 'Pista modular con iluminación interactiva y diseño personalizado.', 3500.00, 'evento', 'Bodas', 'Mobiliario', true),
  ('Coordinador / Planner en Sitio', 'Supervisión logística minuto a minuto de todos los proveedores el día del evento.', 4000.00, 'evento', 'Bodas', 'Staff', true),

  -- Corporativos
  ('Pantallas LED y Microfonía Pro Extra', 'Circuito cerrado, pantallas de alta definición y micrófonos inalámbricos de solapa/mano.', 5000.00, 'evento', 'Corporativos', 'Audiovisual', true),
  ('Coffee Break Continuo & Bocadillos', 'Estación de café gourmet, té, jugos, fruta fresca y repostería durante las sesiones.', 2500.00, 'evento', 'Corporativos', 'Catering', true),
  ('Personal de Registro y Recepción (2 pax)', 'Staff capacitado para control de accesos, entrega de gafetes y atención a ponentes.', 2000.00, 'evento', 'Corporativos', 'Staff', true),
  ('Escenografía, Backing de Prensa y Señalética', 'Estructuras personalizadas con logotipos de la empresa y patrocinadores.', 4000.00, 'evento', 'Corporativos', 'Publicidad', true),
  ('Transmisión en Vivo HD (Streaming)', 'Transmisión multicanal para asistentes virtuales con soporte técnico en tiempo real.', 3500.00, 'evento', 'Corporativos', 'Audiovisual', true),

  -- Graduaciones
  ('Seguridad Privada & Control de Acceso', 'Filtro de seguridad con brazaletes de control y vigilancia en accesos.', 3000.00, 'evento', 'Graduaciones', 'Seguridad', true),
  ('Plataforma de Video 360 Spin', 'Cabina giratoria de video en cámara lenta con descarga instantánea para graduados.', 3500.00, 'evento', 'Graduaciones', 'Entretenimiento', true),
  ('Mariachi en Vivo para Brindis de Cierre', 'Presentación de mariachi tradicional para el vals y brindis final (1 hora).', 4000.00, 'evento', 'Graduaciones', 'Música', true),
  ('Set Fotográfico con Cuadro del Recuerdo', 'Escenografía con toga/birrete para fotos individuales y familiares con impresión.', 2800.00, 'evento', 'Graduaciones', 'Fotografía', true),
  ('Pirotecnia Fría para Entrada / Brindis', 'Detonadores de chispa fría para interiores sin humo ni riesgo de quemaduras.', 2500.00, 'evento', 'Graduaciones', 'Efectos Especiales', true),

  -- Sociales
  ('Letras Gigantes Luminosas', 'Iniciales o números iluminados (ej. XV, edad o nombre) para sesión de fotos.', 1800.00, 'evento', 'Sociales', 'Decoración', true),
  ('Mesa de Snacks y Dulces Temática', 'Barra de golosinas, frituras preparadas y mesa dulce decorada según la temática.', 2200.00, 'evento', 'Sociales', 'Snacks', true),
  ('Cabina Fotográfica con Fotos Ilimitadas', 'Fotos instantáneas con accesorios divertidos (props) y tiras personalizadas.', 2800.00, 'evento', 'Sociales', 'Fotografía', true),
  ('Show o Animador en Vivo', 'Animación profesional, show infantil o interactivo según el festejo.', 3000.00, 'evento', 'Sociales', 'Entretenimiento', true),
  ('Inflable Temático o Juegos Recreativos', 'Juego inflable grande con operador para fiestas infantiles o juveniles.', 2000.00, 'evento', 'Sociales', 'Juegos', true)
) AS v(nombre, descripcion, precio, unidad, categoria, cat_prov, visible);

COMMIT;
