# XMage-improved

XMage Commander AI melhorado para partidas multiplayer locais.

## Instalação

### Opção 1 — Instalador automático (recomendado)

1. Baixe o [`XMageAIPatch.exe`](../../releases/latest/download/XMageAIPatch.exe)
2. Feche o servidor XMage
3. Dê duplo clique e siga as instruções
4. Reinicie o servidor XMage

> Detecta automaticamente a pasta do XMage, o nome correto dos JARs e aplica o patch de memória JVM.

### Opção 2 — Manual

1. Baixe os 3 JARs da aba [Releases](../../releases/latest)
2. Substitua na pasta do XMage:
   - `mage-player-ai.jar` → `mage-server\lib\mage-player-ai-*.jar`
   - `mage-player-ai-ma.jar` → `mage-server\plugins\mage-player-ai-ma-*.jar`
   - `mage-player-human.jar` → `mage-server\plugins\mage-player-human-*.jar`

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
| Smart Skip F9 | Para auto-skip em remoção/boardwipe no stack (inclui cartas customizadas como Blood Money) |
