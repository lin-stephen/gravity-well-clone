# 0003: Use an adaptive logical viewport

- Status: Accepted
- Date: 2026-10-10

## Context

Browser content areas vary with window size, display density, mobile layout, and
docked developer tools. Stretching a fixed canvas distorts the scene and makes
text blurry, while fixed 16:9 letterboxing wastes available space.

## Decision

Keep the logical height at 720 units and derive the logical width from the
available browser aspect ratio. Size the physical framebuffer to the browser
content area multiplied by `devicePixelRatio`, capped at 2. Use separate
screen-space and world cameras. The world camera follows the player fighter;
the HUD uses screen coordinates.

## Consequences

- The game consumes the full available browser area without distortion.
- Wider windows reveal more horizontal world space and narrower windows reveal
  less, so gameplay must not depend on a fixed visible width.
- UI elements must anchor against the adaptive logical width where appropriate.
- Pointer input must eventually be converted from physical pixels through the
  viewport transform into screen or world coordinates.
