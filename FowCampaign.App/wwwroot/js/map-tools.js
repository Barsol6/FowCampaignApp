window.mapTools = {
    canvas: null,
    ctx: null,
    img: null,
    originalImageData: null,

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

    preprocessImageForOCR: (imageSrc) => {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => {
                const canvas = document.createElement('canvas');
                canvas.width = img.width;
                canvas.height = img.height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);

                const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const data = imageData.data;

                for (let i = 0; i < data.length; i += 4) {
                    const r = data[i];
                    const g = data[i + 1];
                    const b = data[i + 2];
                    const brightness = (r + g + b) / 3;

                    if (brightness < 128) {
                        data[i] = 0;
                        data[i+1] = 0;
                        data[i+2] = 0;
                    } else {
                        data[i] = 255;
                        data[i+1] = 255;
                        data[i+2] = 255;
                    }
                }

                ctx.putImageData(imageData, 0, 0);
                resolve(canvas.toDataURL('image/jpeg', 1.0));
            };
            img.src = imageSrc;
        });
    },

    scanMapText: async (imageSrc) => {
        const processedImage = await window.mapTools.preprocessImageForOCR(imageSrc);

        const worker = Tesseract.createWorker();
        await worker.load();
        await worker.loadLanguage('eng');
        await worker.initialize('eng');

        await worker.setParameters({
            tessedit_pageseg_mode: '11',
            tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.- '
        });

        const { data } = await worker.recognize(processedImage);
        await worker.terminate();

        if (data.words.length === 0) return [];

        let words = data.words.filter(w => w.confidence > 60 && w.text.trim().length > 1);

        const mergedLabels = [];
        const usedIndices = new Set();

        for (let i = 0; i < words.length; i++) {
            if (usedIndices.has(i)) continue;

            let currentGroup = [words[i]];
            usedIndices.add(i);

            let changed = true;
            while(changed) {
                changed = false;
                for (let j = 0; j < words.length; j++) {
                    if (usedIndices.has(j)) continue;

                    const wordB = words[j];

                    for (let member of currentGroup) {
                        const fontHeight = member.bbox.y1 - member.bbox.y0;

                        const isHorizontal =
                            Math.abs(member.bbox.y0 - wordB.bbox.y0) < (fontHeight * 0.7) &&
                            Math.abs((wordB.bbox.x0 - member.bbox.x1)) < (fontHeight * 3.5);

                        const isVertical =
                            Math.abs(member.bbox.x0 - wordB.bbox.x0) < (fontHeight * 2.5) &&
                            (wordB.bbox.y0 - member.bbox.y1) < (fontHeight * 2.5) &&
                            (wordB.bbox.y0 - member.bbox.y1) > -5;

                        if (isHorizontal || isVertical) {
                            currentGroup.push(wordB);
                            usedIndices.add(j);
                            changed = true;
                            break;
                        }
                    }
                    if (changed) break;
                }
            }

            currentGroup.sort((a, b) => {
                if (Math.abs(a.bbox.y0 - b.bbox.y0) > 15) return a.bbox.y0 - b.bbox.y0;
                return a.bbox.x0 - b.bbox.x0;
            });

            const text = currentGroup.map(w => w.text).join(' ');
            const cleanText = text.replace(/[^a-zA-Z\.\-]/g, ' ').trim().toUpperCase();

            if (cleanText.length < 2) continue;

            let minX = Math.min(...currentGroup.map(w => w.bbox.x0));
            let maxX = Math.max(...currentGroup.map(w => w.bbox.x1));
            let minY = Math.min(...currentGroup.map(w => w.bbox.y0));
            let maxY = Math.max(...currentGroup.map(w => w.bbox.y1));

            mergedLabels.push({
                text: cleanText,
                x: (minX + maxX) / 2,
                y: (minY + maxY) / 2
            });
        }

        return mergedLabels;
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
        if (startX < 0 || startY < 0 || startX >= canvas.width || startY >= canvas.height) return null;

        const r = parseInt(colorHex.slice(1, 3), 16);
        const g = parseInt(colorHex.slice(3, 5), 16);
        const b = parseInt(colorHex.slice(5, 7), 16);
        const fillRgb = {r, g, b, a: 255};

        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;
        const visited = new Uint8Array(canvas.width * canvas.height);

        const getPixelColor = (index) => ({
            r: data[index], g: data[index + 1], b: data[index + 2], a: data[index + 3]
        });

        const isBorder = (c) => c.r < 80 && c.g < 80 && c.b < 80;

        const colorMatch = (c1, c2, tol = tolerance) => {
            if (isBorder(c1) || isBorder(c2)) return false;
            return Math.abs(c1.r - c2.r) < tol && Math.abs(c1.g - c2.g) < tol && Math.abs(c1.b - c2.b) < tol;
        };

        const startPos = (startY * canvas.width + startX) * 4;
        const startColor = getPixelColor(startPos);

        if (isBorder(startColor)) return null;
        if (colorMatch(startColor, fillRgb, 10)) return {x: startX, y: startY};

        const queue = [[startX, startY]];
        visited[startY * canvas.width + startX] = 1;

        while (queue.length > 0) {
            const [x, y] = queue.shift();
            const pixelIndex = (y * canvas.width + x) * 4;
            const currentColor = getPixelColor(pixelIndex);

            if (colorMatch(currentColor, startColor)) {
                data[pixelIndex] = fillRgb.r;
                data[pixelIndex + 1] = fillRgb.g;
                data[pixelIndex + 2] = fillRgb.b;
                data[pixelIndex + 3] = fillRgb.a;

                const neighbors = [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]];
                for (const [nx, ny] of neighbors) {
                    if (nx >= 0 && ny >= 0 && nx < canvas.width && ny < canvas.height) {
                        const vIdx = ny * canvas.width + nx;
                        if (!visited[vIdx]) {
                            visited[vIdx] = 1;
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
        if (window.mapTools.ctx && window.mapTools.originalImageData) {
            window.mapTools.ctx.putImageData(window.mapTools.originalImageData, 0, 0);
        }
    },

    getSize: (x, y) => window.mapTools.getCanvasCoordinates(x, y)
};