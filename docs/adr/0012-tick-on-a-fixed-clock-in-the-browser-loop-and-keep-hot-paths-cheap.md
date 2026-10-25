# 0012: Tick on a fixed clock in the browser loop and keep hot paths cheap

- Status: Accepted
- Date: 2026-10-25

## Context

Playing in Chrome kept a CPU core at 100% and the game froze now and then. Profiling the real browser build
found two separate causes:

- `Application.Main` called `Raylib.SetTargetFPS(60)`. On the web raylib enforces that by spinning in a loop
  inside `EndDrawing`, so every frame callback took exactly 16.7 ms and the main thread was 99.6% busy. The
  game's own work is about 5 ms a frame.
- The freezes (180 to 490 ms) were all inside the AI autopilot, in `RoutePlanner.Plan`. It cost about 2 ms and
  226 KB per call natively, and the browser runs this .NET code in an interpreter that is about a hundred times
  slower than native. Garbage collection was not the cause: frames containing a collection took 4 ms.

## Decision

- **No `SetTargetFPS` on the web.** `main.js` ticks on a fixed 60 Hz clock instead: each `requestAnimationFrame`
  checks whether a tick is due, with a quarter tick of slack for timestamp jitter, and skips ticks missed in a
  hitch rather than running a burst. Skipping "if less than 1/60 s since the last tick" would give a 144 Hz
  display only 48 Hz. The simulation steps by the measured frame time, so it is correct at any rate.
- **Route planning stays cheap.** `RoutePlanner` returns the straight path when it is clear, otherwise searches
  with A* over flat arrays, and allocates nothing. Its old Dijkstra version is kept in the tests as an oracle:
  `RoutePlannerTests` require routes of the same length.
- **Per-frame queries do not allocate.** `Planet.HasStructure` and `HasOperationalStructure` are plain loops,
  guarded by a test, because every system asks them of every planet every frame.

## Consequences

- Measured in Chrome with the same scripted play: the main thread went from 99.6% to about 30% busy, and the
  worst frames from 180 to 490 ms down to about 32 ms.
- Native timings understate the cost here by about a hundred times. A change that adds even a millisecond to a
  per-order or per-frame path can be a visible stall in the browser, so measure there.
- A 60 Hz cap on a display slightly faster than 60 Hz (60.2 Hz, say) drops one tick every few seconds, a 33 ms
  gap. Displays at other rates tick evenly or in a steady pattern.
- Detours around planets still take roughly 0.2 ms natively, which is a couple of dropped frames in the browser.
  A destination that cannot be reached at all makes the search visit the whole graph, and the autopilot retries
  every second. If that shows up, bound the search or remember that the destination is unreachable.
- Further options if more headroom is needed: ahead-of-time compilation of the game, a warm-up pass during
  loading (the first plan after startup also stalls), and trimming the roughly 13 KB of garbage per tick.
