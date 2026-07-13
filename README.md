# FCG-UsersAPI

Microserviço responsável pelo cadastro de usuários, autenticação via JWT e publicação do evento `UserCreatedEvent` no RabbitMQ.

Parte do **FIAP Cloud Games (FCG)** — Tech Challenge Fase 2.

---

## Tecnologias

- .NET 10 / ASP.NET Core
- Entity Framework Core 10 + SQL Server
- MassTransit + RabbitMQ
- JWT Bearer Authentication
- FluentValidation
- Swagger / OpenAPI
- Serilog (logs estruturados em JSON)

---

## Endpoints

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `POST` | `/api/auth/register` | Cadastra um novo usuário | Não |
| `POST` | `/api/auth/login` | Realiza login e retorna o token JWT | Não |
| `GET` | `/health` | Health check | Não |

### Payload de Cadastro

```json
{
  "name": "João Silva",
  "email": "joao@exemplo.com",
  "password": "Senha@123"
}
```

### Payload de Login

```json
{
  "email": "joao@exemplo.com",
  "password": "Senha@123"
}
```

### Resposta do Login

```json
{
  "token": "<jwt>",
  "userId": "<guid>",
  "name": "João Silva",
  "email": "joao@exemplo.com",
  "role": "User"
}
```

> O token JWT retornado deve ser usado como `Bearer <token>` no header `Authorization` de todos os endpoints autenticados das demais APIs.

---

## Eventos

| Direção | Evento | Gatilho |
|---------|--------|---------|
| Publica | `UserCreatedEvent` | Após cadastro bem-sucedido |

O evento carrega `UserId`, `Name`, `Email` e `CorrelationId`, permitindo que a **NotificationsAPI** envie o e-mail de boas-vindas de forma assíncrona.

---

## Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | String de conexão do SQL Server |
| `Jwt__Key` | Chave secreta HMAC para assinatura dos tokens JWT |
| `Jwt__Issuer` | Emissor do token (ex: `UsersAPI`) |
| `Jwt__Audience` | Audiência do token (ex: `FCG`) |
| `RabbitMq__Host` | Hostname do RabbitMQ |
| `RabbitMq__Username` | Usuário do RabbitMQ |
| `RabbitMq__Password` | Senha do RabbitMQ |

> **Importante:** `Jwt__Key`, `Jwt__Issuer` e `Jwt__Audience` devem ser **idênticos** aos configurados na **FCG-CatalogAPI**, pois o token emitido aqui é validado lá.

---

## Executando Localmente

### Docker Compose (via FCG-Orchestration)

```bash
cd FCG-Orchestration
docker compose up --build
```

Swagger disponível em: http://localhost:5101/swagger

### Kubernetes

```bash
# 1. Build da imagem local
cd FCG-UsersAPI
docker build -t fcg-users-api:latest -f services/UsersAPI/Dockerfile .

# 2. Aplique a infra (RabbitMQ + SQL Server) primeiro
cd ../FCG-Orchestration/k8s
kubectl apply -f .

# 3. Aplique os manifestos da UsersAPI
cd ../../FCG-UsersAPI/k8s
kubectl apply -f .

# 4. Verifique os pods
kubectl get pods
kubectl get services

# 5. Acesse via port-forward
kubectl port-forward service/users-api 5101:80
```

Swagger disponível em: http://localhost:5101/swagger

#### Manifestos Kubernetes

| Arquivo | Tipo | Descrição |
|---------|------|-----------|
| `deployment.yaml` | Deployment | Define o Pod com 1 réplica, imagem, probes e referências a ConfigMap/Secret |
| `service.yaml` | Service | Expõe a API internamente no cluster na porta 80 |
| `configmap.yaml` | ConfigMap | Configurações não-sensíveis (RabbitMQ host/username, Jwt Issuer/Audience) |
| `secret.yaml` | Secret | Dados sensíveis em base64 (connection string, Jwt Key, RabbitMQ password) |

As **readinessProbe** e **livenessProbe** do Deployment apontam para `/health` — o pod só recebe tráfego após o healthcheck passar.

---

## Testes Unitários

```bash
cd FCG-UsersAPI
dotnet test FCG-UsersAPI.sln
```

Os testes utilizam **xUnit**, **Bogus** para geração de dados fictícios e o provider **InMemory** do Entity Framework Core para isolar a camada de persistência sem banco real.

---

## Estrutura da Solution

```
FCG-UsersAPI/
├── FCG-UsersAPI.sln
├── contracts/
│   └── FCG.Contracts/        # Contratos de eventos compartilhados
├── services/
│   └── UsersAPI/             # Projeto principal do serviço
├── tests/
│   └── UsersAPI.Tests/       # Testes unitários (xUnit)
└── k8s/                      # Manifestos Kubernetes
    ├── deployment.yaml
    ├── service.yaml
    ├── configmap.yaml
    └── secret.yaml
```

---

## Repositórios Relacionados

- [FCG-Orchestration](https://github.com/posgraduacaofiapnet/FCG-Orchestration) — Docker Compose + infraestrutura K8s global
- [FCG-CatalogAPI](https://github.com/posgraduacaofiapnet/FCG-CatalogAPI)
- [FCG-PaymentsAPI](https://github.com/posgraduacaofiapnet/FCG-PaymentsAPI)
- [FCG-NotificationsAPI](https://github.com/posgraduacaofiapnet/FCG-NotificationsAPI)
