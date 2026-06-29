# Payment Service - Idempotent Transaction Processing

## Overview

The Payment Service has been redesigned to implement idempotent transaction processing to prevent duplicate transactions and ensure data consistency.

## Key Features

### 1. Idempotent Key Generation
- **Purpose**: Prevent duplicate transaction processing
- **Implementation**: SHA256 hash of `OrderId:Reference`
- **Uniqueness**: Database-level unique constraint ensures no duplicates

### 2. Transaction Status Management
- **Pending (0)**: Transaction created but not yet processed
- **Completed (1)**: Transaction successfully processed and event published
- **Failed (2)**: Transaction processing failed
- **Cancelled (3)**: Transaction was cancelled

### 3. Atomic Processing
- Transactions are saved in `Pending` state first
- Status updated to `Completed` only after successful event publishing
- Failed event publishing marks transaction as `Failed`

## Architecture Components

### Models
- **Transaction**: Enhanced with `IdempotentKey`, `Status`, and `ProcessedAt`
- **TransactionStatus**: Enum for transaction states
- **AccountDetails**: Record for bank account information

### Services
- **ITransactionService**: Core transaction processing logic
- **TransactionService**: Implementation with idempotent processing
- **PayOSService**: Updated to use TransactionService

### Controllers
- **WebhookController**: Handles PayOS webhook notifications
- **TransactionController**: Provides transaction query endpoints

## Database Schema

### New Columns
```sql
ALTER TABLE transactions 
ADD COLUMN idempotent_key VARCHAR(255) NOT NULL,
ADD COLUMN status INTEGER NOT NULL DEFAULT 0,
ADD COLUMN processed_at TIMESTAMP;
```

### Indexes
- `ix_transactions_idempotent_key` (UNIQUE)
- `ix_transactions_reference` (UNIQUE)
- `ix_transactions_order_id_status` (Composite)
- `ix_transactions_status`

### Constraints
- `ck_transactions_amount_positive`: Ensures amount > 0
- `ck_transactions_status_valid`: Validates status values (0-3)

## API Endpoints

### Webhook Processing
```
POST /api/webhook/payos
```
- Processes PayOS webhook notifications
- Implements idempotent processing
- Returns appropriate HTTP status codes

### Transaction Queries
```
GET /api/transaction/by-idempotent-key/{key}
GET /api/transaction/by-order/{orderId}
POST /api/transaction/generate-idempotent-key
```

## Idempotent Processing Flow

1. **Webhook Received**: PayOS sends webhook notification
2. **Key Generation**: Create idempotent key from OrderId + Reference
3. **Duplicate Check**: Query existing transaction by idempotent key
4. **Status Handling**:
   - If `Completed`: Return success (already processed)
   - If `Pending`: Return conflict (currently processing)
   - If `Failed/Cancelled`: Return error
5. **New Transaction**: Create transaction in `Pending` state
6. **Event Publishing**: Publish payment completed event
7. **Status Update**: Mark as `Completed` or `Failed` based on result

## Error Handling

### Duplicate Processing
- **Same Request**: Returns success if already completed
- **Concurrent Requests**: Returns conflict if currently processing
- **Failed Requests**: Returns error with failure reason

### Event Publishing Failures
- Transaction marked as `Failed`
- Detailed error logging
- Allows for retry mechanisms

## Configuration

### PayOS Settings
```json
{
  "PayOS": {
    "ClientId": "your-client-id",
    "ApiKey": "your-api-key", 
    "ChecksumKey": "your-checksum-key"
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-postgresql-connection-string"
  }
}
```

## Migration Instructions

### Apply Migration
```sql
-- Run the migration script
\i Services/Ecommerce.Payment/PaymentService/Data/Migrations/AddIdempotentKeyToTransactions.sql
```

### Rollback Migration
```sql
-- Run the rollback script if needed
\i Services/Ecommerce.Payment/PaymentService/Data/Migrations/RollbackIdempotentKeyFromTransactions.sql
```

## Testing

### Idempotent Key Generation
```csharp
var key1 = transactionService.GenerateIdempotentKey(orderId, reference);
var key2 = transactionService.GenerateIdempotentKey(orderId, reference);
Assert.Equal(key1, key2); // Should be identical
```

### Duplicate Prevention
```csharp
// First request - should succeed
var result1 = await transactionService.ProcessPaymentAsync(...);
Assert.True(result1.IsSuccess);

// Second request with same data - should return existing transaction
var result2 = await transactionService.ProcessPaymentAsync(...);
Assert.True(result2.IsSuccess);
Assert.Equal(result1.Data.Id, result2.Data.Id);
```

## Monitoring and Logging

### Key Metrics
- Transaction processing time
- Duplicate request rate
- Event publishing success rate
- Transaction status distribution

### Log Levels
- **Information**: Successful processing, duplicate detection
- **Warning**: Failed event publishing, invalid requests
- **Error**: Unexpected errors, database issues

## Security Considerations

### Idempotent Key Security
- Uses SHA256 for deterministic key generation
- Keys are not reversible to original data
- Unique constraint prevents collision attacks

### Webhook Verification
- PayOS webhook signature verification
- Request validation and sanitization
- Rate limiting recommended for production

## Performance Optimizations

### Database Indexes
- Unique indexes for fast duplicate detection
- Composite indexes for efficient queries
- Concurrent index creation to minimize downtime

### Caching Strategy
- Consider Redis caching for frequently accessed transactions
- Cache idempotent key lookups for recent transactions
- Implement cache invalidation on status updates