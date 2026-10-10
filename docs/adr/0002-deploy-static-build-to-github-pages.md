# 0002: Deploy the static build to GitHub Pages

- Status: Accepted
- Date: 2026-10-10

## Context

The client is a self-contained browser application and doesn't need a server.
The repository is hosted on GitHub and should deploy automatically from `main`.

## Decision

Publish a Release build with `dotnet publish`, upload `publish/wwwroot` as a
Pages artifact, and deploy it with GitHub Actions. All runtime URLs remain
relative so the application works below the `/gravity-well-clone/` project path.
The generated .NET import map resolves fingerprinted framework modules.

## Consequences

- Hosting is static, inexpensive, and tied directly to the repository workflow.
- Features that require a server must use a separate service or a different
  hosting design.
- The output must include `.nojekyll` so GitHub Pages preserves `_framework`.
- Action versions must be kept current with GitHub runner runtimes.
