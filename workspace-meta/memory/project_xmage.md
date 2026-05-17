# XMage — patch de IA (notas do workspace)

Registro **curto** para agentes. Detalhes operacionais: [`CLAUDE.md`](../CLAUDE.md).

## Estado

- Última atualização: 2026-05-17 — **Sprint 16** publicado: release `v1.4.58-latest` em `dinga-hub/Xmage-improved` (commit `6e3ae7b`, JAR `mage-player-ai-ma.jar`). Amigos: `XMageAIPatch.exe` como sempre. Deploy local feito; teste/feedback pendente.
- Anterior: **Smart Skip** F5/F9/F11 + fix alvos na pilha; release `v1.4.58-latest`.
- Anterior: opção C sem fix de alvos (Path falhava).
- Anterior (2026-05-09): Smart Skip F9 — NPE `effect.getText(null)` corrigido.
- Branch `mage-source`: `diego-ai-improvements` (confirmar com `git branch`)

## Backlog / próximo

- Testar Sprint 16 em Commander 4p (tokens + ameaça em outro jogador); feedback de amigos após `XMageAIPatch.exe`.
- Calibrar se necessário: `CHUMP_RESERVE_MAX_SCORE`, `CHUMP_THREAT_MIN_POWER`, `MAX_CHUMPS_RESERVED`.

## Observações / riscos

- **Smart Skip alvos na pilha:** usar `CardUtil.getAllSelectedTargets(ability, game)` — não confiar só em `ability.getTargets()` (Path, Solitude ETB, etc.).
- Fluxo unificado pós-update oficial: **`XMageAIPatch.exe`** (detecta nomes dos JARs no disco). Dev local: `build-and-deploy-ai.bat`. Removidos da raiz: `reaplicar-patch-ai.bat`, `install-ai-patch.bat`; pasta duplicada `dist-repo` removida se existia.
- **Espelho Git leve:** `Xmage-improved/workspace-meta/` (regras Cursor, `CLAUDE`, `AGENTS`, `memory/`, `.bat`) — antes de push no `Xmage-improved`, copiar alterações da raiz do workspace para lá; ver `workspace-meta/README.md`.
- **Deploy local:** parar o servidor XMage antes de copiar JARs — copy com servidor aberto pode travar (PowerShell). Usar `build-and-deploy-ai.bat` ou cópia rápida dos 3 targets.
- **Sprint 16 debug:** `AI_DEBUG_LOG = true` em `ComputerPlayer6.java` → logs `[HOLD] expendable chump` no servidor.
