# XMage AI — Roadmap Forge-XMage Hybrid

**Documento mestre. Atualizar este arquivo ao final de cada sprint.**

**Versão**: 1.0 — criado em 2026-05-19
**Status global**: Sprint 18 concluída (mana reservation — context-sensitive scoring). Próxima: Sprint 19 (Reactive Instant Strategy — counter + protection timing).
**Prioridade**: Este é o foco principal do projeto a partir desta data.

---

## 1. Como usar este documento

### Para o usuário (Diego, não-dev)
- Este é o plano de longo prazo. Você não precisa lê-lo todo.
- Quando começar uma nova sessão com Claude Code ou Cursor, peça: **"vamos avançar a próxima sprint do AI_ROADMAP.md"**.
- A AI saberá ler a sprint corrente, executar e atualizar o tracking.
- Use o tracking table (seção 11) para ver progresso.

### Para a AI (Claude / Cursor / qualquer agente)
- Leia esta seção 1 + seção 2 (Estado atual) + a sprint corrente. Não precisa ler tudo.
- Cada sprint é auto-contida: tem objetivo, arquivos, design, API translation, validação.
- Siga o **Execution Playbook** (seção 10) para o workflow padrão.
- Ao terminar uma sprint: atualize seção 11 + adicione entrada em `memory/project_xmage.md`.
- **Nunca modifique este documento estruturalmente sem aprovação explícita** — só atualizar status/tracking.

---

## 2. Estado atual (snapshot 2026-05-19)

### Sprints concluídas (1–17)
Ver tabela completa em [CLAUDE.md](CLAUDE.md). Resumo:

- **Sprints 1+8**: `evaluatePlayerThreat` multiplayer-aware (board + ramp + hand + life)
- **Sprint 2**: `declareAttackers` ordena oponentes por threat score
- **Sprint 3**: retém bloqueador quando oponente ameaçador
- **Sprint 4**: removal ponderado por `threatScore/5000`
- **Sprint 5**: `InstantTimingOptimizer` para timing de instants
- **Sprint 5b**: `BoardwipeOptimizer` suprime wipe em vantagem
- **Sprint 6**: scoring utilitário (70% combat + 30% base)
- **Sprint 7**: suprime ataque inútil
- **Sprint 9**: multi-block death check
- **Sprint 12**: restraint early-game
- **Sprint 15**: proteção de high-value pieces
- **Sprint 16**: reserva chumps cross-opponent
- **Sprint 17**: review coordenada do swarm (incomingDamageNextRotation + swarmAttackValue)

### Arquivos principais já tocados
| Arquivo | Módulo |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | mad-bot, alvo de quase tudo |
| `Mage.Player.AI.MA/src/mage/player/ai/score/GameStateEvaluator2.java` | evaluator (TODO: multi-player) |
| `Mage.Player.AI.MA/src/mage/player/ai/score/ArtificialScoringSystem.java` | scoring utilitário |
| `Mage.Player.AI.MA/src/mage/player/ai/PossibleTargetsComparator.java` | target sort |
| `Mage.Player.AI/src/mage/player/ai/ComputerPlayer.java` | base AI |
| `Mage.Player.AI.MA/src/mage/player/ai/SimulatedPlayer2.java` | fix X=0 |
| `Mage.Player.Human/src/mage/player/human/HumanPlayer.java` | Smart Skip |
| `Mage.Player.AI.MA/src/mage/player/ai/ma/optimizers/impl/InstantTimingOptimizer.java` | novo |
| `Mage.Player.AI.MA/src/mage/player/ai/ma/optimizers/impl/BoardwipeOptimizer.java` | novo |

### Estado de debug
- `AI_DEBUG_LOG = true` em `ComputerPlayer6.java` (ligar/desligar conforme uso local vs release).
- Desligar antes de publicar release para amigos via `XMageAIPatch.exe`.

---

## 3. Visão do híbrido Forge-XMage

### O que o XMage tem hoje que vamos PRESERVAR
1. Minimax + α-β em `ComputerPlayer6/7` (mais profundo que o 1-ply do Forge)
2. Sprint 17 coordination (Forge não tem)
3. `evaluatePlayerThreat` multiplayer-aware (Sprint 1+8)
4. `isExpendableChump` cross-opponent (Sprint 16)
5. Optimizers plugáveis (`BoardwipeOptimizer`, `InstantTimingOptimizer`)
6. Smart Skip F5/F9/F11 no HumanPlayer

### O que vamos IMPORTAR como design pattern do Forge
1. **Reserva de mana em 4 buckets** (Sprint 18)
2. **Categorização de spells na stack** (Sprint 19)
3. **`useRemovalNow` math** para decidir gastar removal (Sprint 20)
4. **Block pipeline em 6 fases** (Sprint 21)
5. **`ComputerUtilCombat` utility class** (Sprint 22)
6. **`HOLD_LAND_DROP_FOR_MAIN2` e sequencing** (Sprint 23)
7. **Per-card AI registry** para top-20 cartas Commander (Sprint 24+)
8. **Profile system (`AiProps`)** (Sprint 25)
9. **Threat tagging** para engine pieces (Sprint 26)
10. **BoardWipeWatcher** para Smart Skip avançado (Sprint 27)

### O que vamos PULAR
- Avaliador 1v1 (XMage tem o mesmo problema, mas evaluatePlayerThreat compensa)
- 1-ply `SpellAbilityPicker` (XMage tem minimax melhor)
- Fallbacks "pega primeiro / random boolean"
- Good-block algorithm capado em 3 criaturas
- Política completa (archenemy/archetype) — Forge também não tem, escopo gigante
- Sideboarding (raro em Commander casual)
- Format-specific (Momir/Mojhosto)

### Princípio de implementação: clean-room port
1. Ler arquivo Forge (via URL raw do GitHub)
2. **Entender o algoritmo** — não copiar texto literal
3. Reescrever usando APIs XMage
4. **Preservar constantes/thresholds do Forge** (anos de tuning, não chutar)
5. Adicionar logging via `aiLog()` no padrão Sprint 17

### Licenciamento
- Forge é GPL v3 (forte copyleft); XMage é MIT (permissivo).
- **Clean-room**: estamos reimplementando design baseado em leitura do Forge. Não copiamos texto.
- Risco prático: muito baixo. Se quiser zerar risco para distribuição em `Xmage-improved`, podemos relicenciar o repo como GPL no futuro.

---

## 4. Arquitetura de infraestrutura compartilhada

Três classes/módulos novos sustentam várias sprints. Construí-los na ordem certa importa.

### 4.1. `AiMemory` (criado em Sprint 18)
**Pacote**: `mage.player.ai.memory`
**Arquivo novo**: `Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/memory/AiMemory.java`

```java
public class AiMemory {
    public enum MemoryCategory {
        HELD_MANA_FOR_OPP_DECLBLK,    // mana reservada para resposta em combate oponente
        HELD_MANA_FOR_NEXT_SPELL,     // mana reservada para counterspell/instant
        HELD_MANA_FOR_MAIN2,          // mana reservada para main 2
        HELD_MANA_FOR_DECLBLK,        // mana reservada para combat trick em ataque próprio
        MANDATORY_ATTACKERS,          // criaturas que TÊM que atacar este turno
        TRICK_ATTACKERS,              // atacantes que dependem de pump em combate
        CHOSEN_FOG_EFFECT,            // fog já escolhido neste turno
        REVEALED_OPP_CARDS,           // cartas reveladas pelo oponente
        ATTACHED_THIS_TURN,           // equipamentos colocados neste turno
        ANIMATED_THIS_TURN,           // lands animadas neste turno
        BOUNCED_THIS_TURN             // bounces já usados
    }

    private final Map<MemoryCategory, Set<UUID>> categories;

    public void remember(MemoryCategory c, UUID cardId);
    public boolean isRemembered(MemoryCategory c, UUID cardId);
    public Set<UUID> get(MemoryCategory c);
    public void forget(MemoryCategory c);
    public void clearAtEndOfTurn();  // chamado em endTurn hook
}
```

**Ciclo de vida**: por jogador, reset no final do turno do jogador.
**Quem usa**: Sprint 18 (mana reservation), Sprint 19 (counter), Sprint 23 (sequencing), Sprint 27 (fog tracking).

### 4.2. `CombatPredictor` (criado em Sprint 22)
**Pacote**: `mage.player.ai.combat`
**Arquivo novo**: `Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/combat/CombatPredictor.java`

Extrai do `ComputerPlayer6.java` (~1700 linhas) e do `CombatUtil.java` toda a matemática de combate em métodos estáticos reutilizáveis. **Referência**: `forge-ai/src/main/java/forge/ai/ComputerUtilCombat.java`.

Métodos principais:
```java
public static int damageIfUnblocked(Permanent attacker, Player defender, Game game);
public static int sumDamageIfUnblocked(List<Permanent> attackers, Player defender, Game game);
public static boolean lifeInDanger(UUID playerId, Game game, int lookahead);
public static boolean lifeInSeriousDanger(UUID playerId, Game game);
public static int getEnoughDamageToKill(Permanent target, Permanent dealer, boolean withDeathtouch);
public static int predictDamageTo(Permanent target, int damage, Permanent source, Game game);
public static boolean dealsFirstStrikeDamage(Permanent p);
public static int getTotalFirstStrikeBlockPower(List<Permanent> blockers);
```

**Quem usa**: Sprint 21 (block pipeline), Sprint 18 (mana threat), e todas as futuras decisões de combate.

### 4.3. `AiProfile` (criado em Sprint 25)
**Pacote**: `mage.player.ai.profile`
**Arquivo novo**: `Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/profile/AiProfile.java`

Container de constantes configuráveis (~150 knobs análogos a `AiProps.java`). Cada `ComputerPlayer6` recebe um perfil no construtor.

```java
public class AiProfile {
    // Existing constants migrate here
    public int DEFENDER_THRESHOLD = 3000;
    public int HIGH_VALUE_THRESHOLD = 1200;
    // ... etc

    // New
    public int RESERVE_MANA_FOR_MAIN2_CHANCE = 50;
    public int MIN_SPELL_CMC_TO_COUNTER = 3;
    public boolean ALWAYS_COUNTER_WIPE = true;
    public boolean ALWAYS_COUNTER_REMOVAL = false;
    public int PLAY_AGGRO = 3; // 0-6 scale

    public static AiProfile DEFAULT = new AiProfile();
    public static AiProfile AGGRO = aggressivePreset();
    public static AiProfile CONTROL = controlPreset();
    public static AiProfile MIDRANGE = midrangePreset();
}
```

**Quem usa**: Sprint 25 e todas as subsequentes.

---

## 4.5. Sprint 0 — Foundation Research (PRÓXIMA AÇÃO)

### Objetivo
Coletar sabedoria Commander estruturada de fontes reais (Command Zone, Game Knights, cEDH content, etc.) **antes** de codar qualquer sprint nova. A base coletada vai informar o briefing Etapa 0 de todas as sprints subsequentes.

### Por que primeira
- Forge codificou heurísticas de "desenvolvedor que joga Magic" há ~10+ anos. A comunidade Commander evoluiu muito desde então.
- LLMs (especialmente GPT Pro / Gemini com Deep Research) conseguem extrair regras estruturadas de conteúdo MTG em prosa.
- Aplicar essas regras no XMage **nos coloca à frente do Forge** em áreas onde a sabedoria moderna não foi codificada por ninguém.

### Por que NÃO é uma sprint de código
Nenhum arquivo Java é tocado. Output é uma base estruturada em `research/commander_wisdom/` que **alimenta** todas as sprints futuras.

### Tarefas

**Tarefa 0.1 — Diego dispara pesquisas externas** (assíncrono, ~1-2 horas paralelas)
1. Abrir `research/external_research_prompts.md`
2. Para cada um dos 7 prompts:
   - Abrir nova conversa em GPT Pro ou Gemini Deep Research
   - Colar prompt
   - Aguardar resposta estruturada (≥15-25 regras por tópico)
   - Salvar resposta em `research/commander_wisdom/[N]_[topico].md`
3. Pode fazer em paralelo (7 conversas simultâneas em browser/web)

**Tarefa 0.2 — Claude estrutura e cross-references** (1 sessão)
1. Ler todos os arquivos populados em `research/commander_wisdom/`
2. Identificar:
   - Regras que **alinham** com sprints 18-27 já planejadas → adicionar referência cruzada
   - Regras **conflitantes entre fontes** → marcar para Diego decidir
   - Regras **novas** que justificam sprints adicionais → propor a Diego
3. Atualizar `AI_ROADMAP.md` se necessário (adicionar sprints novas ou refinar existentes)
4. Atualizar tracking table (seção 17)

### Validação
- Diego revê uma amostra das regras coletadas, valida fontes
- Conflitos são discutidos antes de continuar
- Resultado: 100-150 regras estruturadas distribuídas em 7 tópicos

### Modelo recomendado
- Tarefa 0.1: GPT Pro / Gemini (não Claude — Deep Research deles é melhor pra busca web ampla)
- Tarefa 0.2: Claude Sonnet 4.6 (leitura + estruturação, sem código)

### Dependências
- Nenhuma. **Primeira ação da próxima sessão.**

### Saída esperada
```
research/commander_wisdom/
├── 01_threat_assessment.md
├── 02_mana_management.md
├── 03_combat_decisions.md
├── 04_removal_strategy.md
├── 05_counter_strategy.md
├── 06_sequencing_curve.md
└── 07_politics_multiplayer.md
```

Mais (possivelmente) revisões em `AI_ROADMAP.md` baseadas em descobertas.

---

## 5. Sprint 18 — Mana Reservation Memory

### Objetivo
Bot deixa de tapar toda a mana e passa a guardar fontes específicas para responder no turno do oponente (counter, removal instant-speed, fog).

### Melhoria visível ao usuário
"O bot tem Counterspell e mana flutuando, então não baixa criatura na main 2 — fica esperando." Mais visível: bot reage a wipes/removals do oponente.

### Forge reference
- **Principal**: `forge-ai/src/main/java/forge/ai/AiCardMemory.java`
  URL: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiCardMemory.java
- **Uso prático**: `AiController.java` busca `RESERVE_MANA_FOR_*` antes de gastar mana
- **Profile knob**: `AiProps.RESERVE_MANA_FOR_MAIN2_CHANCE`

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/memory/AiMemory.java` | **CRIAR** — classe nova (4.1) |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | adicionar campo `AiMemory memory`, init no construtor, reset em `endOfTurn` |
| `Mage.Player.AI/src/mage/player/ai/ComputerPlayer.java` | hook em `getAvailableManaInPool()` ou similar para excluir mana reservada |

### Design (algoritmo)

**Quando reservar?** (entrada na main 2)
1. Scan hand para spells flash/instant relevantes:
   - `Counterspell`-like: custom CMC, `Instant` + `target spell`
   - Removal instant: `Instant` + dano/destroy
   - Fog-like: `Instant` + prevenção
   - Combat trick: `Instant` + pump em criatura
2. Se encontrar pelo menos 1, computar `maxCMC` entre os candidatos
3. Reservar `maxCMC` mana em `HELD_MANA_FOR_NEXT_SPELL`
   - Marca os IDs das fontes de mana específicas (lands/rocks) como reservadas
4. Probabilidade: `RESERVE_MANA_FOR_NEXT_SPELL_CHANCE = 70` (não 100% — varia)

**Como bloquear o gasto?**
- Override `getAvailableMana()` (ou método similar) para retornar `total - reservedMana`
- Ou interceptar em `canPayCost` no minimax simulation

**Quando liberar?**
- Final do turno do jogador → `memory.clearAtEndOfTurn()`
- Quando a spell que foi reservada é jogada (não dá pra detectar perfeitamente, ok aceitar leak)

### API translation map

| Forge call | XMage equivalent |
|---|---|
| `ai.getCardsIn(ZoneType.Hand)` | `game.getPlayer(playerId).getHand().getCards(game)` |
| `card.isInstant()` | `card.isInstant(game)` |
| `card.hasKeyword(Keyword.FLASH)` | `card.getAbilities().containsKey(FlashAbility.getInstance().getId())` |
| `ai.getManaPool().getAmount()` | `game.getPlayer(playerId).getManaPool().getMana()` |
| `AiCardMemory.rememberCard(ai, card, MemorySet.HELD_MANA_FOR_NEXT_SPELL)` | `memory.remember(HELD_MANA_FOR_NEXT_SPELL, card.getId())` |

### Constantes a importar (do AiProps do Forge)
```java
public int RESERVE_MANA_FOR_MAIN2_CHANCE = 50;        // Forge default
public int RESERVE_MANA_FOR_NEXT_SPELL_CHANCE = 70;   // Forge default
public int RESERVE_MANA_FOR_COMBAT_TRICK_CHANCE = 60; // Forge default
public int MIN_MANA_LEFT_FOR_TRICK = 2;
```

### Validação
1. Build limpo: `build-and-deploy-ai.bat`
2. Cenário manual: bot tem `Counterspell` na mão, 4 mana, board sem ameaça imediata. Joga rampa na main 1, passa turno com 2+ mana destapada.
3. Log esperado: `[AI:Bot] [RESERVE] Holding 2 mana for Counterspell (HELD_MANA_FOR_NEXT_SPELL).`
4. Regressão: bot ainda joga criaturas grandes quando não tem instants relevantes.

### Modelo recomendado por fase
| Fase | Modelo |
|---|---|
| Pesquisa Forge (agente Explore) | Haiku 4.5 ou Sonnet 4.6 |
| Implementar `AiMemory` (mecânica) | Sonnet 4.6 |
| Implementar hook em mana payment (lógica delicada) | **Opus 4.7** |
| Validação e calibração de constantes | Sonnet 4.6 |

### Dependências
- Nenhuma (sprint 18 é fundação)

### Pause points
- ⏸️ Após criar `AiMemory`: troque para Opus para o hook de mana payment
- ⏸️ Após hook funcionar: troque para Sonnet para validação

---

## 6. Sprint 19 — Counter Strategy Categorization

### Objetivo
Bot decide quando counterar baseado em **categoria** do spell na stack, não no outcome genérico do minimax.

### Melhoria visível ao usuário
Bot azul guarda `Counterspell` para wipes/win conditions ao invés de contrar Lightning Bolt random.

### Forge reference
- **Principal**: `forge-ai/src/main/java/forge/ai/ability/CounterAi.java`
- **Profile knobs em**: `forge-ai/src/main/java/forge/ai/AiProps.java`:
  - `ALWAYS_COUNTER_DAMAGE`, `ALWAYS_COUNTER_REMOVAL`, `ALWAYS_COUNTER_PUMP`, `ALWAYS_COUNTER_AURAS`, `ALWAYS_COUNTER_0_CMC_MANA`, `ALWAYS_COUNTER_OTHER_COUNTERS`
  - `MIN_SPELL_CMC_TO_COUNTER`, `CHANCE_TO_COUNTER_CMC_X`
  - `DONT_EVAL_KILLSPELLS_ON_STACK_WITH_PERMISSION`

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/stack/StackThreatClassifier.java` | **CRIAR** — classifica spell na stack |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | hook em decisão de counter |
| `Mage.Player.AI.MA/src/mage/player/ai/profile/AiProfile.java` | adicionar profile knobs |

### Design

**`StackThreatClassifier.classify(StackObject)` retorna**:
```java
public enum SpellCategory {
    BOARDWIPE,        // mata 3+ criaturas
    TARGETED_REMOVAL, // destruição/exile alvo único
    DAMAGE_SPELL,     // dano direto
    PUMP,             // buff em criatura
    DRAW_ENGINE,      // Rhystic Study, Smothering Tithe
    RAMP,             // Cultivate, Rampant Growth
    MANA_ROCK,        // Sol Ring, Mana Crypt
    TUTOR,            // Demonic Tutor
    EXTRA_TURN,       // Time Walk
    COMBO_PIECE,      // peça conhecida de combo (lista hardcoded)
    COUNTER,          // Counterspell
    AURA,             // Lignify, Imprisoned in the Moon
    UTILITY,          // outros
    UNKNOWN
}
```

**Lógica de classificação** (pattern matching em Effects e CardName):
- Boardwipe: `DestroyAllEffect` + filtro `creatures` OU CardName em lista (`Wrath of God`, `Damnation`, `Toxic Deluge`, `Cyclonic Rift overload`, etc.)
- Targeted removal: `DestroyTargetEffect`/`ExileTargetEffect` com target em criatura/permanente
- Etc.

**Decisão de counter** (dado um Counterspell na mão e spell na stack):
```java
SpellCategory cat = StackThreatClassifier.classify(stackObject);
if (profile.ALWAYS_COUNTER_WIPE && cat == BOARDWIPE) return true;
if (profile.ALWAYS_COUNTER_EXTRA_TURN && cat == EXTRA_TURN) return true;
if (cat == TARGETED_REMOVAL && targetIsMyEnginePiece) return true;
if (stackObject.getManaValue() < profile.MIN_SPELL_CMC_TO_COUNTER) return false;
// fall through to existing minimax logic
```

### Constantes a importar
```java
public boolean ALWAYS_COUNTER_BOARDWIPE = true;
public boolean ALWAYS_COUNTER_EXTRA_TURN = true;
public boolean ALWAYS_COUNTER_REMOVAL = false; // depende do alvo
public boolean ALWAYS_COUNTER_DRAW_ENGINE = true;
public boolean ALWAYS_COUNTER_TUTOR = true;
public int MIN_SPELL_CMC_TO_COUNTER = 3;
public int CHANCE_TO_COUNTER_CMC_3 = 40;
public int CHANCE_TO_COUNTER_CMC_4 = 60;
public int CHANCE_TO_COUNTER_CMC_5_PLUS = 80;
```

### Validação
- Bot com Counterspell + mana reservada (Sprint 18) deixa Lightning Bolt resolver e contra Wrath of God.
- Log: `[COUNTER] Counterspell vs Wrath of God (BOARDWIPE, ALWAYS_COUNTER)`.

### Dependências
- **Sprint 18 OBRIGATÓRIA** (sem mana reservada o counter nunca dispara)

---

## 7. Sprint 20 — Removal Timing Math

### Objetivo
Decide se deve gastar removal agora ou guardar para alvo melhor.

### Melhoria visível
Bot não gasta Swords to Plowshares na primeira criatura média; espera o comandante ou engine piece chegar.

### Forge reference
- **Principal**: `forge-ai/src/main/java/forge/ai/ComputerUtilCard.java` método `useRemovalNow` (~L880–967)

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/removal/RemovalTimingEvaluator.java` | **CRIAR** |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | integrar com `useRemoval` ou ponto de decisão equivalente |

### Design

**Função principal**:
```java
public static boolean shouldUseRemovalNow(
    Permanent target,
    Card removal,
    Game game,
    UUID aiPlayerId
);
```

**Algoritmo** (parametrizado por categoria do target):

```
threatScore = computeThreatScore(target, game)  // reusa Sprint 4
tempoCost = removal.getManaValue() * profile.TEMPO_COST_PER_CMC
futureThreatExpectation = estimateFutureThreat(game, aiPlayerId)
// expectation = "se eu esperar 1 turno, qual o melhor alvo provável?"
// rough proxy: opp cards in hand * avg threat value

if (threatScore >= futureThreatExpectation * 0.8)
    return true; // ameaça atual é boa o bastante
if (threatScore >= profile.REMOVAL_MIN_TARGET_SCORE && lifeInDanger(game, aiPlayerId))
    return true;
return false; // guarda
```

**Fórmulas por tipo de target** (do Forge):
- Criatura: `damageRatio = power / opp.maxLife`
- Planeswalker: `1.0` (sempre alta prioridade)
- Artifact/Enchantment com efeito contínuo: `continuous = 0.8`
- Buff em criatura: `X * creatureCount`
- Mana rock: `0.5 * CMC / oppLandCount`

### Constantes
```java
public int REMOVAL_MIN_TARGET_SCORE = 800; // já existe!
public int TEMPO_COST_PER_CMC = 100;
public double REMOVAL_THRESHOLD = 0.8;
public int FUTURE_THREAT_PER_CARD_IN_HAND = 300;
```

### Dependências
- Sprint 4 (threat scoring) — JÁ FEITO
- Recomendado: Sprint 22 (`CombatPredictor`) para `lifeInDanger`, mas dá pra inline temporário

---

## 8. Sprint 21 — Block Pipeline em 6 Fases

### Objetivo
Refatorar `declareBlockers` em pipeline explícito (espelho da Sprint 17 para ataque).

### Melhoria visível
Bot bloqueia melhor: chump correto, gang block quando vale, protege planeswalker.

### Forge reference
- **Principal**: `forge-ai/src/main/java/forge/ai/AiBlockController.java`
  Pipeline em L970–1070

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | refatorar método `declareBlockers` (linha ~1196 atual) |
| `Mage.Player.AI.MA/src/mage/player/ai/combat/BlockPipeline.java` | **CRIAR** |
| `Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/util/CombatUtil.java` | possíveis ajustes |

### Design

**Pipeline (ordem de execução)**:

```
1. GOOD BLOCKS
   - Bloqueador mata atacante sem morrer
   - Exemplo: 4/4 bloqueia 3/3 → mata, sobrevive

2. GANG BLOCKS LETAIS
   - 2+ bloqueadores matam atacante, perdas < ganho
   - Exemplo: 2/2 + 2/2 bloqueiam 3/3 → mata atacante (5 dano), só 1 dos 2/2 morre

3. TRADE BLOCKS
   - P/T equivalente, ambos morrem
   - Só executa se attackerScore >= blockerScore

4. CHUMP BLOCKS (life-saving)
   - Se lifeInSeriousDanger → bloqueia para reduzir dano fatal
   - Prioriza chumps expendable (Sprint 16)

5. TRAMPLE REINFORCEMENT
   - Para atacantes com trample já bloqueados, soma toughness para absorver

6. NON-LETHAL GANG (último recurso)
   - Gang block sem matar, mas reduz dano significativamente
```

**Hooks especiais**:
- **Commander-lethal bypass** (se Sprint Commander damage for adicionada futuramente): atacante comandante prestes a matar por 21 → prioridade máxima
- **Planeswalker protection**: se atacante mira PW próprio com loyalty baixa → chump preventivo

### Constantes
```java
public int CHUMP_LIFE_THRESHOLD = 10; // se vida <= isso, chump permitido
public int GANG_BLOCK_MAX_LOSS_RATIO = 70; // máx 70% do valor do atacante em perdas
public boolean PROTECT_PLANESWALKERS = true;
```

### Validação
- Cenário: bot tem 5 HP, oponente ataca com 7/7. Bot tem 4 chumps. Deve bloquear com 1 chump (lifeInSeriousDanger).
- Cenário: bot tem PW com 3 loyalty. Atacante 3/3. Deve chump preventivo.

### Dependências
- Sprint 22 (`CombatPredictor`) recomendada antes desta — `lifeInDanger` etc.

---

## 9. Sprint 22 — `CombatPredictor` Utility Class

### Objetivo
Extrair toda matemática de combate do `ComputerPlayer6.java` para classe utilitária reutilizável.

### Melhoria visível
Nenhuma direta. **Mas habilita código mais limpo em todas as sprints futuras.**

### Forge reference
- **Principal**: `forge-ai/src/main/java/forge/ai/ComputerUtilCombat.java` (1700+ linhas, ~50 métodos)

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/combat/CombatPredictor.java` | **CRIAR** |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | substituir cálculos inline por chamadas ao predictor |

### Design

**Métodos a portar do Forge (prioridade alta)**:
```java
// Damage prediction
int damageIfUnblocked(Permanent attacker, Player defender, Game game);
int sumDamageIfUnblocked(List<Permanent> attackers, Player defender, Game game);
int predictDamageTo(Permanent target, int damage, Permanent source, Game game);

// Life/danger
boolean lifeInDanger(UUID playerId, Game game, int lookahead);
boolean lifeInSeriousDanger(UUID playerId, Game game);
int wouldLoseLifeNextTurn(UUID playerId, Game game);

// Combat math
int getEnoughDamageToKill(Permanent target, Permanent dealer, boolean withDeathtouch);
boolean dealsFirstStrikeDamage(Permanent p);
int getTotalFirstStrikeBlockPower(List<Permanent> blockers);
boolean isProfitableBlock(Permanent attacker, List<Permanent> blockers, Game game);

// Reuse Sprint 17 logic
int incomingDamageNextRotation(Game game, UUID playerId, Set<UUID> attackingIds);
int swarmAttackValue(Game game, UUID defenderId, List<Permanent> swarm);
```

### Constraints
- **NÃO QUEBRAR Sprint 17**: os métodos `incomingDamageNextRotation` e `swarmAttackValue` ficam no `CombatPredictor` mas com a mesma assinatura usada pela Sprint 17. Atualizar referências no `ComputerPlayer6`.

### Validação
- Regressão: rodar partidas, verificar que todos os logs `[HOLD-VALUE]`, `[HOLD-LETHAL]`, `[HOLD]` continuam aparecendo.

### Dependências
- Recomendado FAZER ANTES da Sprint 21 (block pipeline usa esses primitivos)

---

## 10. Sprint 23 — Spell Sequencing / Land Timing

### Objetivo
Bot decide melhor entre main 1 e main 2 — segura land/spell quando faz sentido.

### Melhoria visível
"O bot não baixou o land no início e usou para ativar habilidade na main 2." Pequeno mas perceptível.

### Forge reference
- `forge-ai/src/main/java/forge/ai/AiController.java` método `playLand` L516–714
- Profile knobs: `HOLD_LAND_DROP_FOR_MAIN2_IF_UNUSED`, `RESERVE_MANA_FOR_MAIN2_CHANCE`

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI/src/mage/player/ai/ComputerPlayer.java` | hook em `playLand` ou similar |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | reorder spells por main phase |

### Design

**Hold-land-for-main2**:
- Se na main 1 não há spell para gastar, e o bot tem `instant` ou `flash` na mão, **segurar o land drop**
- Se na main 2 ainda não jogou, baixar antes de end step

**Spell ordering**:
- Main 1: rampas, mana rocks, criaturas com haste, ETBs proativos
- Main 2: criaturas vanilla, peças que serão alvo

### Constantes
```java
public boolean HOLD_LAND_DROP_FOR_MAIN2 = true;
public int LAND_DROP_HOLD_CHANCE = 50;
```

### Dependências
- Sprint 18 (mana reservation) — saber quanto guardar

---

## 11. Sprint 24+ — Card-Specific AI (Top 20 Commander)

### Objetivo
AI específica para as 20 cartas mais frequentes e decisivas em Commander.

### Melhoria visível
Bot joga as cartas-chave "como humano": paga 1 do Rhystic Study, segura Cyclonic Rift para overload, não usa Counterspell em coisa fraca.

### Forge reference
- `forge-ai/src/main/java/forge/ai/ability/` — ~180 classes, uma por ApiType
- `forge-ai/src/main/java/forge/ai/SpellApiToAi.java` — dispatch
- Cards específicos no Forge: usar AILogic SVar em card data files

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/cardlogic/CardSpecificAi.java` | **CRIAR** — registry interface |
| `Mage.Player.AI.MA/src/mage/player/ai/cardlogic/impl/*.java` | **CRIAR** — uma classe por carta |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | hook em decisões de jogo/target |

### Lista priorizada (top 20 cartas Commander)

| # | Carta | Categoria | Lógica resumida | Forge ref |
|---|---|---|---|---|
| 1 | Rhystic Study | Engine | "Sempre pague 1 contra esse bot" + "destrua se possível" | (genérico) |
| 2 | Smothering Tithe | Engine | "Sempre pague 2" + "destrua urgente" | (genérico) |
| 3 | Cyclonic Rift | Wipe | "Segure para overload na main 2 do oponente" | (genérico) |
| 4 | Dockside Extortionist | Mana | "Counter sempre se possível" | (genérico) |
| 5 | Sol Ring | Mana ramp | "Counter se turno 1, ignore depois" | (genérico) |
| 6 | Mana Crypt | Mana | "Counter alta prioridade" | (genérico) |
| 7 | Counterspell | Counter | Sprint 19 cobre |  |
| 8 | Force of Will | Counter free | Sprint 19 cobre |  |
| 9 | Mana Drain | Counter | Sprint 19 cobre |  |
| 10 | Toxic Deluge | Wipe | "Use X = max viable, target windows" | `DamageDealAi` |
| 11 | Wrath of God | Wipe | "Use quando 3+ criaturas oponentes" | (genérico) |
| 12 | Damnation | Wipe | igual Wrath |  |
| 13 | Swords to Plowshares | Removal | Sprint 20 cobre |  |
| 14 | Path to Exile | Removal | Sprint 20 cobre |  |
| 15 | Sensei's Divining Top | Engine | "Use em response a wipes" + "scry após draw" | `ScryAi` |
| 16 | Demonic Tutor | Tutor | Sprint 19 cobre defensivamente |  |
| 17 | Vampiric Tutor | Tutor | Sprint 19 cobre defensivamente |  |
| 18 | Cultivate | Ramp | "Pegue Forest + utility land" | (genérico) |
| 19 | Sylvan Library | Engine | "Pague 4 se vida alta, evite se baixa" | (custom) |
| 20 | Esper Sentinel | Engine | "Pague 1 sempre" | (genérico) |

**Subdivisão por sub-sprints**: faça 5 cartas por sprint (Sprint 24, 24b, 24c, 24d).

### Arquitetura

**Registry baseado em nome de carta**:
```java
public interface CardSpecificAi {
    boolean shouldCast(Card card, Game game, UUID aiPlayerId);
    Permanent chooseTarget(SpellAbility sa, Game game, UUID aiPlayerId);
    int chooseX(Card card, Game game, UUID aiPlayerId);
    // ... outras decisões
}

public class CardSpecificAiRegistry {
    private static final Map<String, CardSpecificAi> byName = new HashMap<>();
    static {
        byName.put("Rhystic Study", new RhysticStudyAi());
        byName.put("Smothering Tithe", new SmotheringTitheAi());
        // ...
    }
    public static Optional<CardSpecificAi> get(String cardName);
}
```

### Dependências
- Sprint 18 (mana reservation) — várias cartas dependem
- Sprint 19 (counter categorization) — counter-related
- Sprint 20 (removal timing) — removal-related

---

## 12. Sprint 25 — Profile System

### Objetivo
Containers de constantes configuráveis. Permite ter bots com personalidades diferentes na mesma mesa.

### Melhoria visível
3 bots numa mesa jogam diferentes (aggro / control / midrange).

### Forge reference
- `forge-ai/src/main/java/forge/ai/AiProps.java`

### Arquivos
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/profile/AiProfile.java` | **CRIAR** (4.3 acima) |
| `Mage.Player.AI.MA/src/mage/player/ai/profile/profiles/*.java` | **CRIAR** — Aggro/Control/Midrange |
| `Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` | construtor recebe profile |

### Design

Migrar todas as constantes existentes para `AiProfile`. Criar 3 perfis:

**Aggro**: `PLAY_AGGRO = 5`, `MIN_REMOVAL_TARGET_SCORE = 1500` (gasta mais cedo), `RESERVE_MANA_FOR_NEXT_SPELL_CHANCE = 30` (menos reserva).

**Control**: `PLAY_AGGRO = 2`, `RESERVE_MANA_FOR_NEXT_SPELL_CHANCE = 85`, `ALWAYS_COUNTER_BOARDWIPE = true`, `ALWAYS_COUNTER_DRAW_ENGINE = true`.

**Midrange (default)**: valores intermediários.

### Dependências
- Idealmente FAZER cedo (Sprint 25 = depois de 18-22) porque facilita tuning das sprints subsequentes.

---

## 13. Sprint 26 — Threat Tagging (Engine Pieces)

### Objetivo
Identificar engine pieces (Rhystic, Tithe, Top, Sylvan Library, etc.) e marcar como ameaça alta independente de stats.

### Forge reference
- Não tem implementação direta no Forge, mas `useRemovalNow` por tipo se aproxima.

### Arquivos
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI.MA/src/mage/player/ai/score/ArtificialScoringSystem.java` | extender com tag-based bonus |
| `Mage.Player.AI.MA/src/mage/player/ai/score/EnginePieceRegistry.java` | **CRIAR** — lista de cartas com tag |

### Design
```java
public class EnginePieceRegistry {
    private static final Set<String> ENGINES = Set.of(
        "Rhystic Study", "Smothering Tithe", "Esper Sentinel",
        "Mystic Remora", "Sylvan Library", "Sensei's Divining Top",
        "Bolas's Citadel", "Necropotence", ...
    );
    public static boolean isEngine(Card c);
}
```

Bonus de score quando `isEngine(target)` = true → +2000 pts. Influencia removal targeting (Sprint 4 já existe), threat ranking, counter decision.

### Dependências
- Sprint 4 (threat scoring) — JÁ FEITO

---

## 14. Sprint 27 — BoardWipeWatcher para Smart Skip

### Objetivo
Smart Skip do HumanPlayer para também na presença de boardwipes não-targetados (DestroyAll, ExileAll).

### Forge reference
- Não diretamente. Inspiração no `ComputerUtilCombat.combatTriggerWillTrigger`.

### Arquivos
| Arquivo | Ação |
|---|---|
| `Mage.Player.Human/src/mage/player/human/HumanPlayer.java` | adicionar BoardWipeWatcher check |
| Possível nova classe Watcher | dependendo do approach |

### Design
Já existe TODO no `project_xmage.md`. Ver "Smart Skip F5/F9/F11 — evolução possível".

### Dependências
- Independente do resto do roadmap.

---

## 15. Ordem de execução recomendada

```
Sprint 0  (Foundation Research)     ← PRIMEIRA, antes de qualquer código
   ↓
Sprint 18 (Mana reservation)        ← FUNDAÇÃO de código
   ↓
Sprint 22 (CombatPredictor utility) ← refatoração ANTES de mais sprints de combate
   ↓
Sprint 19 (Counter strategy)        ← depende de 18
Sprint 20 (Removal timing)          ← independente
Sprint 21 (Block pipeline)          ← depende de 22
   ↓
Sprint 25 (Profile system)          ← antes das sprints de tuning fino
Sprint 23 (Sequencing)              ← depende de 18
   ↓
Sprint 26 (Engine tagging)
Sprint 24a-d (Card-specific x4)     ← em paralelo, 5 cartas por sub-sprint
   ↓
Sprint 27 (BoardWipeWatcher)        ← polish
Sprint 28 (Modal Card Choice)       ← independente, recomendado após Sprint 19
```

---

## 16. Execution Playbook (workflow para AI agents)

### Início de cada sessão de sprint
1. Ler **seção 2** (estado atual) e **seção da sprint corrente**
2. Verificar `memory/project_xmage.md` e `memory/project_ai_roadmap.md` para contexto
3. Confirmar modelo recomendado para a fase corrente

### Workflow padrão por sprint

#### ⚠️ ETAPA 0 — BRIEFING PARA DIEGO (OBRIGATÓRIA, ANTES DE QUALQUER CÓDIGO)

**Por que existe**: Diego é jogador de Commander experiente mas não é dev. Antes de gastar tempo de engenharia, ele precisa validar se a heurística que vamos codar **faz sentido na perspectiva do jogo real**. Se a lógica está errada estrategicamente, o código vai ficar perfeito e o comportamento vai parecer burro.

**Linguagem**: usar **apenas vocabulário de Magic/Commander** (commander, threat, tempo, ramp, engine, wipe, removal, value, instant-speed, end step, etc.). **Zero termos técnicos** (classe, método, refactor, API, hook, etc.).

**Estrutura do briefing** — siga este template:

```
# Sprint [N] — Briefing para Diego

## O problema (do ponto de vista de Magic)
[1-2 parágrafos descrevendo o comportamento atual do bot em linguagem
de jogador. Exemplo bom: "Hoje o bot tapa toda a mana toda vez que
joga uma criatura na main 1. Resultado: você nunca vê um bot com mana
aberta no end step esperando contraparar. Mesmo que ele tenha
Counterspell na mão, ele nunca consegue usar porque já gastou tudo."]

## O que vamos ensinar o bot a fazer
[3-5 bullets em linguagem de jogador, descrevendo a NOVA regra de
comportamento. Exemplo: "- Olhar a mão antes de gastar mana. Se tem
instant/flash relevante, deixar mana suficiente aberta. - Identificar
o CMC máximo dos instants na mão (Counterspell = UU, então 2 mana
reservadas). - Variar a reserva: nem sempre reservar 100% das vezes,
porque jogador humano também varia."]

## Quando o bot vai aplicar
[Em que momentos do turno/jogo a lógica dispara. Exemplo: "Toda main
phase, antes de decidir o que jogar. Mais relevante na main 2 quando
o turno está acabando e o bot precisa pensar no turno do oponente."]

## Como você vai ver isso na partida
[O que vai aparecer no log da partida e o que vai mudar no
comportamento visível. Exemplo: "Mensagem nova no log:
'[RESERVE] Holding 2 mana for Counterspell'. Comportamento visível:
bot vai passar turno com mana destapada quando antes tapava tudo."]

## Limites — o que o bot NÃO vai fazer
[O que está fora do escopo, para não criar expectativa errada.
Exemplo: "- NÃO vai prever que oponente vai jogar wipe. - NÃO vai
descartar criatura para liberar mana. - NÃO vai trocar reserva entre
turnos."]

## Perguntas para você (Diego)
[3-5 perguntas concretas que afetam o design e que dependem do
conhecimento de Magic. Exemplo:
1. Em quantos % dos turnos faz sentido reservar mana? 50%? 80%? 100%?
2. Vale reservar mesmo sem instant na mão, "por precaução"?
3. Se eu reservar 2 mana e na main 2 tem uma criatura 4cmc que quero
   muito jogar, troco a reserva pelo jogo da criatura ou mantenho?
4. Reserva muda em early game vs late game?]
```

**Diego responde as perguntas**, valida ou pede ajustes. Só depois disso a AI pode partir para Etapa 1.

#### Etapas técnicas (após briefing aprovado)

1. **Pesquisa Forge** (agente Explore ou WebFetch direto):
   - Ler arquivo Forge da seção "Forge reference"
   - Extrair algoritmo + constantes
   - Reportar em ≤500 palavras
2. **Implementação Etapa 1** (helpers / classes novas):
   - Sonnet 4.6 suficiente
   - Apenas estrutura, sem lógica complexa
3. **PAUSA 1** — trocar modelo se necessário
4. **Implementação Etapa 2** (lógica principal):
   - Opus 4.7 para algoritmos delicados
5. **PAUSA 2** — voltar para Sonnet
6. **Build + Deploy**:
   - `build-and-deploy-ai.bat` (ou Maven manual)
   - Verificar build SUCCESS
7. **Validação live**:
   - Diego joga 1 partida com `AI_DEBUG_LOG = true`
   - Verifica logs específicos da sprint
   - Relata comportamentos estranhos
8. **Calibração** (se necessário, Opus):
   - Ajustar constantes baseado em observação
9. **Atualização de tracking**:
   - Marcar sprint como COMPLETE em seção 17
   - Atualizar `memory/project_xmage.md` com referência
   - Atualizar `CLAUDE.md` se tocou em arquitetura geral

### Regras invariantes
- **ETAPA 0 (briefing) é obrigatória** — Diego precisa aprovar a lógica antes do código
- **NUNCA modificar arquivos sem confirmação explícita** (regra global do Diego em [feedback_workflow.md])
- Sempre fazer build + deploy ao final de cada implementação
- `AI_DEBUG_LOG = true` durante dev, **desligar antes de release público**
- Não tocar `selectBlockers` reativo do minimax (Sprint 21 trabalha em `declareBlockers`)
- Manter Sprint 17 funcionando — todos os logs `[HOLD-VALUE]`, `[HOLD-LETHAL]` continuam aparecendo

### Padrão obrigatório de comentários (estabelecido em 2026-05-20)

Todo código novo ou modificado deve seguir este padrão — sem exceção:

**Constantes numéricas**: comentário com (1) o que o valor significa em termos de Magic, (2) por que esse número específico, (3) o que acontece se aumentar/diminuir.
```java
// WHY 3000: a player with a mid-sized board scores ~2700; 3000 catches anyone with a real board.
private static final int DEFENDER_THRESHOLD = 3000;
```

**Decisões de arquitetura / detecção**: comentário com (1) qual abordagem foi usada, (2) quais alternativas foram consideradas, (3) por que as alternativas foram descartadas.
```java
// Detection: effect-based (GainAbilityAllEffect + "indestructible" in text) + small name list.
// Why NOT name-only: the list is infinite and stale. Why NOT effect-only: exile-and-return
// cards (Eerie Interlude) don't emit indestructible effects.
```

**Listas hardcoded** (nomes de cartas, categorias): comentário explicando (1) por que hardcoded em vez de detecção genérica, (2) quem decidiu e por quê, (3) critério para adicionar itens.

**Qualquer decisão de design não-óbvia DEVE ser discutida com Diego antes de implementar** — especialmente: listas de cartas, thresholds novos, abordagem de detecção (nome vs efeito vs texto), mudanças que afetam comportamento em partida.

### Validação final (ao término da última sprint do roadmap)

Ao concluir a última sprint planejada (Sprint 27 ou a que encerrar o roadmap ativo):
1. **Audit de comentários**: varrer todos os arquivos modificados pelo projeto; qualquer constante, lista ou decisão sem comentário WHY → adicionar.
2. **Audit de thresholds**: comparar todos os valores calibráveis com experiência acumulada de partidas; ajustar os que ficaram fora de lugar.
3. **Audit de comportamento**: jogar 3 partidas completas com `AI_DEBUG_LOG = true`; verificar que logs de todas as sprints (1–N) ainda aparecem conforme esperado.
4. **Release final**: pack-release.bat → publicar no GitHub com changelog completo.

---

## 17. Tracking — Status das Sprints

**Legenda**: ⬜ Pendente | 🟦 Em progresso | ✅ Completa | ⚠️ Bloqueada

| Sprint | Nome | Status | Data | Notas |
|---|---|---|---|---|
| **0** | **Foundation Research (Commander wisdom)** | ✅ | 2026-05-19 | 7 arquivos em `research/commander_wisdom/` (~1665 linhas, ~150 regras). Próxima sessão: Claude lê, cross-references com Forge inventory, ajusta plano se necessário |
| 18 | Mana Reservation Memory | ✅ | 2026-05-19 | Context-sensitive scoring em GameStateEvaluator2. `AiMemory` criado. Reserva via bonus de untapped mana ponderado por self-position (archenemy/leading/parity/trailing). Proteção em massa = 400pts/mana. |
| 19 | Reactive Instant Strategy (counter + protection timing) | ⬜ | — | **PRÓXIMA** — renomeada: cobre counter E protection. Depende de 18 ✅ |
| 20 | Removal Timing | ⬜ | — | Independente |
| 21 | Block Pipeline 6-fases | ⬜ | — | Depende de 22 |
| 22 | CombatPredictor utility | ⬜ | — | Recomendado antes de 21 |
| 23 | Spell Sequencing | ⬜ | — | Depende de 18 |
| 24a | Card AI: engines (Rhystic, Tithe, Esper Sentinel, Mystic Remora, Sylvan) | ⬜ | — | |
| 24b | Card AI: wipes/removal (Cyc Rift, Toxic Deluge, Wrath, Damnation, Swords) | ⬜ | — | |
| 24c | Card AI: counters/tutors (Force, Mana Drain, Demonic, Vampiric, Brainstorm) | ⬜ | — | |
| 24d | Card AI: mana/utility (Sol Ring, Mana Crypt, Top, Cultivate, Dockside) | ⬜ | — | |
| 25 | Profile System | ⬜ | — | Idealmente cedo |
| 26 | Engine Piece Tagging | ⬜ | — | |
| 27 | BoardWipeWatcher Smart Skip | ⬜ | — | Polish |
| 28 | Modal Card Choice Intelligence | ⬜ | — | `chooseMode()` heurístico; independente; recomendado após Sprint 19 |

---

## 18. Referências essenciais

### Forge — arquivos críticos no GitHub
URLs raw para WebFetch direto:

- AiController: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiController.java
- AiAttackController: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiAttackController.java
- AiBlockController: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiBlockController.java
- ComputerUtilCombat: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ComputerUtilCombat.java
- ComputerUtilCard: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ComputerUtilCard.java
- AiCardMemory: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiCardMemory.java
- AiProps: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/AiProps.java
- SpellApiToAi: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/SpellApiToAi.java
- SpellAbilityAi (base): https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/SpellAbilityAi.java
- CounterAi: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ability/CounterAi.java
- GameStateEvaluator: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/simulation/GameStateEvaluator.java
- SpellAbilityPicker: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/simulation/SpellAbilityPicker.java
- GoadAi: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ability/GoadAi.java
- VoteAi: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ability/VoteAi.java
- DestroyAi: https://raw.githubusercontent.com/Card-Forge/forge/master/forge-ai/src/main/java/forge/ai/ability/DestroyAi.java

### XMage — arquivos críticos locais
- `mage-source/Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/ComputerPlayer6.java` — alvo principal
- `mage-source/Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/score/GameStateEvaluator2.java` — score
- `mage-source/Mage.Server.Plugins/Mage.Player.AI/src/mage/player/ai/ComputerPlayer.java` — base
- `mage-source/Mage.Server.Plugins/Mage.Player.Human/src/mage/player/human/HumanPlayer.java` — Smart Skip
- `mage-source/Mage.Server.Plugins/Mage.Player.AI.MA/src/mage/player/ai/util/CombatUtil.java`

### XMage — arquivos de regra Commander
- `mage-source/Mage/src/main/java/mage/game/GameCommanderImpl.java` — engine Commander
- `mage-source/Mage/src/main/java/mage/watchers/common/CommanderInfoWatcher.java` — commander damage tracking

### Build & deploy
- `build-and-deploy-ai.bat` — Maven + cópia local
- `XMageAIPatch.exe` — patch para amigos via release
- `pack-release.bat` — empacota JARs para release

---

## 19. Notas finais

### Sobre escopo
- Este roadmap cobre ~5-8 meses de trabalho part-time.
- Cada sprint = 1-3 sessões com Claude.
- Não é necessário seguir ordem rígida das sprints **não-bloqueantes** (20, 24a-d podem ir em paralelo).

### Sobre validação
- Diego é o validador final. Cada sprint termina com jogo de teste real (não simulação automatizada).
- `AI_DEBUG_LOG = true` permanente durante desenvolvimento.
- Toda sprint adiciona pelo menos 1 log prefix novo (`[RESERVE]`, `[COUNTER]`, `[REMOVAL]`, etc.) para visibilidade.

### Sobre regressões
- Sprint 17 e todas anteriores precisam continuar funcionando.
- Cada sprint nova: rodar 1 partida sanity check antes de declarar concluída.

### Quando atualizar este documento
- Ao **completar** uma sprint: atualizar seção 17 (tracking)
- Ao **descobrir nova dependência**: atualizar seção 15 (ordem)
- Ao **mudar de approach**: marcar seção da sprint com `⚠️ REVISÃO PENDENTE`
- **Sempre confirmar com Diego** antes de mudar estrutura/escopo geral

---

## 20. Sprint 28 — Modal Card Choice Intelligence

### Objetivo
Bot passa a escolher o **modo certo** em cartas com múltiplos modos, ao invés de sempre pegar o primeiro modo válido.

### Melhoria visível ao usuário
"O bot parou de fazer sempre a mesma escolha na Braids / Charm / Cryptic Command. Agora ele sacrifica a criatura mais fraca, returna o permanente mais valioso, etc."

### Diagnóstico (descoberto em 2026-05-20)

Dois caminhos distintos no código:

**A) Spells modais ao lançar** (`addModeOptions()` em `PlayerImpl.java:4540`):
- A AI já cria uma branch de simulação por modo → minimax escolhe a melhor.
- **Limitação documentada**: "choose 2" / "choose up to 3" não gera combinações, só modos individuais.
- TODO original no código: *"TODO: support modal spells with more than one selectable mode"*.

**B) Triggered/activated abilities durante resolução** (`chooseMode()` em `ComputerPlayer.java:941`):
- **Sempre retorna o primeiro modo válido**. Sem scoring, sem contexto.
- TODO original: *"TODO: add AI support to select best modes, current code uses first valid mode"*.
- Causa o comportamento observado na Braids e similares.

### Forge reference
- `forge-ai/src/main/java/forge/ai/SpellAbilityAi.java` — método `choseMode` (verifica outcome por modo)
- `forge-ai/src/main/java/forge/ai/ability/` — algumas classes específicas têm lógica por modo (ex: `ChooseAi`)

### Arquivos XMage a tocar
| Arquivo | Ação |
|---|---|
| `Mage.Player.AI/src/mage/player/ai/ComputerPlayer.java` | melhorar `chooseMode()` com heurísticas por categoria de efeito |
| `Mage.Player.AI/src/mage/player/ai/score/ModeScorer.java` | **CRIAR** — scorer estático por tipo de efeito |

### Design (abordagem B — heurísticas por categoria)

**Por que B e não simular cada modo**:
- Simular cada modo durante resolução seria caro: triggered abilities resolvem no meio da stack.
- Heurísticas couvram 90% dos casos com custo zero de runtime.

**`ModeScorer.score(Mode mode, Ability source, Game game, UUID aiPlayerId)`**:

```
Para cada modo disponível:
1. Detecta categoria do efeito principal do modo:
   - Sacrifice own permanent → prefira a criatura de menor score
   - Return opponent permanent → prefira a de maior score (oponente perde mais)
   - Return own permanent → prefira a de maior score (salva mais valor)
   - Destroy/exile target → prefira maior ameaça (reusa threat scoring Sprint 4)
   - Draw cards → sempre positivo (+500)
   - Deal damage to player → positivo proporcional à vida do player mais ameaçador
   - Counter spell → avalia stack (reusa Sprint 19 quando existir)
   - No targets required → pontuação base pelo outcome positivo

2. Retorna int score por modo
→ chooseMode() seleciona o de maior score
```

**Detecção de categoria**: por classe do Effect (ex: `SacrificeEffect`, `ReturnToHandTargetEffect`, `DrawCardEffect`, `DestroyTargetEffect`) — mesmo padrão da detecção de mass protection (Sprint 18).

### Constantes
```java
// ModeScorer
private static final int DRAW_CARD_SCORE = 500;       // drawing is the most important mechanic
private static final int DAMAGE_TO_PLAYER_PER_POINT = 20;
private static final int RETURN_OPPONENT_BONUS = 200; // extra for bouncing threats
```

### Validação
- **Braids, Cabal Minion**: com board vantagem, bot escolhe sacrificar criatura mais fraca (não a primeira da lista).
- **Cryptic Command**: com spell na stack, bot escolhe "counter + bounce" ao invés de "tap + draw" cegamente.
- Regressão: modos simples (um modo apenas) continuam funcionando.

### Dependências
- Sprint 4 (threat scoring) — JÁ FEITO, reutilizado para score de target
- Sprint 19 (stack classifier) — recomendado antes, mas sprint 28 pode funcionar sem ele (ignora modos de counter sem o classifier)

---

**FIM DO ROADMAP**
