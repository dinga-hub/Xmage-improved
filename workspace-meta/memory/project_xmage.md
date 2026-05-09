# XMage — patch de IA (notas do workspace)

Registro **curto** para agentes. Detalhes operacionais: [`CLAUDE.md`](../CLAUDE.md).

## Estado

- Última atualização: 2026-05-09 — **Smart Skip F9:** corrigido NPE em `HumanPlayer` (`effect.getText(null)` → `getText(ability.getModes().getMode())` + try/catch). Build Maven OK; JARs no `xmage\mage-server\`; `release-jars\`; `Xmage-improved\` + **push `main`**; release **`v1.4.58-latest`** recriado no GitHub com 3 JARs + `XMageAIPatch.exe`. Commit local em `mage-source` branch `diego-ai-improvements` (remote upstream `magefree/mage` — push 403; fork próprio se precisar espelhar).
- Branch `mage-source`: `diego-ai-improvements` (confirmar com `git branch`)

## Backlog / próximo

- (itens)

## Observações / riscos

- Fluxo unificado pós-update oficial: **`XMageAIPatch.exe`** (detecta nomes dos JARs no disco). Dev local: `build-and-deploy-ai.bat`. Removidos da raiz: `reaplicar-patch-ai.bat`, `install-ai-patch.bat`; pasta duplicada `dist-repo` removida se existia.
- **Espelho Git leve:** `Xmage-improved/workspace-meta/` (regras Cursor, `CLAUDE`, `AGENTS`, `memory/`, `.bat`) — antes de push no `Xmage-improved`, copiar alterações da raiz do workspace para lá; ver `workspace-meta/README.md`.
