-- Migration: Add Idempotent Key and Status to Transactions Table
-- Date: 2026-04-15
-- Description: Adds idempotent key, status, and processed_at columns to prevent duplicate transactions

-- Add new columns
ALTER TABLE transactions 
ADD COLUMN idempotent_key VARCHAR(255),
ADD COLUMN status INTEGER DEFAULT 0,
ADD COLUMN processed_at TIMESTAMP;

-- Update existing records to have a status of Completed (1) and generate idempotent keys
UPDATE transactions 
SET status = 1, 
    processed_at = created_at,
    idempotent_key = ENCODE(SHA256(CONCAT(order_id::text, ':', reference)::bytea), 'base64')
WHERE idempotent_key IS NULL;

-- Make idempotent_key and status NOT NULL after updating existing records
ALTER TABLE transactions 
ALTER COLUMN idempotent_key SET NOT NULL,
ALTER COLUMN status SET NOT NULL;

-- Create unique index on idempotent_key
CREATE UNIQUE INDEX CONCURRENTLY ix_transactions_idempotent_key 
ON transactions (idempotent_key);

-- Create unique index on reference (if not exists)
CREATE UNIQUE INDEX CONCURRENTLY ix_transactions_reference 
ON transactions (reference);

-- Create composite index for performance
CREATE INDEX CONCURRENTLY ix_transactions_order_id_status 
ON transactions (order_id, status);

-- Create index for querying by status
CREATE INDEX CONCURRENTLY ix_transactions_status 
ON transactions (status);

-- Add check constraints
ALTER TABLE transactions 
ADD CONSTRAINT ck_transactions_amount_positive CHECK (amount > 0),
ADD CONSTRAINT ck_transactions_status_valid CHECK (status IN (0, 1, 2, 3));

-- Add comments for documentation
COMMENT ON COLUMN transactions.idempotent_key IS 'SHA256 hash of order_id:reference for preventing duplicate processing';
COMMENT ON COLUMN transactions.status IS '0=Pending, 1=Completed, 2=Failed, 3=Cancelled';
COMMENT ON COLUMN transactions.processed_at IS 'Timestamp when transaction processing was completed';

-- Create function to automatically generate idempotent key if not provided
CREATE OR REPLACE FUNCTION generate_idempotent_key()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.idempotent_key IS NULL OR NEW.idempotent_key = '' THEN
        NEW.idempotent_key := ENCODE(SHA256(CONCAT(NEW.order_id::text, ':', NEW.reference)::bytea), 'base64');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to auto-generate idempotent key
CREATE TRIGGER tr_transactions_generate_idempotent_key
    BEFORE INSERT ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION generate_idempotent_key();