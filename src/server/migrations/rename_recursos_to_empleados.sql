-- ============================================================
-- Migración: renombrar tabla 'recursos' → 'empleados'
-- y actualizar la columna referenciada en la tabla 'usuarios'
-- ============================================================
-- Ejecutar en la base de datos PostgreSQL de Supabase
-- ============================================================

-- 1. Renombrar la tabla principal
ALTER TABLE IF EXISTS recursos RENAME TO empleados;

-- 2. Renombrar la columna de referencia en la tabla usuarios
--    (de recurso_id → empleado_id)
ALTER TABLE IF EXISTS usuarios
    RENAME COLUMN recurso_id TO empleado_id;

-- 3. (Opcional) Si existe una foreign key constraint que apunta a 'recursos',
--    renombrarla para mantener consistencia.
--    Ajustar 'usuarios_recurso_id_fkey' al nombre real de la constraint si difiere.
-- ALTER TABLE IF EXISTS usuarios
--     RENAME CONSTRAINT usuarios_recurso_id_fkey TO usuarios_empleado_id_fkey;

-- ============================================================
-- Verificación rápida
-- ============================================================
-- SELECT column_name, data_type
-- FROM information_schema.columns
-- WHERE table_name = 'empleados';

-- SELECT column_name, data_type
-- FROM information_schema.columns
-- WHERE table_name = 'usuarios'
-- AND column_name LIKE '%empleado%';
