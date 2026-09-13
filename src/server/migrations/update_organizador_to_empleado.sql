-- ============================================================
-- Migración: Unificar rol de organizadores y empleados
-- ============================================================
-- Ejecutar este script en el editor SQL de tu panel de Supabase.
-- Garantiza que cualquier rol previo 'organizador' se convierta a 'empleado'
-- en el tipo ENUM y en la restricción chk_usr_vinculo.
-- ============================================================

-- 1. Renombrar el valor del ENUM rol_usuario de 'organizador' a 'empleado'
ALTER TYPE rol_usuario RENAME VALUE 'organizador' TO 'empleado';

-- 2. Actualizar la restricción chk_usr_vinculo en la tabla usuarios
ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS chk_usr_vinculo;

ALTER TABLE usuarios ADD CONSTRAINT chk_usr_vinculo 
    CHECK (((rol = 'cliente'::rol_usuario) AND (cliente_id IS NOT NULL) AND (empleado_id IS NULL)) 
        OR ((rol = 'empleado'::rol_usuario) AND (empleado_id IS NOT NULL) AND (cliente_id IS NULL)) 
        OR ((rol = 'admin'::rol_usuario) AND (cliente_id IS NULL) AND (empleado_id IS NULL)));

-- 3. Consulta de verificación
SELECT enumlabel FROM pg_enum JOIN pg_type ON pg_enum.enumtypid = pg_type.oid WHERE pg_type.typname = 'rol_usuario';
SELECT id, email, rol, empleado_id, cliente_id FROM usuarios ORDER BY id DESC;
