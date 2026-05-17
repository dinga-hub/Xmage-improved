# XMage — patch de IA (notas do workspace)

Registro **curto** para agentes. Detalhes operacionais: [`CLAUDE.md`](../CLAUDE.md).

## Estado

- Última atualização: 2026-05-17 — **Smart Skip:** F5/F9/F11; opção C (dano em jogador / each opponent); **fix alvos na pilha** (`CardUtil.getAllSelectedTargets` + `getPermanentOrLKIBattlefield`) — Path/Solitude no commander OK em teste. Publicado `Xmage-improved` + release `v1.4.58-latest`.
- Anterior: opção C sem fix de alvos (Path falhava).
- Anterior (2026-05-09): Smart Skip F9 — NPE `effect.getText(null)` corrigido.
- Branch `mage-source`: `diego-ai-improvements` (confirmar com `git branch`)

## Backlog / próximo

- (itens)

## Observações / riscos

- **Smart Skip alvos na pilha:** usar `CardUtil.getAllSelectedTargets(ability, game)` — não confiar só em `ability.getTargets()` (Path, Solitude ETB, etc.).
- Fluxo unificado pós-update oficial: **`XMageAIPatch.exe`** (detecta nomes dos JARs no disco). Dev local: `build-and-deploy-ai.bat`. Removidos da raiz: `reaplicar-patch-ai.bat`, `install-ai-patch.bat`; pasta duplicada `dist-repo` removida se existia.
- **Espelho Git leve:** `Xmage-improved/workspace-meta/` (regras Cursor, `CLAUDE`, `AGENTS`, `memory/`, `.bat`) — antes de push no `Xmage-improved`, copiar alterações da raiz do workspace para lá; ver `workspace-meta/README.md`.
