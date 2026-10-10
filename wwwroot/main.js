import { dotnet } from './_framework/dotnet.js';

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
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

    exports.Application.ResizeCanvas(width, height);
}

function mainLoop() {
    exports.Application.UpdateFrame();
    loadingScreen?.remove();
    window.requestAnimationFrame(mainLoop);
}

await runMain();
window.addEventListener('resize', resizeCanvas);
resizeCanvas();
window.requestAnimationFrame(mainLoop);
