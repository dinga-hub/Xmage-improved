# XMage AI Improvement Project — Orchestrator

**Cursor:** em todo chat neste workspace as regras do projeto vêm de `.cursor/rules/xmage-orchestrator.mdc` e do resumo em `AGENTS.md`. Este arquivo continua sendo a **referência longa** (build, deploy, sprints, publicação).

## Contexto do projeto

XMage 1.4.58 rodando localmente. Diego joga Commander (4 players) contra bots e melhora o comportamento da IA modificando o código-fonte Java, compilando e deployando os JARs no servidor local.

## Ambiente de build

| Componente | Caminho |
|---|---|
| Fonte | `C:\Users\diego\Desktop\XMage\mage-source` (branch `diego-ai-improvements`) |
| JDK 8 | `C:\Program Files\Eclipse Adoptium\jdk-8.0.482.8-hotspot` |
| Maven 3.9.6 | `C:\Users\diego\Desktop\XMage\apache-maven-3.9.6` |
| Servidor XMage | `C:\Users\diego\Desktop\XMage\xmage\mage-server` |

### Build (3 módulos)

```bash
cd "C:/Users/diego/Desktop/XMage/mage-source"
"C:/Users/diego/Desktop/XMage/apache-maven-3.9.6/bin/mvn.cmd" install \
  -pl Mage.Server.Plugins/Mage.Player.AI,Mage.Server.Plugins/Mage.Player.AI.MA,Mage.Server.Plugins/Mage.Player.Human \
  -am -DskipTests
```

### Deploy (3 JARs)

```bash
SRC="C:/Users/diego/Desktop/XMage/mage-source"
DST="C:/Users/diego/Desktop/XMage/xmage/mage-server"

cp "$SRC/Mage.Server.Plugins/Mage.Player.AI/target/mage-player-ai.jar"       "$DST/lib/mage-player-ai-1.4.58.jar"
cp "$SRC/Mage.Server.Plugins/Mage.Player.AI.MA/target/mage-player-ai-ma.jar" "$DST/plugins/mage-player-ai-ma-1.4.58.jar"
cp "$SRC/Mage.Server.Plugins/Mage.Player.Human/target/mage-player-human.jar" "$DST/plugins/mage-player-human-1.4.58.jar"
```

### Scripts de manutenção

- **`XMageAIPatch.exe`** (raiz do workspace) — **reaplica o patch de IA** para você e para os amigos: baixa os 3 JARs do release, detecta os nomes versionados **já instalados** (qualquer versão do servidor), ajusta JVM. Rode de novo **sempre que o update oficial do XMage sobrescrever** `lib\` e `plugins\`.
- **`build-installer.bat`** — recompila `installer-src\XMageInstaller.cs` → `XMageAIPatch.exe`.
- **`build-and-deploy-ai.bat`** — quando você **mudou o Java** em `mage-source`: Maven + cópia dos JARs para os nomes detectados em `xmage\mage-server\` (também independente da versão no disco).
- **`pack-release.bat`** — copia os 3 JARs compilados para `release-jars\` antes de publicar no GitHub.

> Após cada sprint que mexe em build, validar `build-and-deploy-ai.bat` e o fluxo do instalador.

### Depois do update oficial do XMage (launcher / Grath)

1. **Só reaplicar o patch já publicado** (caso típico, inclusive amigos): rode **`XMageAIPatch.exe`**. Não depende de todos estarem na mesma versão da semana: o instalador lê os `mage-player-*-*.jar` que **já existem** na pasta do servidor.
2. **Sincronizar o seu `mage-source` com o upstream** antes de voltar a programar:
```bash
cd "C:/Users/diego/Desktop/XMage/mage-source"
git fetch origin
git rebase origin/master
```
3. **Compilar as suas alterações** e colocar no servidor local: `build-and-deploy-ai.bat`.

### Sync com upstream (Grath weekly)

Ver passo 2 acima; em seguida compile quando for trabalhar em cima do código.

## Arquivos modificados vs upstream

| Arquivo | Módulo | O que faz |
|---|---|---|
| `score/GameStateEvaluator2.java` | Mage.Player.AI.MA | evaluatePlayerThreat + floating mana score |
| `score/ArtificialScoringSystem.java` | Mage.Player.AI.MA | Sprint 6: valoriza criaturas utilitárias |
| `ai/PossibleTargetsComparator.java` | Mage.Player.AI.MA | Sprint 4: remoção ponderada por ameaça |
| `ai/ComputerPlayer.java` | Mage.Player.AI | Sprint 4: skip remoção de alvos fracos |
| `ComputerPlayer6.java` | Mage.Player.AI.MA | Sprints 2, 3, 7, 9: ataque/defesa inteligente |
| `SimulatedPlayer2.java` | Mage.Player.AI.MA | Fix X=0 |
| `optimizers/impl/BoardwipeOptimizer.java` | Mage.Player.AI.MA | Sprint 5b: suprime boardwipe com vantagem (novo arquivo) |
| `optimizers/impl/InstantTimingOptimizer.java` | Mage.Player.AI.MA | Sprint 5: timing de instants/flash (novo arquivo) |
| `HumanPlayer.java` | Mage.Player.Human | Smart Skip F5/F9/F11 |

## Sprints implementadas

| Sprint | Descrição | Arquivo principal |
|---|---|---|
| 1+8 | `evaluatePlayerThreat()`: board presence, ramp, mão, vida | `GameStateEvaluator2.java` |
| 2 | `declareAttackers()` ordena oponentes por ThreatScore | `ComputerPlayer6.java` |
| 3 | Retém bloqueador quando oponente ameaçador | `ComputerPlayer6.java` |
| 4 | Remoção ponderada por threatScore/5000 | `PossibleTargetsComparator.java` + `ComputerPlayer.java` |
| 5 | `InstantTimingOptimizer`: guarda instants para turno do oponente | `InstantTimingOptimizer.java` (novo) |
| 5b | `BoardwipeOptimizer`: suprime boardwipe quando em vantagem | `BoardwipeOptimizer.java` (novo) |
| 6 | Scoring utilitário: 70% combat-scaled + 30% base fixo | `ArtificialScoringSystem.java` |
| 7 | Suprime ataque inútil (sem trample/lifelink, não mata blocker) | `ComputerPlayer6.java` |
| 9 | Multi-block death check: não ataca quando 2+ blockers matam | `ComputerPlayer6.java` |
| Fix X=0 | `minX = max(1, minX)` em variableManaCost | `SimulatedPlayer2.java` |
| Floating mana | Mana no pool = +100 pts; evita gastar mana sem valor | `GameStateEvaluator2.java` |
| Smart Skip F5/F9/F11 | Para auto-skip: alvo em você/permanente seu, boardwipe, dano a cada oponente, dano mirado em jogador (F5/F9/F11) | `HumanPlayer.java` |

## Calibração — valores ajustáveis

| Constante | Valor atual | Onde |
|---|---|---|
| `MAX_SIMULATED_NODES` | 15000 | bytecode / build config |
| `minDepth` floor | 5 | bytecode / build config |
| JVM heap | Xmx4096m + G1GC | `XMageAIPatch.exe` (startServer.bat + `installed.properties` se existir) |
| `DEFENDER_THRESHOLD` | 3000 | `ComputerPlayer6.java` |
| `MIN_REMOVAL_TARGET_SCORE` | 800 | `ComputerPlayer.java` |
| `THREAT_NORMALIZER` | 5000 | `PossibleTargetsComparator.java` |
| `FLOATING_MANA_VALUE` | 100 | `GameStateEvaluator2.java` |
| Ramp baseline | 4 fontes, +120 pts cada | `GameStateEvaluator2.java` |

## Distribuição para amigos

### Repo de distribuição
`https://github.com/dinga-hub/Xmage-improved`

Instalação recomendada: **`XMageAIPatch.exe`** (mesmo fluxo que você usa após update oficial).  
Fallback sem compilar: no repo **`Xmage-improved`** existe **`install-ai-patch.bat`** (batch puro).  
Baixam os 3 JARs de:
```
https://github.com/dinga-hub/Xmage-improved/releases/latest/download/
```

### Estrutura do repo Xmage-improved
```
jars/                    ← JARs commitados (referência / download manual)
  mage-player-ai.jar
  mage-player-ai-ma.jar
  mage-player-human.jar
workspace-meta/          ← espelho leve: AGENTS, CLAUDE, memory, .cursor/rules, .bat (sem mage-source)
  README.md
installer-src/
  XMageInstaller.cs      ← fonte do exe (C#, compila com csc.exe do .NET 4)
install-ai-patch.bat     ← instalador alternativo em batch puro
build-installer.bat      ← compila o .cs → XMageAIPatch.exe (paths relativos a esta pasta)
README.md
```

### O que o instalador faz (exe; batch no repo espelha em grande parte)
1. **Detecta** a pasta `mage-server` via registro (`HKCU\Software\XMage\InstallDir`) e caminhos comuns
2. **Detecta** os nomes reais dos JARs no disco (`mage-player-ai-*.jar`, etc.) — **não fixa** `1.4.58`; se houver mais de um candidato, o exe prefere o mais recente por data de arquivo
3. **Faz backup** dos JARs atuais (`.backup`)
4. **Baixa** os 3 JARs do GitHub Release (`releases/latest/download`)
5. **Valida** tamanho mínimo (>10 KB) — aborta e restaura backup se falhar
6. **Memória JVM:** `startServer.bat` e, se existir subindo pastas a partir de `mage-server`, **`installed.properties`** — `-Xmx4096m` e `-XX:+UseG1GC`

### Fluxo de publicação (feito pelo Claude em cada atualização)
Diego pede "faz build, deploy e publica". Claude executa:
```bash
# 1. Build
mvn install -pl Mage.Player.AI,Mage.Player.AI.MA,Mage.Player.Human -am -DskipTests

# 2. Deploy local (servidor do Diego): no Windows use build-and-deploy-ai.bat
#    (detecta os *.jar versionados ja presentes). Em shell manual, copie os targets
#    para os mesmos nomes de arquivo que existem em lib/ e plugins/.

# 3. Atualiza jars/ no repo de distribuição e faz push
cp jars/*.jar → Xmage-improved/jars/ → git commit + push

# 4. Recria o GitHub Release (gh CLI, autenticado como dinga-hub)
gh release delete v1.4.58-latest --repo dinga-hub/Xmage-improved --yes
gh release create v1.4.58-latest --repo dinga-hub/Xmage-improved \
  --title "XMage AI - build atual" \
  jars/mage-player-ai.jar jars/mage-player-ai-ma.jar jars/mage-player-human.jar
```

### Ferramentas necessárias
- `gh` CLI instalado em `C:\Program Files\GitHub CLI` — autenticado como `dinga-hub`
- Credenciais git configuradas no repo `Xmage-improved` (user: Diego, email: diegolissoni@gmail.com)

## Tópicos pendentes

### Smart Skip F5/F9/F11 — evolução possível
Modo alternativo (ex: Shift+F9) que para também para boardwipes não-targetados (DestroyAll, ExileAll, etc.).
Abordagem preferida: novo `PlayerAction` enum + boolean no player + botão em `GamePanel.java`.
Decidir na próxima sessão se vale o custo de UI.

## Workflow de colaboração

1. **Nunca modificar arquivos** sem confirmação explícita do usuário. Análise e discussão são livres; execução (Edit/Write) só após "pode implementar" / "sim" / "faz isso".
2. **Atualizar `memory/project_xmage.md`** à medida que o trabalho avança (não só no final).
3. Ao final de cada sprint, validar `build-and-deploy-ai.bat` e o fluxo do **`XMageAIPatch.exe`** / release.
4. **Upstream oficial:** não fazer `git push`, PR nem colaboração direta com `magefree/mage` (patch local + `Xmage-improved` apenas). Detalhe para agentes: `.cursor/rules/xmage-orchestrator.mdc`.
