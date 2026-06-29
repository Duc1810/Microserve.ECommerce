# Microservices Deployment Guide

This directory contains Docker Compose configurations for deploying the microservices architecture on AWS EC2 instances.

## Architecture Overview

The application is deployed across two EC2 instances:

### EC2 #1 (Public Subnet) - API Gateway
- **Purpose**: API Gateway only
- **Services**: Ocelot API Gateway
- **Docker Compose**: `gateway-compose.yml`
- **Access**: Internet → API Gateway (Port 80)

### EC2 #2 (Private Subnet) - Backend Services
- **Purpose**: Backend Services and Infrastructure
- **Docker Compose**: `backend-compose.yml`
- **Services**:
  - AuthAPI (Port 5000)
  - ProductAPI (Port 5001)
  - CartAPI (Port 5002)
  - OrderAPI (Port 5003)
  - EmailAPI (Port 5004)
  - NotificationAPI (Port 5005)
- **Infrastructure**:
  - PostgreSQL (Ports 5433, 5434, 5435)
  - Redis (Port 6379)
  - RabbitMQ (Ports 5672, 15672)
  - Elasticsearch (Port 9200)
  - Kibana (Port 5601)
  - Grafana (Port 3000)
  - Prometheus (Port 9090)
  - Jaeger (Port 16686)
  - Seq (Ports 5341, 5342)

## Deployment Instructions

### Prerequisites
1. Two EC2 instances configured with Docker and Docker Compose
2. Security groups configured for proper port access
3. Private networking between EC2 instances

### Step 1: Configure Environment Variables

1. Copy the environment template:
   ```bash
   cp .env.example .env
   ```

2. Update the `.env` file with your Backend EC2 private IP:
   ```bash
   # Replace 10.0.2.100 with your actual Backend EC2 private IP
   AUTH_API_HOST=10.0.2.100
   PRODUCT_API_HOST=10.0.2.100
   CART_API_HOST=10.0.2.100
   ORDER_API_HOST=10.0.2.100
   EMAIL_API_HOST=10.0.2.100
   NOTIFICATION_API_HOST=10.0.2.100
   ```

### Step 2: Deploy Backend Services (Private EC2)

1. Copy files to Backend EC2:
   ```bash
   scp -r deployment/ backend-compose.yml .env user@backend-ec2-ip:~/
   ```

2. SSH to Backend EC2 and deploy:
   ```bash
   ssh user@backend-ec2-ip
   cd ~/deployment
   docker-compose -f backend-compose.yml up -d
   ```

3. Verify services are running:
   ```bash
   docker-compose -f backend-compose.yml ps
   ```

### Step 3: Deploy API Gateway (Public EC2)

1. Copy files to Gateway EC2:
   ```bash
   scp -r deployment/ gateway-compose.yml .env user@gateway-ec2-ip:~/
   ```

2. SSH to Gateway EC2 and deploy:
   ```bash
   ssh user@gateway-ec2-ip
   cd ~/deployment
   docker-compose -f gateway-compose.yml up -d
   ```

3. Verify API Gateway is running:
   ```bash
   docker-compose -f gateway-compose.yml ps
   ```

## Port Mapping

### Backend EC2 Exposed Ports
| Service | Internal Port | Host Port | Purpose |
|---------|---------------|-----------|---------|
| AuthAPI | 8080 | 5000 | API Gateway access |
| ProductAPI | 8080 | 5001 | API Gateway access |
| CartAPI | 5027 | 5002 | API Gateway access |
| OrderAPI | 8080 | 5003 | API Gateway access |
| EmailAPI | 8080 | 5004 | API Gateway access |
| NotificationAPI | 8080 | 5005 | API Gateway access |
| ProductAPI gRPC | 7001 | 5246 | Internal service communication |

### Gateway EC2 Exposed Ports
| Service | Internal Port | Host Port | Purpose |
|---------|---------------|-----------|---------|
| API Gateway | 8080 | 80 | Public internet access |

## Security Groups Configuration

### Gateway EC2 Security Group
- **Inbound Rules**:
  - HTTP (80) from 0.0.0.0/0
  - SSH (22) from your IP
- **Outbound Rules**:
  - All traffic to Backend EC2 private IP
  - HTTPS (443) to 0.0.0.0/0 (for external dependencies)

### Backend EC2 Security Group
- **Inbound Rules**:
  - Ports 5000-5005 from Gateway EC2 private IP
  - SSH (22) from your IP
  - All traffic from Backend EC2 security group (self-reference)
- **Outbound Rules**:
  - All traffic to 0.0.0.0/0

## Network Communication

### External to Internal
```
Internet → Gateway EC2:80 → Backend EC2:5000-5005
```

### Internal Service Communication
Services within the Backend EC2 communicate using Docker container names:
- `cartAPI` → `productAPI:7001` (gRPC)
- `cartAPI` → `orderAPI:8080` (HTTP)
- All services → `authapi:8080` (Authentication)
- All services → `redis:6379`, `messagebroker:5672`, etc.

## Monitoring and Logging

Access monitoring tools via Backend EC2 public IP (if configured) or through SSH tunneling:

- **Grafana**: http://backend-ec2-ip:3000 (admin/admin)
- **Prometheus**: http://backend-ec2-ip:9090
- **Kibana**: http://backend-ec2-ip:5601
- **Jaeger**: http://backend-ec2-ip:16686
- **Seq**: http://backend-ec2-ip:5341
- **RabbitMQ Management**: http://backend-ec2-ip:15672 (guest/guest)

## Troubleshooting

### Check Service Health
```bash
# Backend services
docker-compose -f backend-compose.yml ps
docker-compose -f backend-compose.yml logs [service-name]

# Gateway service
docker-compose -f gateway-compose.yml ps
docker-compose -f gateway-compose.yml logs apigateway
```

### Test Connectivity
```bash
# From Gateway EC2, test Backend connectivity
curl http://10.0.2.100:5000/health
curl http://10.0.2.100:5001/health
```

### Common Issues
1. **Environment variables not loaded**: Ensure `.env` file is in the same directory as docker-compose files
2. **Network connectivity**: Verify security groups and private IP configuration
3. **Service startup order**: Backend services must be running before Gateway deployment

## Rollback Procedure

### Stop Services
```bash
# Gateway EC2
docker-compose -f gateway-compose.yml down

# Backend EC2
docker-compose -f backend-compose.yml down
```

### Revert to Single-Node Deployment
Use the original `docker-compose.yml` in the project root for single-node deployment.

## Scaling Considerations

- **Horizontal Scaling**: Add more Backend EC2 instances and configure load balancing
- **Database Scaling**: Consider managed PostgreSQL (RDS) for production
- **Cache Scaling**: Consider managed Redis (ElastiCache) for production
- **Message Queue Scaling**: Consider managed RabbitMQ (Amazon MQ) for production