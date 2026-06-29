# Implementation Tasks: Ecommerce Dashboard API

## Task 1: Project Structure Setup

### 1.1 Create Dashboard Service Directory Structure
- [x] Create `Services/Ecommerce.Dashboard/` directory
- [x] Create `Services/Ecommerce.Dashboard/Dashboard.Domain/` project
- [x] Create `Services/Ecommerce.Dashboard/Dashboard.Application/` project  
- [x] Create `Services/Ecommerce.Dashboard/Dashboard.Infrastructure/` project
- [x] Create `Services/Ecommerce.Dashboard/Dashboard.API/` project
- [x] Add projects to main solution file `Microservice.Ecommerce.sln`

### 1.2 Configure Project References
- [x] Dashboard.Application references Dashboard.Domain
- [x] Dashboard.Infrastructure references Dashboard.Application and Dashboard.Domain
- [x] Dashboard.API references Dashboard.Infrastructure, Dashboard.Application, and Dashboard.Domain
- [x] Add BuildingBlocks references to all projects as needed

### 1.3 Add NuGet Package Dependencies
- [x] Add MassTransit.RabbitMQ to Dashboard.Infrastructure
- [x] Add MediatR to Dashboard.Application
- [x] Add Entity Framework Core packages to Dashboard.Infrastructure
- [x] Add Npgsql.EntityFrameworkCore.PostgreSQL to Dashboard.Infrastructure
- [x] Add authentication packages to Dashboard.API

## Task 2: Domain Layer Implementation

### 2.1 Create Domain Entities
- [x] Create `DailyRevenueSummary` entity with Date, TotalRevenue, CompletedOrderCount, CancelledOrderCount
- [x] Create `OrderStateRecord` entity with OrderId, Status, LastUpdatedAt (set-based approach)
- [x] Create `TopProductSnapshot` entity with ProductId, ProductName, TotalQuantitySold, TotalRevenue, LastSoldAt
- [x] Create `DashboardEventStore` entity with EventId, EventType, Payload, OccurredOn, ReceivedAt, SequenceNumber

### 2.2 Create Domain Value Objects and Enums
- [ ] Create `TimePeriod` enum (Daily, Weekly, Monthly)
- [ ] Create `DateRangeFilter` value object
- [ ] Create `OrderStatus` enum matching existing system ("Draft", "Pending", "Completed", "Cancelled")

### 2.3 Create Domain Events (if needed)
- [ ] Create domain events for audit trail if required

## Task 3: Application Layer Implementation

### 3.1 Create CQRS Commands and Queries
- [ ] Create `UpdateRevenueOnPaymentCommand` with OrderId, Amount, OccurredOn, Items
- [ ] Create `UpdateOrderStateCommand` with OrderId, NewStatus, OccurredOn
- [ ] Create `UpdateTopProductsCommand` with Items, OccurredOn
- [ ] Create `RebuildMaterializedViewsCommand` and `RebuildResult`
- [ ] Create `GetRevenueSummaryQuery` and `RevenueSummaryResult`
- [ ] Create `GetOrderStatusSummaryQuery` and `OrderStatusSummaryResult`
- [ ] Create `GetTopProductsQuery` and `TopProductsResult`
- [ ] Create `GetRevenueTimeSeriesQuery` and `RevenueTimeSeriesResult`

### 3.2 Create Command Handlers (Aggregation)
- [ ] Implement `UpdateRevenueOnPaymentHandler` with atomic idempotency using INSERT ON CONFLICT DO NOTHING
- [ ] Implement `UpdateOrderStateHandler` for set-based order status tracking
- [ ] Implement `UpdateTopProductsHandler` for product sales aggregation
- [ ] Implement `RebuildMaterializedViewsHandler` for admin rebuild functionality

### 3.3 Create Query Handlers
- [ ] Implement `GetRevenueSummaryHandler` with date range filtering and AOV calculation
- [ ] Implement `GetOrderStatusSummaryHandler` with GROUP BY status aggregation
- [ ] Implement `GetTopProductsHandler` with TOP N filtering and sorting
- [ ] Implement `GetRevenueTimeSeriesHandler` with period-based aggregation (daily/weekly/monthly)

### 3.4 Create DTOs and Response Models
- [ ] Create `OrderItemDto` for event payload
- [ ] Create `RevenueSummaryResult`, `OrderStatusSummaryResult`, `TopProductsResult`, `RevenueTimeSeriesResult`
- [ ] Create `TopProductItem`, `RevenueDataPoint` DTOs
- [ ] Create `ApiResponse<T>` wrapper for consistent API responses

### 3.5 Create Application Services and Abstractions
- [ ] Create `IDashboardDbContext` interface
- [ ] Create application service interfaces if needed
- [ ] Add MediatR registration and dependency injection setup

## Task 4: Infrastructure Layer Implementation

### 4.1 Create Database Context and Configuration
- [ ] Create `DashboardDbContext` inheriting from DbContext
- [ ] Configure entity mappings with proper indexes (Date unique, ProductId unique, etc.)
- [ ] Configure PostgreSQL-specific features (BIGSERIAL for SequenceNumber)
- [ ] Add database connection string configuration

### 4.2 Create Event Consumers
- [ ] Create `PaymentCompletedConsumer` implementing IConsumer<IPaymentCompletedEvent>
- [ ] Create `OrderSubmittedConsumer` implementing IConsumer<IOrderSubmittedEvent>  
- [ ] Create `OrderCancelledConsumer` implementing IConsumer<ICancelOrderCommand>
- [ ] Implement atomic idempotency pattern in all consumers

### 4.3 Create Repository Implementations (if needed)
- [ ] Create repository implementations if not using EF Core directly
- [ ] Implement unit of work pattern if required

### 4.4 Configure MassTransit and RabbitMQ
- [ ] Configure MassTransit with RabbitMQ transport
- [ ] Register event consumers with proper retry policies
- [ ] Configure Dead Letter Queue (DLQ) handling
- [ ] Add exponential backoff retry policy

### 4.5 Create Database Migrations
- [ ] Create initial migration for all dashboard entities
- [ ] Ensure proper indexes are created (Date, ProductId, SequenceNumber, Status)
- [ ] Configure unique constraints and foreign keys

## Task 5: API Layer Implementation

### 5.1 Create Dashboard Controller
- [ ] Create `DashboardController` with [Authorize] attribute
- [ ] Implement `GetRevenueSummary` endpoint with date range parameters
- [ ] Implement `GetOrderStatusSummary` endpoint
- [ ] Implement `GetTopProducts` endpoint with topN parameter and validation
- [ ] Implement `GetRevenueTimeSeries` endpoint with period and date range parameters

### 5.2 Create Admin Controller
- [ ] Create admin-only `RebuildMaterializedViews` endpoint with [Authorize(Roles = "Admin")]
- [ ] Implement proper error handling and response formatting
- [ ] Add request validation and parameter checking

### 5.3 Configure Authentication and Authorization
- [ ] Configure JWT authentication integration with Ecommerce.Authentication service
- [ ] Set up role-based authorization for Admin endpoints
- [ ] Configure authentication middleware and policies

### 5.4 Add API Documentation and Validation
- [ ] Add Swagger/OpenAPI documentation for all endpoints
- [ ] Implement request validation attributes
- [ ] Add proper HTTP status code responses (400, 401, 403, 500)
- [ ] Create API response models and error handling

### 5.5 Configure Dependency Injection
- [ ] Register all services in Program.cs or DependencyInjection.cs
- [ ] Configure database context with connection string
- [ ] Register MediatR and MassTransit services
- [ ] Configure logging and health checks

## Task 6: Integration and Event Handling

### 6.1 Extend BuildingBlocks Events (if needed)
- [ ] Check if IPaymentCompletedEvent has Amount and Items properties
- [ ] Extend IPaymentCompletedEvent interface if missing required fields
- [ ] Update event publishers in Payment service if needed
- [ ] Ensure event correlation between Order and Payment services

### 6.2 Implement Event Processing Logic
- [ ] Implement HandlePaymentCompleted with all materialized view updates in single transaction
- [ ] Implement HandleOrderSubmitted for order state tracking
- [ ] Implement HandleOrderCancelled for order state updates
- [ ] Add proper error handling and logging for event processing

### 6.3 Create Projection Functions for Rebuild
- [ ] Create ApplyPaymentCompleted (pure projection, no side effects)
- [ ] Create ApplyOrderSubmitted (pure projection)
- [ ] Create ApplyOrderCancelled (pure projection)
- [ ] Ensure ApplyXxx functions are separate from HandleXxx functions

## Task 7: Configuration and Deployment

### 7.1 Add Configuration Files
- [ ] Create appsettings.json with database connection strings
- [ ] Add RabbitMQ configuration settings
- [ ] Configure JWT authentication settings
- [ ] Add logging configuration

### 7.2 Create Docker Configuration
- [ ] Create Dockerfile for Dashboard.API
- [ ] Update docker-compose.yml to include Dashboard service
- [ ] Configure service dependencies and networking
- [ ] Add environment variables for configuration

### 7.3 Add Health Checks and Monitoring
- [ ] Implement health check endpoints for database and RabbitMQ
- [ ] Add logging for event processing and API requests
- [ ] Configure metrics and monitoring if needed

## Task 8: Testing Implementation

### 8.1 Create Unit Tests
- [ ] Create unit tests for command handlers with in-memory database
- [ ] Create unit tests for query handlers
- [ ] Test idempotency behavior with duplicate events
- [ ] Test aggregation logic and calculations

### 8.2 Create Integration Tests
- [ ] Create integration tests with Testcontainers (PostgreSQL + RabbitMQ)
- [ ] Test end-to-end event processing flow
- [ ] Test API endpoints with authentication
- [ ] Test rebuild functionality

### 8.3 Create Property-Based Tests
- [ ] Implement property tests for atomic idempotency
- [ ] Test revenue monotonicity property
- [ ] Test aggregation consistency across time periods
- [ ] Test rebuild equivalence property

## Task 9: Performance Optimization

### 9.1 Database Optimization
- [ ] Add proper database indexes for query performance
- [ ] Configure connection pooling and timeout settings
- [ ] Implement database partitioning for large tables if needed
- [ ] Add query performance monitoring

### 9.2 Caching Implementation (Optional)
- [ ] Add Redis caching for frequently accessed data
- [ ] Implement cache invalidation strategies
- [ ] Configure cache TTL policies
- [ ] Add cache performance metrics

## Task 10: Documentation and Deployment

### 10.1 Create Documentation
- [ ] Document API endpoints and usage examples
- [ ] Create deployment and configuration guide
- [ ] Document event processing flow and troubleshooting
- [ ] Add performance tuning recommendations

### 10.2 Final Integration and Testing
- [ ] Test complete system integration with existing services
- [ ] Verify event flow from Order/Payment services to Dashboard
- [ ] Test admin rebuild functionality in staging environment
- [ ] Perform load testing and performance validation

### 10.3 Production Deployment
- [ ] Deploy to staging environment for testing
- [ ] Configure production database and RabbitMQ
- [ ] Set up monitoring and alerting
- [ ] Deploy to production with proper rollback plan