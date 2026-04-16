# XMage-improved

XMage Commander AI melhorado para partidas multiplayer locais.

## Instalação

1. Baixe o [`install-ai-patch.bat`](../../raw/main/install-ai-patch.bat)
2. Dê duplo clique e siga as instruções
3. Reinicie o servidor XMage

> Requer Python (para o patch de memória JVM). Se não tiver, instale em python.org — ou ajuste o `-Xmx` no `startServer.bat` manualmente para `4096m`.

## O que muda

| Sprint | Descrição |
|--------|-----------|
| 1+8 | `evaluatePlayerThreat()`: board presence, ramp, mão, vida |
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
| Smart Skip F9 | Para auto-skip em remoção/boardwipe no stack |
