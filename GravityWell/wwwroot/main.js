import { dotnet } from './_framework/dotnet.js';

// raylib loads fonts from the Emscripten virtual filesystem, so the font must be
// registered as a VFS asset. Without this entry, LoadFontEx falls back to the default font.
const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withConfig({
        resources: {
            vfs: [{ name: '../fonts/Orbitron-Medium.ttf', virtualPath: 'fonts/Orbitron-Medium.ttf' }],
        },
    })
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const canvas = document.getElementById('canvas');
const loadingScreen = document.getElementById('loading');

dotnet.instance.Module.canvas = canvas;

function resizeCanvas() {
    const pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
    const width = Math.max(1, Math.round(window.innerWidth * pixelRatio));
    const height = Math.max(1, Math.round(window.innerHeight * pixelRatio));

    exports.GravityWell.Application.ResizeCanvas(width, height);
}

// requestAnimationFrame fires at the display's refresh rate: 60, 120, 144 Hz and so on. The game steps by the
// measured frame time, so it is correct at any rate, but a faster display would just do more work for nothing.
// Ticks are scheduled on a fixed 60 Hz clock rather than "at least 1/60 s since the last one", which would drop a
// 144 Hz display to 48 Hz. raylib's own SetTargetFPS is not used for this: on the web it spins in a loop to fill the
// frame, which kept a CPU core at 100% all the time.
const TICK_MS = 1000 / 60;
// A frame may arrive this much before its tick is due and still take it, so the jitter in a 60 Hz display's frame
// timestamps (a millisecond or so, more on some browsers) cannot make it skip ticks. The grid is fixed, so this never
// raises the average above 60 ticks per second.
const TICK_SLACK_MS = TICK_MS / 4;
let nextTick = 0;

function mainLoop(now) {
    if (now >= nextTick - TICK_SLACK_MS) {
        // After a long hitch skip the ticks that were missed instead of running a burst of catch-up frames.
        nextTick += TICK_MS * (1 + Math.max(0, Math.floor((now - nextTick) / TICK_MS)));
        exports.GravityWell.Application.UpdateFrame();
        loadingScreen?.remove();
    }

    window.requestAnimationFrame(mainLoop);
}

await runMain();
window.addEventListener('resize', resizeCanvas);
resizeCanvas();
window.requestAnimationFrame(mainLoop);
