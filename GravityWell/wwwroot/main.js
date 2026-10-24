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

function mainLoop() {
    exports.GravityWell.Application.UpdateFrame();
    loadingScreen?.remove();
    window.requestAnimationFrame(mainLoop);
}

await runMain();
window.addEventListener('resize', resizeCanvas);
resizeCanvas();
window.requestAnimationFrame(mainLoop);
