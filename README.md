# FCG Users API - Fase 4

Microserviço .NET 10 responsável por cadastro, autenticação JWT e persistência transacional do
evento `UserCreated`, mantendo a evolução transacional da Fase 3 como base para a Fase 4.

## Papel na entrega da Fase 4

UsersAPI está no escopo obrigatório de CI/CD. O workflow em
[publish-container.yml](.github/workflows/publish-container.yml) executa testes em PRs e pushes na
`master`. Em push ou execução manual, autentica na AWS por OIDC, constrói e publica imagens
no Amazon ECR com tags SHA e `latest`, executa Trivy sem bloquear a entrega e atualiza o
Deployment no EKS com `kubectl set image`, aguardando o rollout. PRs executam somente testes.
O Deployment e os recursos de infraestrutura precisam existir antes da execução da pipeline.
Configuração de IAM, cluster e secrets: [guia de CI/CD](https://github.com/posgraduacaofiapnet/FCG-Orchestration/blob/master/docs/CICD.md).

A [documentação central da Fase 4](https://github.com/posgraduacaofiapnet/FCG-Orchestration/blob/master/docs/FASE4.md)
contém a matriz de requisitos e as pendências. O [Orchestration](https://github.com/posgraduacaofiapnet/FCG-Orchestration)
centraliza execução, infraestrutura e relatório de entrega.

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

Cadastro público cria role `User`. O seed inicial pode criar role `Admin`, configurada por
`Admin__Email` e `Admin__Password`; não há endpoint de promoção de role nesta API. O token inclui
`user_id`, e-mail e role. O CatalogAPI exige `Admin` para escrita de jogos e consulta de status
de notificações, e verifica o dono de compra/biblioteca.

## Banco e inicialização

O schema não é criado pela aplicação. Antes de subir a API, aplique os scripts versionados em
`FCG-Orchestration/k8s/database/scripts`. No Compose, `database-init` é uma dependência obrigatória;
no Kubernetes, o `initContainer` executa novamente o script idempotente antes do container da API.

## Execução

```bash
cd FCG-Orchestration
powershell -NoProfile -ExecutionPolicy Bypass -File ./start-local.ps1
```

O comando usa PowerShell. Clonar também CatalogAPI, PaymentsAPI, `FCG-Outbox-Processor` e
`FCG-Notifications-Lambda` como pastas irmãs. O script gera o pacote da Lambda e sobe o Compose;
o fluxo completo usa LocalStack, SQL Server, Redis, MongoDB, RabbitMQ e Kong. A API atende
diretamente em `http://localhost:5101`, Swagger em `/swagger` e pelo Kong em `http://localhost:8000`.

Para Kubernetes, seguir o guia de CI/CD do Orchestration: preparar os Secrets locais a partir
dos exemplos, aplicar com `scripts/apply-k8s-secrets.sh`, depois a infraestrutura e os recursos
das APIs com `scripts/apply-api-manifests.sh`. Os scripts `.sh` exigem Bash (por exemplo, WSL).
Não usar `kubectl apply -f k8s/` nesta API: incluiria `secret.example.yaml` e poderia sobrescrever
um Secret real com placeholders. `k8s/secret.yaml` é local e ignorado pelo Git.

O Kustomize da infraestrutura não aplica gateway, monitoramento, APIs nem workers. O manifesto
da API usa ECR `latest` como imagem inicial; a pipeline seleciona a tag SHA no rollout. Há duas
réplicas e `maxUnavailable: 0`. `/health` não verifica todas as dependências. A documentação
central registra execução no EKS em 11/11/2026; nesta revisão de 16/11/2026 não foi repetido o
deploy nem verificado o ambiente ao vivo. Evidência de tráfego contínuo durante rollout continua
separada da configuração da estratégia.

## Configuração

| Variável | Finalidade |
|---|---|
| `ConnectionStrings__DefaultConnection` | Conexão SQL de `FCGUsersDb` |
| `Jwt__Key` | Chave JWT, coerente com CatalogAPI e gateway |
| `Jwt__Issuer` / `Jwt__Audience` | Emissor e audiência validados entre serviços |
| `Admin__Email` / `Admin__Password` | Identificação e senha do administrador inicial |
| `ASPNETCORE_URLS` | Endereço de escuta, porta 8080 no container |

Não registrar valores sensíveis na documentação. Existem valores fixos no código/configurações
atuais; a exigência de zero credenciais fixas da Fase 4 ainda não foi atendida. Usar Secrets em
runtime não resolve a presença de valores versionados por si só.

## Testes

Na raiz deste repositório:

```bash
dotnet test tests/UsersAPI.UnitTests/UsersAPI.UnitTests.csproj --configuration Release
```

Em 10/11/2026: 16 testes aprovados. O restore reportou NU1902/NU1903 para
`System.Security.Cryptography.Pkcs` 9.0.4. Essa execução não valida SQS/Lambda nem o cluster cloud.
