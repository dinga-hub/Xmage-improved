# XMage-improved

XMage Commander AI melhorado para partidas multiplayer locais.

## Desenvolvimento / Cursor (`workspace-meta/`)

Para **não perder** regras de agente, `CLAUDE.md`, notas e scripts sem versionar o `mage-source` gigante, existe a pasta **[`workspace-meta/`](workspace-meta/)** — espelho leve com `AGENTS.md`, `CLAUDE.md`, `memory/`, `.cursor/rules/` e `.bat`. Ver [`workspace-meta/README.md`](workspace-meta/README.md).

## Como instalar (amigos)

### Opção 1 — Instalador automático (recomendado)

1. Baixe o [`XMageAIPatch.exe`](../../releases/latest/download/XMageAIPatch.exe)
2. Feche o servidor XMage
3. Dê duplo clique e siga as instruções
4. Reinicie o servidor XMage

> Detecta a pasta do XMage, troca os 3 JARs de IA, **injeta `GameChangerRegistry` no `mage-*.jar` core** (necessário após update do Grath) e aplica o patch de memória JVM.
>
> **Sempre rode de novo depois de um update oficial** — o launcher sobrescreve `lib\` / `plugins\`.

### Opção 2 — Manual (3 JARs + classe)

1. Baixe da [release latest](../../releases/latest):
   - `mage-player-ai.jar` → `mage-server\lib\mage-player-ai-*.jar` (nome versionado que já existir)
   - `mage-player-ai-ma.jar` → `mage-server\plugins\mage-player-ai-ma-*.jar`
   - `mage-player-human.jar` → `mage-server\plugins\mage-player-human-*.jar`
   - `GameChangerRegistry.class` — injete no core com:
     `jar uf lib\mage-1.4.60.jar mage/cards/repository/GameChangerRegistry.class`
     (coloque o `.class` no path certo antes, ou use o `.exe`)

2. Reinicie o servidor XMage

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
