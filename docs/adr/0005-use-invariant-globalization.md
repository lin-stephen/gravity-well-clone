# 0005: Use invariant globalization

- Status: Accepted
- Date: 2026-10-10

## Context

.NET's browser runtime includes ICU and timezone data for culture-sensitive
formatting. The game currently has an English-only interface and doesn't use
localized dates, currency, collation, or timezone behavior. Those resources made
up a significant portion of the initial WASM download.

## Decision

Set `InvariantGlobalization` and `InvariantTimezone` to `true` in the project.
Use explicit game-facing formats instead of relying on the browser's current
culture.

## Consequences

- Release output decreased from approximately 7.79 MB to 3.20 MB uncompressed,
  and its Brotli sidecars from approximately 2.11 MB to 0.93 MB.
- Culture-specific formatting and localized timezone behavior are unavailable.
- If localization becomes a requirement, this decision must be revisited and
  the resulting download-size increase measured.
