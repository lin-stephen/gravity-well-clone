# Architecture decision records

ADRs document technical choices that would otherwise be difficult to reconstruct
from the code or commit history. They describe why a choice was made as well as
its tradeoffs.

| ADR | Decision | Status |
|-----|----------|--------|
| [0001](0001-csharp-webassembly-and-raylib-cs.md) | Use C# WebAssembly and raylib-cs | Accepted |
| [0002](0002-deploy-static-build-to-github-pages.md) | Deploy the static build to GitHub Pages | Accepted |
| [0003](0003-adaptive-logical-viewport.md) | Use an adaptive logical viewport | Accepted |
| [0004](0004-separate-state-systems-and-renderers.md) | Separate state, systems, and renderers | Accepted |
| [0005](0005-use-invariant-globalization.md) | Use invariant globalization | Accepted |
| [0006](0006-colonize-planets-on-touch.md) | Colonize planets on touch until collision exists | Accepted |
| [0007](0007-ai-fighters-share-the-planet-approach-logic.md) | AI fighters share the planet approach logic | Accepted |
| [0008](0008-autopilots-are-strategies-over-a-shared-runner.md) | Autopilots are strategies over a shared runner | Accepted |
| [0009](0009-a-base-guards-a-planet-from-takeover.md) | A base guards a planet from takeover | Accepted |
| [0010](0010-fighter-behaviour-is-an-enum-chosen-per-faction-at-spawn.md) | Fighter behaviour is an enum chosen per faction at spawn | Accepted |

New records should copy [the template](template.md), use the next four-digit
number, and be added to this table. Accepted ADRs aren't edited to disguise a
later change: supersede them with a new ADR instead.
