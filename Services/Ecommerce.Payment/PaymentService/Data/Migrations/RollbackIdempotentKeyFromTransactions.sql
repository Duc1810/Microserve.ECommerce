-- Rollback Migration: Remove Idempotent Key and Status from Transactions Table
-- Date: 2026-04-15
-- Description: Rollback script to remove idempotent key, status, and processed_at columns

-- Drop trigger and function
DROP TRIGGER IF EXISTS tr_transactions_generate_idempotent_key ON transactions;
DROP FUNCTION IF EXISTS generate_idempotent_key();

-- Drop indexes
DROP INDEX CONCURRENTLY IF EXISTS ix_transactions_idempotent_key;
DROP INDEX CONCURRENTLY IF EXISTS ix_transactions_reference;
DROP INDEX CONCURRENTLY IF EXISTS ix_transactions_order_id_status;
DROP INDEX CONCURRENTLY IF EXISTS ix_transactions_status;

-- Drop check constraints
ALTER TABLE transactions 
DROP CONSTRAINT IF EXISTS ck_transactions_amount_positive,
DROP CONSTRAINT IF EXISTS ck_transactions_status_valid;

-- Drop columns
ALTER TABLE transactions 
DROP COLUMN IF EXISTS idempotent_key,
DROP COLUMN IF EXISTS status,
DROP COLUMN IF EXISTS processed_at;