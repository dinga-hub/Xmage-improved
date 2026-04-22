# XMage-improved

XMage Commander AI melhorado para partidas multiplayer locais.

## Como instalar (amigos)

### Opção 1 — Instalador automático (recomendado)

1. Baixe o [`XMageAIPatch.exe`](../../releases/latest/download/XMageAIPatch.exe)
2. Feche o servidor XMage
3. Dê duplo clique e siga as instruções
4. Reinicie o servidor XMage

> Detecta automaticamente a pasta do XMage, o nome correto dos JARs e aplica o patch de memória JVM.

### Opção 2 — Manual (3 arquivos)

1. Baixe os 3 JARs da pasta [`jars/`](jars/):
   - [`mage-player-ai.jar`](jars/mage-player-ai.jar)
   - [`mage-player-ai-ma.jar`](jars/mage-player-ai-ma.jar)
   - [`mage-player-human.jar`](jars/mage-player-human.jar)

2. Substitua na pasta do XMage (feche o servidor antes):
   - `mage-player-ai.jar` → `mage-server\lib\mage-player-ai-1.4.58.jar`
   - `mage-player-ai-ma.jar` → `mage-server\plugins\mage-player-ai-ma-1.4.58.jar`
   - `mage-player-human.jar` → `mage-server\plugins\mage-player-human-1.4.58.jar`

3. Reinicie o servidor XMage

## O que muda nos bots

Os bots mostram as decisões em **laranja** no chat do jogo:

| Tag | Significado |
|-----|-------------|
| `[ATTACK]` | Ordem de prioridade de ataque por threat score |
| `[HOLD]` | Por que segurou uma criatura em vez de atacar |
| `[BLOCK]` | Por que e como decidiu bloquear |
| `[BOARDWIPE]` | Por que segurou um boardwipe |
| `[REMOVAL]` | Por que pulou um alvo fraco de remoção |

## Sprints implementadas

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
