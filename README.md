# FCG Users API

Microserviço .NET 10 responsável por cadastro, autenticação JWT e persistência transacional do
evento `UserCreated` para a Fase 3.

## Cadastro e outbox

`POST /api/auth/register` grava `Users` e `OutboxMessages` na mesma transação SQL. O payload
contém `userId`, `name`, `email` e `createdAt`. O `FCG-Outbox-Processor` publica o envelope na
SQS e a `FCG-Notifications-Lambda` executa o serviço de e-mail de boas-vindas.

Não há publicação direta na fila pelo processo HTTP. Se o SQL falhar, nem usuário nem evento
são persistidos; se a SQS estiver indisponível, o evento permanece elegível para retentativa.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/auth/register` | Cadastra usuário e cria o evento de outbox |
| `POST` | `/api/auth/login` | Retorna token JWT |
| `GET` | `/health` | Health check |
| `GET` | `/metrics` | Métricas Prometheus |

## Banco e inicialização

O schema não é criado pela aplicação. Antes de subir a API, aplique os scripts versionados em
`FCG-Orchestration/k8s/database/scripts`. No Compose, `database-init` é uma dependência obrigatória;
no Kubernetes, o `initContainer` executa novamente o script idempotente antes do container da API.

## Execução

```bash
cd FCG-Orchestration
docker compose up --build
```

Para Kubernetes, aplique primeiro `kubectl apply -k FCG-Orchestration/k8s`, depois os
manifestos desta API e do worker. Os testes existem na solution, mas devem ser executados em uma
etapa de validação separada.
