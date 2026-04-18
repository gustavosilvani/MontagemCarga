# MontagemCarga.Api

Microsservico de **Montagem de Carga** para agrupar pedidos em carregamentos conforme regras deterministicas de capacidade, filial, tipo de operacao, tipo de carga, rota e disponibilidade de frota.

## Estado atual do MVP

O projeto **ja nao e um stub**. Hoje ele entrega:
- preview deterministico e VRP de agrupamento
- sessao persistida de montagem/roteirizacao
- rejeicao explicita de pedidos inelegiveis e inconsistencias operacionais
- criacao persistida de carregamentos a partir da sessao
- listagem paginada e detalhe rico de carregamentos
- autenticacao JWT e contexto de tenant por embarcador

Fora do escopo atual do MVP:
- multimodal, booking, navio e redespacho avancado
- edicao, cancelamento ou reagruparo manual de carregamentos persistidos

## Estrutura

- **MontagemCarga.Api** - API REST (ASP.NET Core)
- **MontagemCarga.Application** - Commands, Queries, DTOs e Validators
- **MontagemCarga.Domain** - entidades, enums, interfaces e value objects
- **MontagemCarga.Infrastructure** - EF Core (PostgreSQL), repositorios e motor deterministico de agrupamento

## Endpoints

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/v1/montagem-carga/agrupar` | Gera preview imediato de agrupamento sem persistir sessao |
| POST | `/api/v1/montagem-carga/sessoes` | Cria sessao persistida de montagem e devolve o resultado corrente |
| GET | `/api/v1/montagem-carga/sessoes/{id}` | Reabre uma sessao persistida |
| PUT | `/api/v1/montagem-carga/sessoes/{id}` | Atualiza pedidos/parametros da sessao e reprocessa |
| POST | `/api/v1/montagem-carga/sessoes/{id}/pedidos` | Adiciona pedidos na sessao e reprocessa |
| DELETE | `/api/v1/montagem-carga/sessoes/{id}/pedidos/{codigoPedido}` | Remove pedido da sessao e reprocessa |
| POST | `/api/v1/montagem-carga/sessoes/{id}/reprocessar` | Reprocessa a sessao atual |
| POST | `/api/v1/montagem-carga/sessoes/{id}/persistir` | Persiste carregamentos sem divergencia com o preview da sessao |
| POST | `/api/v1/montagem-carga/sessoes/{id}/cancelar` | Cancela a sessao e bloqueia novas alteracoes |
| POST | `/api/v1/montagem-carga/carregamentos` | Cria carregamentos a partir de `pedidos + parametros`; `grupos` e opcional e serve apenas para consistencia do preview |
| GET | `/api/v1/montagem-carga/carregamentos/{id}` | Obtem carregamento persistido por ID |
| GET | `/api/v1/montagem-carga/carregamentos` | Lista carregamentos persistidos com paginacao (`page`, `pageSize`) |

## Contrato atual

### POST /agrupar

Body:

```json
{
  "pedidos": [ { ... } ],
  "parametros": { ... }
}
```

Comportamento atual:
- aceita modos deterministico, `VrpCapacidade`, `VrpTimeWindows` e `SimuladorFrete`
- exige `centroCarregamentoId`
- usa `disponibilidades` efetivas do centro ou fallback quando `utilizarDispFrotaCentroDescCliente = true`
- usa peso, cubagem, pallets, coordenadas, janelas e custo simulado quando o modo exigir
- aceita alguns campos ainda reservados de fase posterior sem alterar o motor atual

### POST /carregamentos

Body:

```json
{
  "pedidos": [ { ... } ],
  "parametros": { ... },
  "grupos": [ { ... } ],
  "filialId": "opcional",
  "empresaId": "opcional"
}
```

Regras:
- `pedidos` e `parametros` sao obrigatorios
- `grupos` e opcional e so protege contra preview desatualizado
- a criacao direta existe por compatibilidade, mas o fluxo recomendado e `sessoes -> persistir`

### Recurso de sessao

O recurso de sessao e hoje a forma principal de operacao:
- guarda pedidos, parametros, agrupamento corrente e numeros reservados
- permite reprocessar incrementalmente ao adicionar ou remover pedidos
- preserva o preview operacional que sera persistido
- devolve resumo operacional, alertas e carregamentos criados

## Campos ativos vs reservados

Ativos no motor atual:
- `Codigo`
- `FilialId`
- `TipoOperacaoId`
- `TipoDeCargaId`
- `RotaFreteId`
- `PesoSaldoRestante`
- `CubagemTotal`
- `NumeroPaletes`
- `DataCarregamentoPedido`
- `Latitude`
- `Longitude`
- `DestinatarioCnpj` quando `AgruparPedidosMesmoDestinatario = true`
- `RemetenteCnpj`
- `RecebedorCnpj`
- `CanalEntregaPrioridade` como criterio de ordenacao
- `CanalEntregaLimitePedidos`
- `PrevisaoEntrega` como criterio de ordenacao
- `JanelaEntregaInicioUtc`
- `JanelaEntregaFimUtc`
- `TempoServicoMinutos`
- `PedidoBloqueado`
- `LiberadoMontagemCarga`
- `NaoUtilizarCapacidadeVeiculo`
- `QuantidadeMaximaEntregasRoteirizar`
- `IgnorarRotaFrete`
- `PermitirPedidoBloqueado`
- `UtilizarDispFrotaCentroDescCliente`
- `ModelosVeiculares.CapacidadePesoTransporte`
- `ModelosVeiculares.ToleranciaPesoExtra`
- `ModelosVeiculares.Cubagem`
- `ModelosVeiculares.NumeroPaletes`
- `TipoMontagemCarregamentoVRP`
- `TipoOcupacaoMontagemCarregamentoVRP`
- `ConfiguracaoRoteirizacao`
- `ConfiguracaoSimulacaoFrete`

Reservados para fase 2, hoje aceitos sem efeito no motor:
- `NivelQuebraProdutoRoteirizar`
- parte do detalhamento incremental de multimodal e redespacho

Nao suportados explicitamente nesta versao:
- `MontagemCarregamentoPedidoProduto = true` como persistencia final real por item
- cenarios multimodais e filtros laterais do legado fora do nucleo operacional

## Executar

1. Configurar PostgreSQL e `ConnectionStrings:DefaultConnection`.
2. Aplicar migracoes:
   ```bash
   dotnet ef database update --project src/MontagemCarga.Infrastructure --startup-project src/MontagemCarga.Api
   ```
3. Subir a API:
   ```bash
   dotnet run --project src/MontagemCarga.Api
   ```
4. Abrir Swagger na porta configurada em `launchSettings.json`.
