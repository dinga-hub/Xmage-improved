---
name: project-ai-roadmap
description: Roadmap mestre de evolução da AI do XMage (Sprints 18-27). Forge-XMage hybrid port. Prioridade principal do projeto desde 2026-05-19.
metadata: 
  node_type: memory
  type: project
  originSessionId: b50134a1-5e67-4f0d-9a99-65ebb773739a
---

O projeto XMage AI tem agora um roadmap mestre de longo prazo em `C:\Users\diego\Desktop\XMage\AI_ROADMAP.md`.

**Por que existe**: as Sprints 1-17 cobriram o ataque coordenado, mas a análise comparativa com a AI do Forge revelou que estamos atrás em ~8 sistemas grandes (mana reservation, counter strategy, removal timing, block pipeline, sequencing, card-specific AI, profile system, threat tagging). Diego decidiu fazer port estruturado do Forge para o XMage como prioridade principal.

**Como aplicar**:
- Quando Diego pede para "avançar a próxima sprint", ler `AI_ROADMAP.md` seção 17 (tracking) para encontrar a próxima sprint pendente.
- Cada sprint é auto-contida no documento.
- Seguir o "Execution Playbook" (seção 16): pesquisa Forge → implementação em fases → build/deploy → validação live → calibração → atualização do tracking.
- **Sprint 0 (Foundation Research) COMPLETA em 2026-05-19**. 7 arquivos em `research/commander_wisdom/` (01_threat_assessment, 02_mana_management, 03_combat_decisions, 04_removal_strategy, 05_counter_strategy, 06_sequencing_curve, 07_politics_multiplayer). ~1665 linhas, ~150 regras estruturadas extraídas via GPT Plus modo pesquisa.
- **Sprint 18 COMPLETA (2026-05-20)**. Implementada via context-sensitive scoring em `GameStateEvaluator2.java`: bonus de untapped mana ponderado por self-position (archenemy=300/mana, leading=200, parity=100, trailing=0). Mass protection = 400/mana quando board forte (detecção effect-based: `GainAbilityAllEffect`+"indestructible" OU `PhaseOutAllEffect` + lista suplementar de 3 cartas). `AiMemory.java` criado em `mage.player.ai.memory`. Campo `memory` + `clearTurnMemory()` adicionados ao `ComputerPlayer6`. `ComputerPlayer7` chama `clearTurnMemory()` no end of turn. Build OK, deploy feito. **Validação live pendente** (Diego jogar 1 partida com `AI_DEBUG_LOG=true`, procurar logs `[RESERVE]`).
- **Sprint 19 (Reactive Instant Strategy) é a PRÓXIMA AÇÃO** — renomeada: cobre counter E protection timing. `StackThreatClassifier` + decisão de quando usar counter/protection baseada em categoria da ameaça na stack.
- **Sprint 28 (Modal Card Choice) adicionada ao roadmap (2026-05-20)**. Diagnóstico: `chooseMode()` em `ComputerPlayer.java:941` sempre retorna o primeiro modo válido sem scoring. Solução: `ModeScorer` com heurísticas por classe de Effect. Independente — pode ser feita após Sprint 19.
- Ordem recomendada de execução está na seção 15 do roadmap.

**Princípios invariantes**:
- Preservar tudo do XMage que já funciona (minimax, Sprint 17 coordination, evaluatePlayerThreat).
- Port "clean-room" do Forge: ler design, reimplementar com APIs XMage, importar constantes/thresholds testados.
- Não importar fraquezas do Forge (1-ply sim, fallbacks "pega primeiro", good-block capado em 3).
- `AI_DEBUG_LOG = true` durante dev; cada sprint adiciona pelo menos 1 log prefix novo (`[RESERVE]`, `[COUNTER]`, etc.).

Relacionado: [[project-xmage]], [[feedback-workflow]], [[feedback-session-sync]].
