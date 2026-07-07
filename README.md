# FCG-UsersAPI

Microservice responsible for user registration, authentication (JWT), and publishing the `UserCreatedEvent` to RabbitMQ.

Part of **FIAP Cloud Games (FCG)** — Tech Challenge Phase 2.

## Tech Stack

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 + SQL Server
- MassTransit + RabbitMQ
- JWT Bearer Authentication
- FluentValidation
- Swagger / OpenAPI

## Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/auth/register` | Register a new user | No |
| `POST` | `/api/auth/login` | Login and receive JWT token | No |
| `GET` | `/health` | Health check | No |

### Register payload

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "Senha@123"
}
```

### Login payload

```json
{
  "email": "john@example.com",
  "password": "Senha@123"
}
```

### Login response

```json
{
  "token": "<jwt>",
  "userId": "<guid>",
  "name": "John Doe",
  "email": "john@example.com",
  "role": "User"
}
```

## Events

| Direction | Event | Trigger |
|-----------|-------|---------|
| Publishes | `UserCreatedEvent` | After successful registration |

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Key` | JWT signing secret key |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |

## Running Locally

### Docker Compose (via FCG-Orchestration)

```bash
cd FCG-Orchestration
docker compose up --build
```

Swagger: http://localhost:5101/swagger

### Kubernetes

```bash
# Build the image first
cd FCG-UsersAPI
docker build -t fcg-users-api:latest -f services/UsersAPI/Dockerfile .

# Apply manifests
cd k8s
kubectl apply -f .

# Verify
kubectl get pods
kubectl port-forward service/users-api 5101:80
```

Swagger: http://localhost:5101/swagger

## Solution Structure

```
FCG-UsersAPI/
├── FCG-UsersAPI.sln
├── contracts/
│   └── FCG.Contracts/        # Shared event contracts
├── services/
│   └── UsersAPI/             # Main service project
└── k8s/                      # Kubernetes manifests
    ├── deployment.yaml
    ├── service.yaml
    ├── configmap.yaml
    └── secret.yaml
```

## Related Repositories

- [FCG-Orchestration](https://github.com/posgraduacaofiapnet/FCG-Orchestration) — Docker Compose + global K8s infra
- [FCG-CatalogAPI](https://github.com/posgraduacaofiapnet/FCG-CatalogAPI)
- [FCG-PaymentsAPI](https://github.com/posgraduacaofiapnet/FCG-PaymentsAPI)
- [FCG-NotificationsAPI](https://github.com/posgraduacaofiapnet/FCG-NotificationsAPI)
