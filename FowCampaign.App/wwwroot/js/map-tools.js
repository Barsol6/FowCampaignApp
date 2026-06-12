window.mapTools = {
    canvas: null,
    ctx: null,
    img: null,
    originalImageData: null,

    initMap: (canvasId, imageSrc) => {
        return new Promise((resolve) => {
            const canvas = document.getElementById(canvasId);
            if (!canvas || !imageSrc) {
                resolve({width: 0, height: 0});
                return;
            }

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
                resolve({width: img.width, height: img.height});
            };


            img.onerror = (err) => {
                console.error(err);
                resolve({width: 0, height: 0});
            };
            img.src = imageSrc;
        });
    },

    getClientDimensions: (elementId) => {
        const el = document.getElementById(elementId);
        if (!el) return {width: 1, height: 1};
        const rect = el.getBoundingClientRect();
        return {width: rect.width, height: rect.height};
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
                        data[i + 1] = 0;
                        data[i + 2] = 0;
                    } else {
                        data[i] = 255;
                        data[i + 1] = 255;
                        data[i + 2] = 255;
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

        const {data} = await worker.recognize(processedImage);
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
            while (changed) {
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

        const width = canvas.width;
        const height = canvas.height;

        if (startX < 0 || startY < 0 || startX >= width || startY >= height) return null;

        const originalData = window.mapTools.originalImageData ? window.mapTools.originalImageData.data : ctx.getImageData(0, 0, width, height).data;
        const currentImageData = ctx.getImageData(0, 0, width, height);
        const data = currentImageData.data;

        const visited = new Uint8Array(width * height);

        let effectiveX = Math.floor(startX);
        let effectiveY = Math.floor(startY);

        const getBrightness = (x, y) => {
            const idx = (y * width + x) * 4;
            return (originalData[idx] + originalData[idx + 1] + originalData[idx + 2]) / 3;
        };

        if (getBrightness(effectiveX, effectiveY) < 80) {
            let foundSafeSpot = false;
            for (let r = 1; r <= 8; r++) {
                const offsets = [[0, r], [0, -r], [r, 0], [-r, 0], [r, r], [-r, -r]];
                for (let o of offsets) {
                    const nx = effectiveX + o[0];
                    const ny = effectiveY + o[1];
                    if (nx >= 0 && ny >= 0 && nx < width && ny < height) {
                        if (getBrightness(nx, ny) > 100) {
                            effectiveX = nx;
                            effectiveY = ny;
                            foundSafeSpot = true;
                            break;
                        }
                    }
                }
                if (foundSafeSpot) break;
            }
            if (!foundSafeSpot) return null;
        }

        let r, g, b;
        if (colorHex.length === 4) {
            r = parseInt(colorHex[1] + colorHex[1], 16);
            g = parseInt(colorHex[2] + colorHex[2], 16);
            b = parseInt(colorHex[3] + colorHex[3], 16);
        } else {
            r = parseInt(colorHex.slice(1, 3), 16);
            g = parseInt(colorHex.slice(3, 5), 16);
            b = parseInt(colorHex.slice(5, 7), 16);
        }
        const fillRgb = {r, g, b, a: 255};

        const startIdx = (effectiveY * width + effectiveX) * 4;
        let startR = originalData[startIdx];
        let startG = originalData[startIdx + 1];
        let startB = originalData[startIdx + 2];

        if (startR > 200 && startG > 200 && startB > 200) {
            startR = 255;
            startG = 255;
            startB = 255;
        }

        const colorsMatch = (idx) => {
            const or = originalData[idx];
            const og = originalData[idx + 1];
            const ob = originalData[idx + 2];
            const diff = Math.abs(or - startR) + Math.abs(og - startG) + Math.abs(ob - startB);
            return diff < tolerance ;
        };

        const queue = [[effectiveX, effectiveY]];
        visited[effectiveY * width + effectiveX] = 1;
        
        let head = 0;

        while (head < queue.length) {
            const [x, y] = queue[head++];
            const pixelIndex = (y * width + x) * 4;

            data[pixelIndex] = fillRgb.r;
            data[pixelIndex + 1] = fillRgb.g;
            data[pixelIndex + 2] = fillRgb.b;
            data[pixelIndex + 3] = fillRgb.a;

            const neighbors = [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]];
            for (const [nx, ny] of neighbors) {
                if (nx >= 0 && ny >= 0 && nx < width && ny < height) {
                    const vIdx = ny * width + nx;
                    const nIdx = vIdx * 4;

                    if (visited[vIdx] === 0) {
                        if (colorsMatch(nIdx)) {
                            visited[vIdx] = 1;
                            queue.push([nx, ny]);
                        }
                    }
                }
            }
        }

        ctx.putImageData(currentImageData, 0, 0);
        return {x: effectiveX, y: effectiveY};
    },


    resetMap: () => {
        if (window.mapTools.ctx && window.mapTools.originalImageData) {
            window.mapTools.ctx.putImageData(window.mapTools.originalImageData, 0, 0);
        }
    },

    getSize: (clientX, clientY) => {
        const canvas = window.mapTools.canvas;
        if (!canvas) return null;
        const wrapper = canvas.closest('.canvas-wrapper');
        if (!wrapper) return null;
        const wrapperRect = wrapper.getBoundingClientRect();
        const canvasRect = canvas.getBoundingClientRect();
        const offsetX_wrapper = clientX - wrapperRect.left;
        const offsetY_wrapper = clientY - wrapperRect.top;
        const scaleX = canvas.width / canvasRect.width;
        const scaleY = canvas.height / canvasRect.height;
        const realX = offsetX_wrapper * scaleX;
        const realY = offsetY_wrapper * scaleY;
        return { x: realX, y: realY };
    },

    getRealCoordinates: (clientX, clientY) => {
        const canvas = window.mapTools.canvas;
        if (!canvas) return null;

        const canvasRect = canvas.getBoundingClientRect();

        const offsetX = clientX - canvasRect.left;
        const offsetY = clientY - canvasRect.top;

      
        return { x: offsetX, y: offsetY };
    },

    getCanvasDataUrl: () => {
        const canvas = window.mapTools.canvas;
        if (!canvas) return null;
        return canvas.toDataURL('image/png', 1.0);
    },

    calculateAdjacency: (zones, borderThickness = 15) => {
        const canvas = window.mapTools.canvas;
        const ctx = window.mapTools.ctx;
        if (!canvas || !ctx || !zones || zones.length === 0) return {};

        const width = canvas.width;
        const height = canvas.height;
        const imgData = ctx.getImageData(0, 0, width, height).data;

        const zoneMap = new Int16Array(width * height).fill(-1);

        const qX = []; const qY = []; const qZone = []; const qDist = [];
        let head = 0;

        for (let i = 0; i < zones.length; i++) {
            const z = zones[i];
            let startX = Math.floor(z.x);
            let startY = Math.floor(z.y);
            const startIdx = (startY * width + startX) * 4;
            const sr = imgData[startIdx], sg = imgData[startIdx + 1], sb = imgData[startIdx + 2];

            const localQX = [startX]; const localQY = [startY];
            let localHead = 0;

            zoneMap[startY * width + startX] = i;
            qX.push(startX); qY.push(startY); qZone.push(i); qDist.push(0);

            while (localHead < localQX.length) {
                const cx = localQX[localHead];
                const cy = localQY[localHead];
                localHead++;

                const neighbors = [[cx + 1, cy], [cx - 1, cy], [cx, cy + 1], [cx, cy - 1]];
                for (let [nx, ny] of neighbors) {
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                        const idx = ny * width + nx;
                        if (zoneMap[idx] === -1) {
                            const px = idx * 4;
                            const diff = Math.abs(imgData[px] - sr) + Math.abs(imgData[px + 1] - sg) + Math.abs(imgData[px + 2] - sb);
                            if (diff < 60) {
                                zoneMap[idx] = i;
                                localQX.push(nx); localQY.push(ny);
                                qX.push(nx); qY.push(ny); qZone.push(i); qDist.push(0);
                            }
                        }
                    }
                }
            }
        }

        const adjacencyList = {};
        for (let z of zones) adjacencyList[z.name] = new Set();

        while (head < qX.length) {
            const cx = qX[head]; const cy = qY[head];
            const zoneIndex = qZone[head]; const dist = qDist[head];
            head++;

            if (dist >= borderThickness) continue;

            const neighbors = [[cx + 1, cy], [cx - 1, cy], [cx, cy + 1], [cx, cy - 1]];
            for (let [nx, ny] of neighbors) {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                    const idx = ny * width + nx;
                    const neighborZone = zoneMap[idx];

                    if (neighborZone === -1) {
                        zoneMap[idx] = zoneIndex;
                        qX.push(nx); qY.push(ny); qZone.push(zoneIndex); qDist.push(dist + 1);
                    } else if (neighborZone !== zoneIndex) {
                        adjacencyList[zones[zoneIndex].name].add(zones[neighborZone].name);
                        adjacencyList[zones[neighborZone].name].add(zones[zoneIndex].name);
                    }
                }
            }
        }

        const finalGraph = {};
        for (let key in adjacencyList) finalGraph[key] = Array.from(adjacencyList[key]);
        return finalGraph;
    },

    calculateAdjacency: (zones, borderThickness = 15) => {
        const canvas = window.mapTools.canvas;
        const ctx = window.mapTools.ctx;
        if (!canvas || !ctx || !zones || zones.length === 0) return {};

        const width = canvas.width;
        const height = canvas.height;
        const imgData = ctx.getImageData(0, 0, width, height).data;
        const zoneMap = new Int16Array(width * height).fill(-1);

        const qX = []; const qY = []; const qZone = []; const qDist = [];
        let head = 0;

        for (let i = 0; i < zones.length; i++) {
            let cx = Math.floor(zones[i].x);
            let cy = Math.floor(zones[i].y);

            const startIdx = (cy * width + cx) * 4;
            const sr = imgData[startIdx], sg = imgData[startIdx + 1], sb = imgData[startIdx + 2];

            const localQX = [cx]; const localQY = [cy];
            let localHead = 0;
            zoneMap[cy * width + cx] = i;

            qX.push(cx); qY.push(cy); qZone.push(i); qDist.push(0);

            while (localHead < localQX.length) {
                const currX = localQX[localHead];
                const currY = localQY[localHead];
                localHead++;

                const neighbors = [[currX + 1, currY], [currX - 1, currY], [currX, currY + 1], [currX, currY - 1]];
                for (let n = 0; n < neighbors.length; n++) {
                    const nx = neighbors[n][0];
                    const ny = neighbors[n][1];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                        const idx = ny * width + nx;
                        if (zoneMap[idx] === -1) {
                            const px = idx * 4;
                            const diff = Math.abs(imgData[px] - sr) + Math.abs(imgData[px+1] - sg) + Math.abs(imgData[px+2] - sb);
                            if (diff < 80) {
                                zoneMap[idx] = i;
                                localQX.push(nx); localQY.push(ny);
                                qX.push(nx); qY.push(ny); qZone.push(i); qDist.push(0);
                            }
                        }
                    }
                }
            }
        }

        const adjacencyList = {};
        for (let i = 0; i < zones.length; i++) {
            adjacencyList[zones[i].name] = new Set();
        }

        while (head < qX.length) {
            const cx = qX[head]; const cy = qY[head];
            const zoneIndex = qZone[head]; const dist = qDist[head];
            head++;

            if (dist >= borderThickness) continue;

            const neighbors = [[cx + 1, cy], [cx - 1, cy], [cx, cy + 1], [cx, cy - 1]];
            for (let n = 0; n < neighbors.length; n++) {
                const nx = neighbors[n][0]; const ny = neighbors[n][1];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                    const idx = ny * width + nx;
                    const neighborZone = zoneMap[idx];

                    if (neighborZone === -1) {
                        zoneMap[idx] = zoneIndex; 
                        qX.push(nx); qY.push(ny); qZone.push(zoneIndex); qDist.push(dist + 1);
                    } else if (neighborZone !== zoneIndex) {
                        adjacencyList[zones[zoneIndex].name].add(zones[neighborZone].name);
                        adjacencyList[zones[neighborZone].name].add(zones[zoneIndex].name);
                    }
                }
            }
        }

        const finalGraph = {};
        for (let key in adjacencyList) {
            finalGraph[key] = Array.from(adjacencyList[key]);
        }
        return finalGraph;
    },

    assignUnitsToZones: (units, zones) => {
        const canvas = window.mapTools.canvas;
        const ctx = window.mapTools.ctx;
        if (!canvas || !ctx || !zones || zones.length === 0 || !units || units.length === 0) return [];

        const width = canvas.width;
        const height = canvas.height;
        const imgData = ctx.getImageData(0, 0, width, height).data;

        const zoneMap = new Int32Array(width * height).fill(-1);
        
        const qX = [];
        const qY = [];
        const qZone = [];
        const startColors = [];

        for (let i = 0; i < zones.length; i++) {
            let cx = Math.floor(zones[i].x);
            let cy = Math.floor(zones[i].y);
            const startIdx = (cy * width + cx) * 4;

            startColors.push({
                r: imgData[startIdx],
                g: imgData[startIdx + 1],
                b: imgData[startIdx + 2]
            });

            zoneMap[cy * width + cx] = i;
            qX.push(cx);
            qY.push(cy);
            qZone.push(i);
        }

        let head = 0;
        while (head < qX.length) {
            const currX = qX[head];
            const currY = qY[head];
            const zId = qZone[head];
            head++;

            const sc = startColors[zId];
            const neighbors = [[currX + 1, currY], [currX - 1, currY], [currX, currY + 1], [currX, currY - 1]];

            for (let n = 0; n < neighbors.length; n++) {
                const nx = neighbors[n][0];
                const ny = neighbors[n][1];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                    const idx = ny * width + nx;

                    if (zoneMap[idx] === -1) {
                        const px = idx * 4;
                        const diff = Math.abs(imgData[px] - sc.r) + Math.abs(imgData[px+1] - sc.g) + Math.abs(imgData[px+2] - sc.b);

                        if (diff < 80) { 
                            zoneMap[idx] = zId;
                            qX.push(nx);
                            qY.push(ny);
                            qZone.push(zId);
                        }
                    }
                }
            }
        }

        const result = [];
        for (let i = 0; i < units.length; i++) {
            let ux = Math.floor(units[i].x !== undefined ? units[i].x : units[i].X);
            let uy = Math.floor(units[i].y !== undefined ? units[i].y : units[i].Y);
            let uId = units[i].id !== undefined ? units[i].id : units[i].Id;

            if (ux >= 0 && ux < width && uy >= 0 && uy < height) {
                let zIdx = zoneMap[uy * width + ux];

      
                if (zIdx === -1) {
                    let found = false;
                    for(let r = 1; r <= 15 && !found; r++) {
                        for(let dx = -r; dx <= r && !found; dx++) {
                            for(let dy = -r; dy <= r && !found; dy++) {
                                if (Math.abs(dx) !== r && Math.abs(dy) !== r) continue;
                                let nx = ux + dx, ny = uy + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height) {
                                    let nIdx = zoneMap[ny * width + nx];
                                    if (nIdx !== -1) {
                                        zIdx = nIdx;
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (zIdx !== -1 && uId !== undefined) {
                    result.push({ unitId: String(uId), zoneId: zIdx });
                }
            }
        }
        return result;
    }
};