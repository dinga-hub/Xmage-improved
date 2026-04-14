# XMage-improved

XMage Commander AI melhorado para partidas multiplayer locais.

## O que é?

Patches para os bots do XMage 1.4.58 que melhoram significativamente o comportamento da IA em partidas Commander (4 jogadores).

## Sprints implementadas

| Sprint | Descrição |
|--------|-----------|
| 1+8 | evaluatePlayerThreat(): board presence, ramp, mão, vida |
| 2 | Ataca o jogador mais ameaçador |
| 3 | Reserva bloqueador quando oponente é ameaçador |
| 4 | Remoção ponderada por ameaça |
| 5 | Guarda instants para o turno do oponente |
| 5b | Não usa boardwipe quando está ganhando |
| 6 | Valoriza criaturas utilitárias (hatebears/stax) |
| 7 | Não ataca sem valor ofensivo |
| 9 | Multi-block death check |
| 10a | Chump block defensivo (sobrevivência) |
| 10b | Limiares de vida recalibrados para 40 HP Commander |
| 12 | Early game restraint (turns 1-4) |
| 13a | Deathtouch blocker priority |
| 13b | Gang-block com criaturas expendáveis |
| 15 | Proteção de peças de alto valor |
| Debug | Logs de decisão visíveis no game log |
| Smart Skip F9 | Para auto-skip em remoção/boardwipe |

## Instalação

1. Baixe o arquivo `install-ai-patch.bat` na seção [Releases](../../releases/latest)
2. Dê duplo clique e siga as instruções
3. Reinicie o servidor XMage

## Para o servidor local (Diego)

Rode `build-and-deploy-ai.bat` após cada modificação no código.
