import { dotnet } from './_framework/dotnet.js';

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const canvas = document.getElementById('canvas');

dotnet.instance.Module.canvas = canvas;

function resizeCanvas() {
    canvas.width = Math.max(640, window.innerWidth);
    canvas.height = Math.max(360, window.innerHeight);
}

function mainLoop() {
    exports.Application.UpdateFrame();
    window.requestAnimationFrame(mainLoop);
}

window.addEventListener('resize', resizeCanvas);
resizeCanvas();
await runMain();
window.requestAnimationFrame(mainLoop);
