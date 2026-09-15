-- Script de migración para ejecutar en la base de datos PostgreSQL de Supabase
-- Agrega las columnas 'categoria' y 'capacidad_personas' a la tabla 'catalogo_eventos' (Cumple con 3FN)

ALTER TABLE public.catalogo_eventos
ADD COLUMN IF NOT EXISTS categoria VARCHAR(50) NOT NULL DEFAULT 'Sociales',
ADD COLUMN IF NOT EXISTS capacidad_personas INT NOT NULL DEFAULT 50,
ADD COLUMN IF NOT EXISTS imagen_url TEXT;

-- Opcional: Clasificación inicial para registros existentes según su nombre
UPDATE public.catalogo_eventos SET categoria = 'Bodas' WHERE LOWER(nombre) LIKE '%boda%';
UPDATE public.catalogo_eventos SET categoria = 'Corporativos' WHERE LOWER(nombre) LIKE '%corporativ%' OR LOWER(nombre) LIKE '%gala%';
UPDATE public.catalogo_eventos SET categoria = 'Graduaciones' WHERE LOWER(nombre) LIKE '%gradua%';
UPDATE public.catalogo_eventos SET categoria = 'Sociales' WHERE LOWER(nombre) LIKE '%xv%' OR LOWER(nombre) LIKE '%quince%';
