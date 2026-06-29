# Requirements Document: Ecommerce Dashboard API

## Introduction

`Ecommerce.Dashboard` is a standalone service in the microservice system, providing aggregated metrics for the ecommerce admin interface. The service operates on a **CQRS + Materialized View + EventStore** model: it listens to integration events from Order, Payment, and Production services via RabbitMQ/MassTransit to pre-aggregate data into dedicated read models. All dashboard APIs return results from pre-computed data — never querying real-time against other services.

The service adds a `DashboardEventStore` table (append-only) to store all received raw events, enabling a full rebuild of materialized views at any time without requiring Kafka.

---

## Glossary

- **Dashboard_Service**: The `Ecommerce.Dashboard` service — standalone service handling all dashboard logic
- **Event_Consumer**: Component that receives integration events from RabbitMQ/MassTransit
- **Aggregation_Handler**: Component that updates materialized views from event data (`HandleXxx` methods)
- **Projection_Handler**: Pure projection component used during rebuild (`ApplyXxx` methods), no side effects
- **Query_Handler**: Component that reads data from Dashboard DB and returns DTOs
- **Dashboard_Controller**: REST API controller exposing dashboard endpoints
- **EventStore**: The `DashboardEventStore` table — append-only, stores all raw events
- **DailyRevenueSummary**: Read model storing daily aggregated revenue
- **OrderStateRecord**: Read model storing per-order status (set-based, not counter-based)
- **TopProductSnapshot**: Read model storing best-selling product statistics
- **Rebuild_Handler**: Component handling the rebuild materialized views command from EventStore
- **JWT**: JSON Web Token — authentication mechanism
- **CQRS**: Command Query Responsibility Segregation — read/write separation pattern
- **Materialized_View**: Pre-aggregated data stored in Dashboard DB
- **SequenceNumber**: Auto-incrementing number (BIGSERIAL) assigned by DB, used for ordering during replay
- **AOV**: Average Order Value

---

## Requirements

### Requirement 1: Integration Event Consumption and Storage

**User Story:** As a dashboard system, I want to receive and store integration events from other services, so that I can pre-aggregate data without querying source services in real-time.

#### Acceptance Criteria

1. WHEN `Event_Consumer` receives `IPaymentCompletedEvent` from RabbitMQ, THE `Dashboard_Service` SHALL store the raw event in `EventStore` before performing any aggregation
2. WHEN `Event_Consumer` receives `IOrderSubmittedEvent` from RabbitMQ, THE `Dashboard_Service` SHALL store the raw event in `EventStore` before performing any aggregation
3. WHEN `Event_Consumer` receives `ICancelOrderCommand` from RabbitMQ, THE `Dashboard_Service` SHALL store the raw event in `EventStore` before performing any aggregation
4. THE `EventStore` SHALL store the following fields: `EventId` (PK), `EventType`, `Payload` (full JSON), `OccurredOn`, `ReceivedAt`, `SequenceNumber` (auto BIGSERIAL)
5. THE `EventStore` SHALL be append-only — never UPDATE or DELETE any record
6. WHEN `Event_Consumer` receives an event whose `EventId` already exists in `EventStore`, THE `Dashboard_Service` SHALL skip all processing and not update any materialized view
7. THE `Dashboard_Service` SHALL perform idempotency check via `INSERT INTO EventStore ON CONFLICT (EventId) DO NOTHING` and check `rows_affected` — if `rows_affected = 0` then exit immediately

---

### Requirement 2: Atomic Processing and Idempotency

**User Story:** As a distributed system, I want to ensure each event is processed exactly once, so that dashboard data is not corrupted by duplicate events or retries.

#### Acceptance Criteria

1. WHEN `Aggregation_Handler` processes an event, THE `Dashboard_Service` SHALL perform all operations (save to EventStore + update all materialized views) within a single database transaction
2. IF two `Event_Consumer` instances process the same event concurrently, THEN THE `Dashboard_Service` SHALL ensure only one instance successfully updates the materialized views — the other receives `rows_affected = 0` and exits
3. WHEN `Aggregation_Handler` processes `IPaymentCompletedEvent`, THE `Dashboard_Service` SHALL update `DailyRevenueSummary`, `OrderStateRecord`, and `TopProductSnapshot` within the same transaction
4. IF the database transaction fails, THEN THE `Dashboard_Service` SHALL rollback all changes and allow MassTransit to retry with exponential backoff policy
5. IF `IPaymentCompletedEvent` is missing the `Amount` or `Items` fields, THEN THE `Dashboard_Service` SHALL log a warning and skip the event — do not throw an exception to avoid infinite retries

---

### Requirement 3: DailyRevenueSummary Updates

**User Story:** As an admin, I want to view aggregated daily revenue, so that I can track business performance over time.

#### Acceptance Criteria

1. WHEN `Aggregation_Handler` processes `IPaymentCompletedEvent`, THE `Dashboard_Service` SHALL upsert `DailyRevenueSummary` for the corresponding day using `INSERT ON CONFLICT (Date) DO UPDATE`
2. THE `DailyRevenueSummary` SHALL have a unique index on the `Date` column — one record per day
3. WHEN `DailyRevenueSummary` is updated, THE `Dashboard_Service` SHALL accumulate `Amount` into `TotalRevenue` and increment `CompletedOrderCount` by 1
4. THE `Dashboard_Service` SHALL only update `DailyRevenueSummary` from `IPaymentCompletedEvent` — not from other events
5. THE `DailyRevenueSummary` SHALL ensure `TotalRevenue >= 0` at all times

---

### Requirement 4: OrderStateRecord Updates (Set-Based)

**User Story:** As an admin, I want to view the current distribution of order statuses, so that I can understand the order processing pipeline.

#### Acceptance Criteria

1. WHEN `Aggregation_Handler` receives an order status change event, THE `Dashboard_Service` SHALL upsert `OrderStateRecord` by `OrderId` using `INSERT ON CONFLICT (Id) DO UPDATE SET Status = newStatus`
2. THE `OrderStateRecord` SHALL store the current status of each order — each `OrderId` has exactly one row
3. THE `Dashboard_Service` SHALL only accept `Status` values ∈ { "Draft", "Pending", "Completed", "Cancelled" }
4. WHEN `Dashboard_Service` computes status distribution, THE `Query_Handler` SHALL execute `SELECT Status, COUNT(*) FROM OrderStateRecord GROUP BY Status`
5. THE `Dashboard_Service` SHALL update `OrderStateRecord` when receiving `IOrderSubmittedEvent` (Status = "Pending"), `IPaymentCompletedEvent` (Status = "Completed"), and `ICancelOrderCommand` (Status = "Cancelled")

---

### Requirement 5: TopProductSnapshot Updates

**User Story:** As an admin, I want to view top-selling products, so that I can make decisions about inventory and business strategy.

#### Acceptance Criteria

1. WHEN `Aggregation_Handler` processes `IPaymentCompletedEvent`, THE `Dashboard_Service` SHALL upsert `TopProductSnapshot` for each item in the order using `INSERT ON CONFLICT (ProductId) DO UPDATE`
2. WHEN `TopProductSnapshot` is updated, THE `Dashboard_Service` SHALL accumulate `Quantity` into `TotalQuantitySold` and accumulate `UnitPrice × Quantity` into `TotalRevenue`
3. THE `TopProductSnapshot` SHALL have a unique index on the `ProductId` column — one row per product
4. THE `Dashboard_Service` SHALL only update `TopProductSnapshot` from `IPaymentCompletedEvent` — not from other events
5. THE `TopProductSnapshot` SHALL ensure `TotalQuantitySold >= 0` and `TotalRevenue >= 0` at all times

---

### Requirement 6: API Endpoint — Revenue Summary

**User Story:** As an admin, I want to view total revenue, order count, and AOV for a time range, so that I can evaluate overall business performance.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL expose endpoint `GET /api/dashboard/revenue/summary?from=&to=`
2. WHEN `Query_Handler` receives `GetRevenueSummaryQuery`, THE `Dashboard_Service` SHALL return `RevenueSummaryResult` containing: `TotalRevenue`, `TotalCompletedOrders`, `AverageOrderValue`, `FromDate`, `ToDate`
3. WHEN `Query_Handler` computes `AverageOrderValue`, THE `Dashboard_Service` SHALL calculate `TotalRevenue / TotalCompletedOrders` — if `TotalCompletedOrders = 0` then `AverageOrderValue = 0`
4. WHEN the request is missing `from` or `to` parameters, THE `Dashboard_Controller` SHALL return HTTP 400 Bad Request with a clear error message
5. THE `Dashboard_Controller` SHALL require a valid JWT — return HTTP 401 if no token is present

---

### Requirement 7: API Endpoint — Order Status Distribution

**User Story:** As an admin, I want to view the current order status distribution, so that I can monitor the order processing pipeline.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL expose endpoint `GET /api/dashboard/orders/status`
2. WHEN `Query_Handler` receives `GetOrderStatusSummaryQuery`, THE `Dashboard_Service` SHALL return `OrderStatusSummaryResult` containing: `Draft`, `Pending`, `Completed`, `Cancelled`, `Total`
3. THE `Dashboard_Service` SHALL compute `Total = Draft + Pending + Completed + Cancelled`
4. THE `Dashboard_Controller` SHALL require a valid JWT — return HTTP 401 if no token is present

---

### Requirement 8: API Endpoint — Top Products

**User Story:** As an admin, I want to view the top N best-selling products, so that I can prioritize inventory management and marketing.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL expose endpoint `GET /api/dashboard/products/top?topN=10`
2. WHEN `Query_Handler` receives `GetTopProductsQuery(topN)`, THE `Dashboard_Service` SHALL return a list of at most `topN` products, sorted descending by `TotalQuantitySold`
3. WHEN the `topN` parameter is not provided, THE `Dashboard_Controller` SHALL use the default value `topN = 10`
4. WHEN the `topN` parameter is `<= 0` or `> 100`, THE `Dashboard_Controller` SHALL return HTTP 400 Bad Request
5. THE `Dashboard_Controller` SHALL require a valid JWT — return HTTP 401 if no token is present

---

### Requirement 9: API Endpoint — Revenue Time Series

**User Story:** As an admin, I want to view revenue as a time series with different granularities (daily/weekly/monthly), so that I can analyze business trends.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL expose endpoint `GET /api/dashboard/revenue/timeseries?period=monthly&from=&to=`
2. THE `Dashboard_Service` SHALL support `period` values ∈ { "daily", "weekly", "monthly" }
3. WHEN `period = "daily"`, THE `Query_Handler` SHALL return one data point per day in the range `[from, to]`
4. WHEN `period = "weekly"`, THE `Query_Handler` SHALL group days by week and return total `Revenue` and `OrderCount` per week
5. WHEN `period = "monthly"`, THE `Query_Handler` SHALL group days by month and return total `Revenue` and `OrderCount` per month
6. THE `Query_Handler` SHALL return `RevenueTimeSeriesResult` with `RevenueDataPoint` entries sorted ascending by `Date`
7. WHEN the request is missing `from`, `to`, or has an invalid `period`, THE `Dashboard_Controller` SHALL return HTTP 400 Bad Request
8. THE `Dashboard_Controller` SHALL require a valid JWT — return HTTP 401 if no token is present

---

### Requirement 10: API Endpoint — Admin Rebuild

**User Story:** As an admin, I want the ability to rebuild all materialized views from the EventStore, so that I can fix data inconsistencies or apply new aggregation logic.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL expose endpoint `POST /api/dashboard/admin/rebuild`
2. WHEN `Rebuild_Handler` receives `RebuildMaterializedViewsCommand`, THE `Dashboard_Service` SHALL truncate `DailyRevenueSummary`, `OrderStateRecord`, and `TopProductSnapshot` — do NOT truncate `EventStore`
3. WHEN `Rebuild_Handler` replays events, THE `Dashboard_Service` SHALL read the entire `EventStore` ordered by `SequenceNumber ASC`
4. WHEN `Rebuild_Handler` replays events, THE `Dashboard_Service` SHALL call `ApplyXxx` (pure projection) — do not call `HandleXxx`, do not write back to `EventStore`, no idempotency check
5. WHEN rebuild completes, THE `Dashboard_Service` SHALL return `RebuildResult` containing `ProcessedCount` and `Duration`
6. THE `Dashboard_Controller` SHALL only allow users with the `Admin` role to call this endpoint — return HTTP 403 if insufficient permissions
7. THE `Dashboard_Controller` SHALL require a valid JWT — return HTTP 401 if no token is present

---

### Requirement 11: No Cross-Service Query Architecture

**User Story:** As a system architect, I want to ensure the Dashboard service never queries other services directly, so that loose coupling and service independence are maintained.

#### Acceptance Criteria

1. THE `Dashboard_Service` SHALL have no HTTP clients or database connections pointing to Order DB, Payment DB, or Production DB
2. THE `Dashboard_Service` SHALL only receive data via integration events from RabbitMQ/MassTransit
3. THE `Query_Handler` SHALL only read from Dashboard DB (materialized views) — no external calls

---

### Requirement 12: Authentication and Authorization

**User Story:** As a system administrator, I want to ensure only authenticated users can access the dashboard, so that sensitive business data is protected.

#### Acceptance Criteria

1. THE `Dashboard_Controller` SHALL require a valid JWT for all endpoints — integrated with the `Ecommerce.Authentication` service
2. IF the request has no JWT or an invalid JWT, THEN THE `Dashboard_Controller` SHALL return HTTP 401 Unauthorized
3. IF a user with a valid JWT but without the `Admin` role calls `POST /api/dashboard/admin/rebuild`, THEN THE `Dashboard_Controller` SHALL return HTTP 403 Forbidden
4. THE `Dashboard_Service` SHALL not expose any write endpoints externally except `POST /api/dashboard/admin/rebuild`

---

### Requirement 13: Error Handling and Resilience

**User Story:** As a production system, I want the Dashboard service to handle errors gracefully, so that stability and self-recovery are ensured.

#### Acceptance Criteria

1. IF `Event_Consumer` encounters an exception during processing, THEN THE `Dashboard_Service` SHALL allow MassTransit to automatically retry with exponential backoff policy
2. IF an event fails after N retries, THEN THE `Dashboard_Service` SHALL move the message to the Dead Letter Queue (DLQ) for manual handling
3. IF Dashboard DB is unavailable, THEN THE `Dashboard_Service` SHALL propagate the exception to MassTransit to trigger retry
4. WHEN `Dashboard_Service` restarts after being down, THE `Dashboard_Service` SHALL be able to rebuild materialized views from `EventStore` via `POST /api/dashboard/admin/rebuild`

---

### Requirement 14: Event Payload Serialization and Deserialization

**User Story:** As an event-driven system, I want to store and restore event payloads accurately, so that rebuilds always produce correct results.

#### Acceptance Criteria

1. WHEN `Event_Consumer` saves an event to `EventStore`, THE `Dashboard_Service` SHALL serialize the full event payload as JSON and store it in the `Payload` column
2. WHEN `Rebuild_Handler` replays an event from `EventStore`, THE `Dashboard_Service` SHALL deserialize the `Payload` JSON into the correct event type corresponding to `EventType`
3. THE `Dashboard_Service` SHALL support deserializing the following `EventType` values: "PaymentCompleted", "OrderSubmitted", "OrderCancelled"
4. IF `Payload` cannot be deserialized successfully, THEN THE `Rebuild_Handler` SHALL log the error and skip that event — do not halt the entire rebuild process

---

## Correctness Properties

*A property is a characteristic or behavior that must hold in every valid execution of the system — essentially a formal statement about what the system must do. Properties bridge the gap between natural language specifications and machine-verifiable correctness guarantees.*

### Property 1: Event Processing Idempotency

*For all* events with the same `EventId`, regardless of how many times `Event_Consumer` receives that event (due to retries or at-least-once delivery), the materialized views are updated exactly once — the final result is identical to processing the event exactly once.

**Validates: Requirement 2.1, 2.2**

---

### Property 2: Revenue Consistency

*For all* sets of processed `IPaymentCompletedEvent`, the sum of `DailyRevenueSummary.TotalRevenue` across all history must equal the sum of `Amount` from all those events.

**Validates: Requirement 3.3, 3.4**

---

### Property 3: Revenue Monotonicity

*For all* sequences of `IPaymentCompletedEvent`, `DailyRevenueSummary.TotalRevenue` only increases — it never decreases after processing additional events.

**Validates: Requirement 3.3, 3.5**

---

### Property 4: OrderStateRecord Set-Based Accuracy

*For all* sets of order events, `COUNT(*) FROM OrderStateRecord` must equal the number of unique `OrderId` values that have received events — no duplicate rows or counter drift ever occurs.

**Validates: Requirement 4.1, 4.2**

---

### Property 5: TopProductSnapshot Accuracy

*For all* sets of `IPaymentCompletedEvent`, `TopProductSnapshot.TotalRevenue` for each `ProductId` must equal the sum of `UnitPrice × Quantity` across all completed order items containing that `ProductId`.

**Validates: Requirement 5.2**

---

### Property 6: Rebuild Equivalence

*For all* sets of events in `EventStore`, after `RebuildMaterializedViews` completes, the result of every dashboard query must be identical to the result of processing each event sequentially from the beginning in `SequenceNumber ASC` order.

**Validates: Requirement 10.2, 10.3, 10.4**

---

### Property 7: Serialization Round-Trip

*For all* valid event objects, serializing to JSON and then deserializing must produce an object equivalent to the original.

**Validates: Requirement 14.1, 14.2**

---

### Property 8: Period Aggregation Consistency

*For all* date ranges and sets of `DailyRevenueSummary`, the sum of `Revenue` across all daily data points must equal the sum across all weekly data points, and also equal the sum across all monthly data points — over the same date range.

**Validates: Requirement 9.4, 9.5**

---

### Property 9: EventStore Append-Only Invariant

*For all* operations in the system (including rebuild), the number of records in `EventStore` only increases or stays the same — it never decreases.

**Validates: Requirement 1.5, 10.2**
