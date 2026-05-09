# Instruções para agentes (Cursor)

Este repositório é o **workspace local do patch de IA** do XMage (Commander contra bots), não o upstream oficial.

1. Leia **`CLAUDE.md`** para build, deploy, caminhos Windows, módulos Maven, tabela de sprints e distribuição para amigos.
2. Siga **`memory/project_xmage.md`** para estado atual e backlog; mantenha-o atualizado durante o trabalho.
3. Regras injetadas automaticamente em todo chat: **`.cursor/rules/xmage-orchestrator.mdc`** (`alwaysApply`).

Resumo humano: melhorar IA em Java (`mage-source`), compilar/deploy com `build-and-deploy-ai.bat`; **após update oficial** (você e amigos) reaplicar JARs com **`XMageAIPatch.exe`**. Explicações em português do Brasil, mudanças pequenas; não editar arquivos sem confirmação explícita.

**Upstream:** agentes não fazem push, PR nem colaboração direta com `magefree/mage` — ver `.cursor/rules/xmage-orchestrator.mdc`.

**Espelho Git:** antes de commit/push no `Xmage-improved`, copiar ficheiros alterados para `Xmage-improved/workspace-meta/` conforme `.cursor/rules/xmage-orchestrator.mdc`.
