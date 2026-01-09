# Microservice E‑Commerce Platform

A production‑grade, fully containerized **Microservices E‑Commerce Platform** built on **.NET 8**, leveraging **gRPC** for high‑performance inter‑service communication, **RabbitMQ** for asynchronous event‑driven workflows, **PostgreSQL** as the primary relational datastore for all services, **SignalR** for real‑time interactions, and a centralized **API Gateway** as the unified entrypoint. The system follows Clean Architecture, SOLID principles, and is fully orchestrated using **Docker Compose**.

---

## 🚀 Tech Overview

* **Identity Service** – JWT Auth (OAuth2/OIDC)
* **Product Service** – Catalog & Inventory
* **Cart Service** – Shopping Cart
* **Order Service** – Order Lifecycle
* **Payment Service** – Async Payment Pipeline + RabbitMQ Events
* **Notification Service** – SignalR + Email
* **API Gateway** – Unified entry point

Inter‑service communication:

* **gRPC** (sync, high‑performance)
* **RabbitMQ** (async event-driven workflows)

---

## 🐳 Run the Entire System (Docker Compose)

**Start all services:**

```
docker compose up -d --build
```

**Stop:**

```
docker compose down
```

---

## 🔌 Service Ports

| Service                | Port  |
| ---------------------- | ----- |
| API Gateway            | 8000  |
| Identity               | 7001  |
| Product                | 7002  |
| Cart                   | 7003  |
| Order                  | 7004  |
| Payment                | 7005  |
| Notification (SignalR) | 7006  |
| RabbitMQ UI            | 15672 |

---

## 🧪 Run Tests

```
dotnet test
```

---

## 📦 Project Structure

```
Services/
 ├── Identity
 ├── Product
 ├── Cart
 ├── Order
 ├── Payment
 └── Notification
Gateway/
docker-compose.yml
```
