---
name: XMage AI Improvement Project
description: XMage 1.4.58 Commander AI — todas as sprints implementadas, estado atual, tópicos pendentes
type: project
originSessionId: da286637-024f-4a6a-bfa0-7c8817439f47
---
## Instalação

- XMage 1.4.58 em `C:\Users\diego\Desktop\XMage`
- Server distribuído pelo Grath (weekly builds upstream magefree/mage): https://grath.github.io
- Launcher: `C:\Users\diego\Desktop\XMage\XMageLauncher-0.3.8.jar`

## Ambiente de build

- Fonte: `C:\Users\diego\Desktop\XMage\mage-source` (branch: `diego-ai-improvements`)
- JDK 8 Temurin: `C:\Program Files\Eclipse Adoptium\jdk-8.0.482.8-hotspot`
- Maven 3.9.6: `C:\Users\diego\Desktop\XMage\apache-maven-3.9.6`

**Comando de build (bash):**
```bash
cd "C:/Users/diego/Desktop/XMage/mage-source"
"C:/Users/diego/Desktop/XMage/apache-maven-3.9.6/bin/mvn.cmd" install \
  -pl Mage.Server.Plugins/Mage.Player.AI,Mage.Server.Plugins/Mage.Player.AI.MA \
  -am -DskipTests
```

**Deploy dos 2 JARs:**
```bash
cp .../Mage.Player.AI/target/mage-player-ai.jar \
   xmage/mage-server/lib/mage-player-ai-1.4.58.jar
cp .../Mage.Player.AI.MA/target/mage-player-ai-ma.jar \
   xmage/mage-server/plugins/mage-player-ai-ma-1.4.58.jar
```

## Patches de performance (bytecode, incorporados no source)

- MAX_SIMULATED_NODES: 5000 → 15000
- minDepth floor: 4 → 5
- JVM: Xmx1G → Xmx4096m + UseG1GC

## Todas as melhorias implementadas ✅

### Sprint 1+8 — evaluatePlayerThreat() [`GameStateEvaluator2.java`]
Novo método para avaliar ameaça de cada jogador em multiplayer:
- Board presence (evaluatePermanent × 3)
- Ramp: cada fonte de mana além de 4 → +120 pts
- Cartas na mão × 50
- Vida >= 30 → +200 (ainda não foi foco)
- Vida <= 10 → -400 (não fazer bully no fraco)

### Sprint 2 — Atacar o jogador mais ameaçador [`ComputerPlayer6.java`]
`declareAttackers()`: ordena oponentes por ThreatScore antes do loop de ataque.

### Sprint 3 — Reservar bloqueador [`ComputerPlayer6.java`]
Se oponente com ThreatScore > 3000 e há mais de 1 safe attacker → retém maior P/T como bloqueador.

### Sprint 4 — Remoção ponderada por ameaça [`PossibleTargetsComparator.java` + `ComputerPlayer.java`]
- Score de permanentes de oponentes amplificado por threatScore/5000
- Removal opcional: skip permanentes com score < 800

### Sprint 5 — Timing de instants/flash [`InstantTimingOptimizer.java`] (NOVO ARQUIVO)
- **Rule 1:** Suprime instants/flash na própria main phase do bot (guarda para turno do oponente)
- **Rule 2:** Suprime habilidades ativadas com TapSourceCost/TapTargetCost durante turno do oponente fora de combate (evita tapar blockers à toa)
- Registrado no static initializer de `ComputerPlayer6`

### Sprint 5b — Boardwipe com vantagem [`BoardwipeOptimizer.java`] (NOVO ARQUIVO)
Suprime boardwipes quando board do bot >= board combinado dos oponentes × 1.5.
Registrado no static initializer de `ComputerPlayer6`.

### Sprint 6 — Valorização de criaturas utilitárias [`ArtificialScoringSystem.java`]
Split do abilityScore: 70% combat-scaled + 30% base fixo. Corrige subvalorização de hatebears/stax.

### Sprint 7 — Não atacar sem valor ofensivo [`ComputerPlayer6.java`]
Suprime ataque "safe mas inútil": sem trample/lifelink E não mata nenhum bloqueador.

### Sprint 9 — Multi-block death check [`ComputerPlayer6.java`]
Suprime ataque quando 2+ blockers podem matar o atacante E o trade de score é desfavorável.
Exceções: Trample, Indestructible, Deathtouch.

### Fix X=0 [`SimulatedPlayer2.java`]
Loop de variableManaCost agora começa em max(1, minX). Bot não considera X=0 como opção.

### Fix Floating Mana Score [`GameStateEvaluator2.java`]
Mana no pool = +100 pts por mana. Gastar {1}{B} em habilidade inútil (+3 pts) → -197 vs passar. Bot preserva mana para usos relevantes.

### Sprint 10b — Life Threshold Recalibration para Commander 40 HP [`GameStateEvaluator2.java`]
Limiares de vida em `evaluatePlayerThreat()` recalibrados para 40 HP:
- `vida >= 35 → +150` (intocado = ameaça; era `>= 30 → +200`)
- `vida <= 15 → -200` (sob pressão)
- `vida <= 8 → -400` (cumulativo: -600 total quando quase morto; era `<= 10 → -400`)

### Sprint 10a — Chump Block Defensivo [`ComputerPlayer6.java`]
Em `declareBlockers()`, após `blockWithGoodTrade2` (que só faz "boas trocas"), verifica se o dano não-bloqueado restante é letal. Se sim, atribui os blockers de menor valor disponíveis aos atacantes mais perigosos até sobreviver o turno.
Resolve a issue #13135 do upstream (aberta Dez 2024, nunca resolvida).

### Sprint 12 — Early Game Restraint [`ComputerPlayer6.java`]
Em `declareAttackers()`, nos turnos 1-4 suprime ataques com criaturas de power < 3 sem evasion (Flying). Bots não desperdiçam criaturas pequenas em chip damage inútil enquanto ainda estão desenvolvendo o board. Kill shots continuam funcionando normalmente.

### Sprint Debug — aiLog() + Chat Observability [`ComputerPlayer6.java`, `BoardwipeOptimizer.java`, `ComputerPlayer.java`]
Constante `AI_DEBUG_LOG = true` (desliga tudo com um flip). Método `aiLog(Game, String)` emite mensagens `[AI:NomeBot] ...` no chat da partida em **laranja** via `game.fireStatusEvent()` (não `informPlayers` — isso era preto e se misturava com eventos normais).

Mensagens com tags legíveis:
- `[ATTACK]` — threat order de todos oponentes no início de cada declaração de ataque
- `[HOLD]` — quando ataque é suprimido (sem valor ofensivo, gang-block desfavorável, early game restraint, proteção de peça valiosa, reserva de bloqueador)
- `[BLOCK]` — Deathtouch block, Gang-block expendable, Chump block (com scores e dano)
- `[BOARDWIPE]` — quando boardwipe é suprimido (board scores do bot vs oponentes, threshold necessário)
- `[REMOVAL]` — quando pula alvo fraco de remoção (score vs threshold)

### Sprint 13 — Deathtouch Blocker Priority + Multi-block Expendable [`ComputerPlayer6.java`]
**13a — Deathtouch priority:** Em `declareBlockers()`, após `blockWithGoodTrade2`, itera atacantes não bloqueados por score desc e atribui o menor (menos valioso) bloqueador com Deathtouch disponível. Só troca se attackerScore > dtScore. Um deathtouch mata qualquer atacante com 1 ponto de dano, tornando a troca eficiente independente do P/T.
**13b — Multi-block expendable:** Para cada atacante não bloqueado com score >= 1200, verifica se 2+ blockers "descartáveis" (score < atkScore × 0.40) podem gang-matar (power combinado >= toughness do atacante). Se sim, declara todos como bloqueadores. Skip em atacantes Indestructible. Trackers `coveredAttackerIds`/`usedBlockerIds` compartilhados entre todos os blocos de lógica de bloqueio.

### Sprint 15 — High-Value Piece Protection [`ComputerPlayer6.java`]
Em `declareAttackers()`, após Sprint 12, protege peças de alto valor (score >= 1200) de ataques em campos minados. Verifica se 2+ blockers expendáveis do oponente (score < atkScore × 0.40) têm poder combinado suficiente para matar o atacante. Se sim, suprime o ataque. Exceções: Trample, Indestructible, Deathtouch. Complementa Sprint 9 (que usa todos os blockers) com foco específico em peças valiosas.

## Arquivos modificados vs upstream

| Arquivo | Módulo | Sprint |
|---------|--------|--------|
| `score/GameStateEvaluator2.java` | Mage.Player.AI | Sprint 1+8, floating mana, Sprint 10b, Sprint 18 |
| `score/ArtificialScoringSystem.java` | Mage.Player.AI | Sprint 6 (+ comentários normalizados em 2026-05-20) |
| `ai/PossibleTargetsComparator.java` | Mage.Player.AI.MA | Sprint 4 |
| `ai/ComputerPlayer.java` | Mage.Player.AI | Sprint 4 |
| `ComputerPlayer6.java` | Mage.Player.AI.MA | Sprint 2, 3, 7, 9, 10a, 12, Debug, 13, 15, 16, Sprint 18 |
| `ComputerPlayer7.java` | Mage.Player.AI.MA | Sprint 18: clearTurnMemory() no end of turn |
| `memory/AiMemory.java` | Mage.Player.AI.MA | Sprint 18 (NOVO ARQUIVO) |
| `SimulatedPlayer2.java` | Mage.Player.AI.MA | Fix X=0 |
| `optimizers/impl/BoardwipeOptimizer.java` | Mage.Player.AI.MA | Sprint 5b (novo), Sprint Debug |
| `optimizers/impl/InstantTimingOptimizer.java` | Mage.Player.AI.MA | Sprint 5 (novo) |
| `HumanPlayer.java` | Mage.Player.Human | Smart Skip F9 |

## Tópicos pendentes de discussão

### Distribuição para amigos — IMPLEMENTADO ✅

Repo: `https://github.com/dinga-hub/Xmage-improved`

Os amigos instalam via **`XMageAIPatch.exe`** (C# compilado de `installer-src/XMageInstaller.cs`).
O instalador:
1. Detecta a pasta `mage-server` via registro + caminhos comuns
2. Detecta o nome versionado dos JARs (ex: `mage-player-ai-1.4.58.jar`)
3. Faz backup dos JARs existentes (`.backup`)
4. Baixa os 3 JARs de `https://github.com/dinga-hub/Xmage-improved/releases/latest/download/`
5. Valida tamanho mínimo (>10KB), restaura backup se falhar
6. Patcha `startServer.bat` com `-Xmx4096m -XX:+UseG1GC`

**Fluxo de publicação (Claude faz tudo, Diego só pede):**
Quando Diego pede "faz build, deploy e publica":
1. `mvn install` nos 3 módulos
2. Copia JARs para servidor local do Diego
3. Copia JARs para `Xmage-improved/jars/` + commit + push
4. Recria GitHub Release `v1.4.58-latest` com os 3 JARs via `gh` CLI

**Ferramentas:**
- `gh` CLI instalado em `C:\Program Files\GitHub CLI`, autenticado como `dinga-hub`
- Git configurado no repo `Xmage-improved` (user: Diego, email: diegolissoni@gmail.com)
- Diego **nunca** faz nada manualmente no git — Claude faz tudo

### Smart Skip / F9 Melhorado (IMPLEMENTADO ✅ — 2026-04-12)
Diego usa F9 (skip to my turn) e quer parar automaticamente quando um oponente lança spell que afeta seu board state.

**Arquitetura descoberta (sessão 2026-04-12):**

O F9 funciona **server-side** via flag `passedAllTurns` em `HumanPlayer.java` (extends `PlayerImpl`).

**Arquivo central:** `Mage.Server.Plugins/Mage.Player.Human/src/mage/player/human/HumanPlayer.java`

**Método chave:** `priority(Game game)` — linha ~1156

**Mecanismo de quickStop já existe** (linha ~1181-1195):
```java
boolean quickStop = false;
if (isGameUnderControl()) {
    // se foi atacado → quickStop = true (stop em DECLARE_ATTACKERS)
}
if (!quickStop && isGameUnderControl()) {
    if (passedAllTurns ...) { passWithManaPoolCheck(game); return false; } // skip
}
// se quickStop=true, cai no loop de interação normal
```

**Para parar em remoção direcionada:** adicionar ao bloco quickStop:
```java
// Itera game.getStack(), para cada stackObject de oponente:
// stackObject.getStackAbility().getTargets() → verifica se algum target
// é uma Permanent com controllerId == playerId
```

**Para boardwipe:** difícil pois são não-targetados. Opções:
- Whitelist de nomes (frágil)
- Checar tipo de efeito (precisaria acesso ao motor de regras no server)

**Duas abordagens de implementação:**
- **Modificar F9 diretamente:** mais simples, sem nova UI. F9 passa a ter smart behavior sempre.
- **Novo modo (ex: Shift+F9 ou F12):** precisa de novo `PlayerAction` enum, novo boolean no player, novo botão na UI (`GamePanel.java`) e novo keybinding.

Diego prefere novo modo se não for muito mais trabalhoso (decidir na próxima sessão).

**Arquivo modificado:** `Mage.Server.Plugins/Mage.Player.Human/src/mage/player/human/HumanPlayer.java`
- Adicionado bloco Smart Skip dentro do `if (isGameUnderControl())` da seção de quickStop em `priority()`
- Detecta: remoção direcionada a permanentes do jogador + boardwipes (DestroyAllEffect, ExileAllEffect, SacrificeAllEffect, ReturnToHandFromBattlefieldAllEffect)
- Requer deploy de **3 JARs** (não 2): inclui `mage-player-human-1.4.58.jar` em `plugins/`

**Build atualizado:**
```bash
cd "C:/Users/diego/Desktop/XMage/mage-source"
"C:/Users/diego/Desktop/XMage/apache-maven-3.9.6/bin/mvn.cmd" install \
  -pl Mage.Server.Plugins/Mage.Player.AI,Mage.Server.Plugins/Mage.Player.AI.MA,Mage.Server.Plugins/Mage.Player.Human \
  -am -DskipTests
```

**Deploy (3 JARs):**
```bash
cp .../Mage.Player.AI/target/mage-player-ai.jar           .../mage-server/lib/mage-player-ai-1.4.58.jar
cp .../Mage.Player.AI.MA/target/mage-player-ai-ma.jar     .../mage-server/plugins/mage-player-ai-ma-1.4.58.jar
cp .../Mage.Player.Human/target/mage-player-human.jar     .../mage-server/plugins/mage-player-human-1.4.58.jar
```

## Calibração — valores que podem precisar ajuste

- `DEFENDER_THRESHOLD = 3000` — quando reservar bloqueador
- `MIN_REMOVAL_TARGET_SCORE = 800` — threshold de remoção
- `THREAT_NORMALIZER = 5000` — escala do peso de ameaça
- `FLOATING_MANA_VALUE = 100` — custo de oportunidade por mana
- Ramp baseline = 4, peso = 120

## Sprint 18 — Mana Reservation (COMPLETA 2026-05-20)

Scoring bonus de mana destapada em `GameStateEvaluator2.java`:
- Classifica self-position: ARCHENEMY (>1.4× avg opp threat), LEADING (>avg), PARITY, TRAILING (<0.7×avg)
- Early game (ninguém com 4+ permanentes não-land) → sem reserva
- Trailing → sem reserva (precisa desenvolver, não guardar)
- Mass protection na mão + board forte → 400 pts/mana (sempre reserva)
- Caso geral: bonusPerMana por posição × min(untappedMana, cheapestInstantCMC)

**Detecção de mass protection** (Option C — effect-based):
- PRIMARY: `effect instanceof PhaseOutAllEffect` → true
- PRIMARY: `effect instanceof GainAbilityAllEffect` + `getText().contains("indestructible")` → true
- FALLBACK: lista de 3 nomes ("Eerie Interlude", "Semester's End", "Scapegoat") para exile-and-return

**Validação pendente**: Diego jogar 1 partida com `AI_DEBUG_LOG=true`, verificar logs `[RESERVE]`.

## Padrão obrigatório de comentários (estabelecido 2026-05-20)

Toda constante numérica deve ter WHY: o que o valor significa em termos de Magic, por que esse número, o que muda se aumentar/diminuir. Toda decisão de arquitetura não-óbvia (lista hardcoded, abordagem de detecção) deve ter WHY e alternativas descartadas. **Qualquer decisão dessas deve ser discutida com Diego antes de implementar.**

## Achado: cartas modais — chooseMode() sempre pega o primeiro (2026-05-20)

`ComputerPlayer.java:941` — `chooseMode()` retorna sempre o primeiro modo válido sem scoring. Causa: Diego observou que o bot sempre faz a mesma escolha (ex: Braids end step). Sprint 28 foi adicionada ao roadmap para corrigir via `ModeScorer` heurístico por classe de Effect.

## Backlog — próximas sprints planejadas

Pesquisa realizada em 2026-04-13: issues abertas do upstream, PRs rejeitados, feedback da comunidade.
**Contexto importante:** JayDi85 (maintainer) rejeitou explicitamente heurísticas estratégicas no PR #14384 — nosso trabalho é exclusivamente para uso local.

### Sprint 11 — Commander Damage Awareness (alta prioridade, próxima)
**Problema:** bot ignora que 21 de dano de comandante mata. Não prioriza atacar com o comandante nem rastreia dano acumulado.
- `GameStateEvaluator2.java`: em `evaluatePlayerThreat()`, somar commander damage recebido por cada oponente: `>= 16 → +600`, `>= 11 → +300`
- `ComputerPlayer6.java`: em `declareAttackers()`, se oponente tem >= 11 de commander damage do nosso comandante, priorizar atacar com ele

### Sprint 13 — ✅ IMPLEMENTADA (ver acima)

### Sprint 15 — ✅ IMPLEMENTADA (ver acima)

### Sprint 14 — Archenemy Detection (baixa prioridade, alta complexidade)
**Problema:** bots não coordenam esforços. Líder da mesa raramente sofre foco.
- `ComputerPlayer6.java` + `PossibleTargetsComparator.java`: se um oponente tem ThreatScore >= 1.8× a média dos outros, amplificar ainda mais seu peso no sorting de atacantes e remoção

### Calibração pendente (pós-Sprint 10)
- Limiar vida chump block: `10` HP absoluto (Sprint 10a já usa `player.getLife()`)
- Commander damage thresholds: `>= 11 → +300`, `>= 16 → +600` (Sprint 11)

## Workflow de sync com upstream (Grath weekly)

```bash
cd "C:/Users/diego/Desktop/XMage/mage-source"
git fetch origin
git rebase origin/master
# compilar e deployar
```
