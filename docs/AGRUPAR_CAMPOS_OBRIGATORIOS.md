# Campos do agrupamento (POST /agrupar)

Este documento descreve o contrato **real** do `POST /api/v1/montagem-carga/agrupar`.

## Resumo do motor atual

O endpoint aceita um payload rico e hoje executa:
- agrupamento deterministico e VRP
- ocupacao por peso, cubagem e pallet
- regras de filial, tipo de operacao, tipo de carga, rota, remetente, recebedor e destinatario opcional
- disponibilidade efetiva do centro ou fallback controlado
- janelas de entrega e simulacao de frete quando o modo exigir

Alguns campos continuam reservados para a proxima onda, mas a maior parte do contrato de fase 2 ja influencia o resultado.

## Request body

```json
{
  "pedidos": [ { ... } ],
  "parametros": { ... }
}
```

## Pedidos

| Campo | Obrigatorio | Status no motor atual | Observacao |
|------|-------------|-----------------------|------------|
| `Codigo` | Sim | Ativo | Identificador unico do pedido no payload |
| `FilialId` | Sim | Ativo | Faz parte da chave-base do agrupamento |
| `TipoOperacaoId` | Nao | Ativo | Faz parte da chave-base quando informado |
| `TipoDeCargaId` | Nao | Ativo | Faz parte da chave-base quando informado |
| `RotaFreteId` | Nao | Ativo | Considerado quando `IgnorarRotaFrete = false` |
| `PesoSaldoRestante` | Sim | Ativo | Base da capacidade do modelo |
| `DataCarregamentoPedido` | Sim | Ativo | Usado em validacao e ordenacao |
| `DestinatarioCnpj` | Nao | Ativo parcial | So entra na chave quando `AgruparPedidosMesmoDestinatario = true` |
| `CanalEntregaPrioridade` | Nao | Ativo parcial | Usado como criterio de ordenacao |
| `PrevisaoEntrega` | Nao | Ativo parcial | Usado como criterio de ordenacao |
| `NaoUtilizarCapacidadeVeiculo` | Nao | Ativo | Pedido entra no grupo sem consumir capacidade de peso |
| `PedidoBloqueado` | Nao | Ativo | Pode excluir o pedido conforme parametro |
| `LiberadoMontagemCarga` | Nao | Ativo | Pedido nao liberado e rejeitado |
| `RemetenteCnpj` | Nao | Ativo | Entra na compatibilidade logistica do grupo |
| `RecebedorCnpj` | Nao | Ativo | Entra na compatibilidade logistica do grupo |
| `Latitude` | Nao | Ativo em VRP | Obrigatorio para modos VRP |
| `Longitude` | Nao | Ativo em VRP | Obrigatorio para modos VRP |
| `CanalEntregaLimitePedidos` | Nao | Ativo | Limita quantidade de pedidos por canal quando informado |
| `Itens` | Nao | Ativo parcial | Usado quando `MontagemCarregamentoPedidoProduto = true` |

## Parametros

| Campo | Obrigatorio | Status no motor atual | Observacao |
|------|-------------|-----------------------|------------|
| `DataPrevistaCarregamento` | Sim | Ativo | Data base do preview |
| `CentroCarregamentoId` | Sim | Ativo | Obrigatorio nesta versao |
| `QuantidadeMaximaEntregasRoteirizar` | Nao | Ativo | Limite por grupo quando maior que zero |
| `AgruparPedidosMesmoDestinatario` | Nao | Ativo | Inclui destinatario na chave-base |
| `IgnorarRotaFrete` | Nao | Ativo | Remove `RotaFreteId` da chave-base |
| `PermitirPedidoBloqueado` | Nao | Ativo | Permite incluir pedidos bloqueados |
| `UtilizarDispFrotaCentroDescCliente` | Nao | Ativo | Permite fallback sem disponibilidade explicita |
| `Disponibilidades` | Condicional | Ativo | Obrigatoria quando nao ha fallback |
| `ModelosVeiculares` | Sim | Ativo | Pelo menos um modelo elegivel |
| `TipoMontagemCarregamentoVRP` | Nao | Ativo | Aceita `Nenhum`, `VrpCapacidade`, `VrpTimeWindows` e `SimuladorFrete` |
| `TipoOcupacaoMontagemCarregamentoVRP` | Nao | Ativo | Define a metrica primaria de ocupacao |
| `MontagemCarregamentoPedidoProduto` | Nao | Ativo parcial | Agrupa em nivel de item no preview e na sessao |
| `NivelQuebraProdutoRoteirizar` | Nao | Ativo parcial | Controla o nivel de quebra do pedido-produto |

## ModelosVeiculares

| Campo | Obrigatorio | Status no motor atual | Observacao |
|------|-------------|-----------------------|------------|
| `Id` | Sim | Ativo | Identificador do modelo |
| `Descricao` | Sim | Ativo | Descricao para auditoria e retorno |
| `CapacidadePesoTransporte` | Sim | Ativo | Capacidade base do modelo |
| `ToleranciaPesoExtra` | Nao | Ativo | Soma na capacidade total de peso |
| `Cubagem` | Nao | Ativo | Capacidade secundaria do modelo |
| `NumeroPaletes` | Nao | Ativo | Capacidade secundaria do modelo |

## Regras importantes

- `pedidos` nao pode vir vazio
- codigos duplicados no payload sao rejeitados
- pedido com data posterior a `DataPrevistaCarregamento` e rejeitado
- pedido sem disponibilidade de modelo ou sem capacidade entra em `pedidosNaoAgrupados`
- modos VRP exigem coordenadas do centro e dos pedidos
- `VrpTimeWindows` exige janela de entrega dos pedidos
- `SimuladorFrete` exige configuracao explicita de frete
- o endpoint gera apenas preview; para operacao persistida use o recurso `/sessoes`

## Relacao com POST /carregamentos

O `POST /carregamentos` reaplica o mesmo motor usando `pedidos + parametros`.

`grupos` pode ser enviado, mas apenas para verificar se o preview ainda esta consistente. Enviar somente `grupos` nao e suportado.

O fluxo operacional principal agora e:
- `POST /sessoes`
- `GET/PUT/POST/DELETE /sessoes/...`
- `POST /sessoes/{id}/persistir`
