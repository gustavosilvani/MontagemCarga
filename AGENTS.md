# AGENTS.md

Instrucoes especificas do `MontagemCarga`.

## Papel deste repositorio

- dominio operacional especializado de montagem de carga
- ownership de agrupamento, sessao, persistencia e reprocessamento da montagem

## Regras

- manter o dominio de montagem isolado do fluxo fiscal
- expor contratos claros para consumo pelo `core-cte`
- nao mover regras de montagem para o `core-cte` por conveniencia
