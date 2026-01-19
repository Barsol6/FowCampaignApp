window.mapTools = {
    canvas: null,
    ctx: null,
    img: null,
    originalImageData: null,
    maxPixelsPerFrame: Number.MAX_SAFE_INTEGER,

    initMap: (canvasId, imageSrc) => {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const ctx = canvas.getContext('2d', {willReadFrequently: true});
        const img = new Image();

        img.onload = () => {
            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0);

            window.mapTools.originalImageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            window.mapTools.canvas = canvas;
            window.mapTools.ctx = ctx;
            window.mapTools.img = img;
        };
        img.src = imageSrc;
    },

    getCanvasCoordinates: (visualX, visualY) => {
        const canvas = window.mapTools.canvas;
        if (!canvas) return {x: visualX, y: visualY};

        const rect = canvas.getBoundingClientRect();
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;

        return {
            x: Math.floor(visualX * scaleX),
            y: Math.floor(visualY * scaleY)
        };
    },

    floodFill: (visualX, visualY, fillColorHex) => {
        const coords = window.mapTools.getCanvasCoordinates(visualX, visualY);
        return window.mapTools.performFloodFill(coords.x, coords.y, fillColorHex, 60);
    },

    floodFillRaw: (x, y, fillColorHex) => {
        window.mapTools.performFloodFill(Math.floor(x), Math.floor(y), fillColorHex, 60);
    },

    performFloodFill: (startX, startY, colorHex, tolerance = 60) => {
        const ctx = window.mapTools.ctx;
        const canvas = window.mapTools.canvas;
        if (!ctx || !canvas) return null;

        if (startX < 0 || startY < 0 || startX >= canvas.width || startY >= canvas.height) {
            return null;
        }

        const r = parseInt(colorHex.slice(1, 3), 16);
        const g = parseInt(colorHex.slice(3, 5), 16);
        const b = parseInt(colorHex.slice(5, 7), 16);
        const fillRgb = {r, g, b, a: 200};

        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;
        const visited = new Uint8Array(canvas.width * canvas.height);

        const getPixelColor = (index) => {
            return {
                r: data[index],
                g: data[index + 1],
                b: data[index + 2],
                a: data[index + 3]
            };
        };

        const isBlackBorder = (color) => {
            return color.r < 50 && color.g < 50 && color.b < 50;
        };

        const colorMatch = (c1, c2, tol = tolerance) => {
            // Nie porównuj czarnych pikseli z niczym
            if (isBlackBorder(c1) || isBlackBorder(c2)) return false;

            return Math.abs(c1.r - c2.r) < tol &&
                Math.abs(c1.g - c2.g) < tol &&
                Math.abs(c1.b - c2.b) < tol;
        };

        const startPos = (startY * canvas.width + startX) * 4;
        const startColor = getPixelColor(startPos);

        // Jeśli kliknięty piksel to czarna granica, szukaj najbliższego non-black piksela
        if (isBlackBorder(startColor)) {
            for (let radius = 1; radius < 20; radius++) {
                for (let dx = -radius; dx <= radius; dx++) {
                    for (let dy = -radius; dy <= radius; dy++) {
                        if (Math.abs(dx) !== radius && Math.abs(dy) !== radius) continue;

                        const nx = startX + dx;
                        const ny = startY + dy;

                        if (nx >= 0 && ny >= 0 && nx < canvas.width && ny < canvas.height) {
                            const neighborPos = (ny * canvas.width + nx) * 4;
                            const neighborColor = getPixelColor(neighborPos);
                            if (!isBlackBorder(neighborColor)) {
                                return window.mapTools.performFloodFill(nx, ny, colorHex, tolerance);
                            }
                        }
                    }
                }
            }
            return null;
        }

        // Jeśli kliknięty piksel ma już docelowy kolor
        if (colorMatch(startColor, fillRgb, 15)) return {x: startX, y: startY};

        const queue = [[startX, startY]];
        visited[startY * canvas.width + startX] = 1;
        let pixelsFilled = 0;

        while (queue.length > 0) {
            const [x, y] = queue.shift();

            if (x < 0 || y < 0 || x >= canvas.width || y >= canvas.height) continue;

            const pixelIndex = (y * canvas.width + x) * 4;
            const currentColor = getPixelColor(pixelIndex);

            if (colorMatch(currentColor, startColor)) {
                data[pixelIndex] = fillRgb.r;
                data[pixelIndex + 1] = fillRgb.g;
                data[pixelIndex + 2] = fillRgb.b;
                data[pixelIndex + 3] = fillRgb.a;
                pixelsFilled++;

                const neighbors = [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]];
                for (const [nx, ny] of neighbors) {
                    if (nx >= 0 && ny >= 0 && nx < canvas.width && ny < canvas.height) {
                        const visitedIndex = ny * canvas.width + nx;
                        if (!visited[visitedIndex]) {
                            visited[visitedIndex] = 1;
                            queue.push([nx, ny]);
                        }
                    }
                }
            }
        }

        ctx.putImageData(imageData, 0, 0);
        return {x: startX, y: startY};
    },

    resetMap: () => {
        const ctx = window.mapTools.ctx;
        const img = window.mapTools.img;
        if (ctx && img) {
            ctx.drawImage(img, 0, 0);
        }
    },

    scanMapText: async (imageSrc) => {
        const worker = Tesseract.createWorker();
        await worker.load();
        await worker.loadLanguage('pol+eng');
        await worker.initialize('pol+eng');

        const {data: {words}} = await worker.recognize(imageSrc);
        await worker.terminate();

        return words.map(w => ({
            text: w.text,
            x: (w.bbox.x0 + w.bbox.x1) / 2,
            y: (w.bbox.y0 + w.bbox.y1) / 2
        }));
    },

    getSize: (x, y) => {
        return window.mapTools.getCanvasCoordinates(x, y);
    }
};
