# 0001: Use C# WebAssembly and raylib-cs

- Status: Accepted
- Date: 2026-10-10

## Context

The game must run in a browser, retain a C# codebase, and support immediate-mode
2D drawing suitable for the original game's vector aesthetic. The browser can't
run raylib's usual blocking game loop on its main thread.

## Decision

Use .NET 10's `Microsoft.NET.Sdk.WebAssembly` SDK and raylib-cs. JavaScript owns
the `requestAnimationFrame` loop and calls a C# `[JSExport]` method once per
frame. C# owns game state, simulation, and drawing.

## Consequences

- Most game code remains in C# and uses the same raylib API as a native build.
- Native raylib and the .NET runtime increase the initial download size.
- Browser lifecycle concerns such as animation frames and canvas sizing require
  a small JavaScript bridge.
- Browser-only behavior must be tested in the published WASM build, not only by
  compiling the managed code.
