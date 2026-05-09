# workspace-meta — kit Cursor + docs (sem `mage-source`)

Esta pasta guarda **cópias** do que define o patch de IA no dia a dia: regras do agente, documentação longa, notas e scripts de build. O **`mage-source` gigante não entra** neste Git — continua a ser um clone local do `magefree/mage` à parte.

## Para quê serve

- **Backup** de contexto para agentes e para ti.
- **PC novo:** instala XMage oficial + JDK 8 + Maven + clone de `mage-source` → abre no Cursor uma pasta que junte `mage-source` com estes ficheiros (ou copia o conteúdo desta pasta para a raiz desse workspace).
- **Regras Cursor:** copia `.cursor/rules/` daqui para `.cursor/rules/` na raiz do teu projeto local (ou mantém o repo clonado e aponta o Cursor para a pasta certa).

## Sincronizar com a árvore principal do patch

Se editas em `...\Desktop\XMage\` (AGENTS, CLAUDE, regras, `memory/`, `.bat`), **volta a copiar** para `workspace-meta/` antes de `git commit` no `Xmage-improved`, para o histórico do GitHub ficar alinhado. Os `.bat` e o `CLAUDE.md` usam caminhos absolutos do Diego — noutro PC, ajusta `JAVA_HOME`, `MAVEN`, `SOURCE`, `LIB`, `PLUGINS`, `RELEASE_DIR`.

## Conteúdo

| Caminho | Origem típica na máquina de dev |
|---------|----------------------------------|
| `AGENTS.md` | Raiz do workspace XMage |
| `CLAUDE.md` | Raiz |
| `memory/project_xmage.md` | `memory/` |
| `.cursor/rules/xmage-orchestrator.mdc` | `.cursor/rules/` |
| `build-and-deploy-ai.bat` | Raiz |
| `build-installer.bat` | Raiz (o repo já tem outro na raiz; este espelha o do desktop) |
| `pack-release.bat` | Raiz |

`installer-src/` e `install-ai-patch.bat` na **raiz** do `Xmage-improved` já cobrem o instalador; não duplicamos aqui.
