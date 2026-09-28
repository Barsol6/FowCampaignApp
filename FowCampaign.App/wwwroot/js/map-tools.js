window.mapTools = {
    canvas: null,
    ctx: null,
    img: null,
    originalImageData: null,
    zoneLookup: null,
    zoneLookupNames: [],
    zoneLookupSeeds: [],

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
                window.mapTools.zoneLookup = null;
                window.mapTools.zoneLookupNames = [];
                window.mapTools.zoneLookupSeeds = [];
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
        window.mapTools.zoneLookup = null;
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

    buildZoneLookup: (zones) => {
        const canvas = window.mapTools.canvas;
        const ctx = window.mapTools.ctx;
        if (!canvas || !ctx || !zones) return;

        const width = canvas.width;
        const height = canvas.height;
        const pixels = window.mapTools.originalImageData?.data ?? ctx.getImageData(0, 0, width, height).data;
        const lookup = new Int32Array(width * height).fill(-1);
        const queue = [];
        const colors = [];

        zones.forEach((zone, index) => {
            const x = Math.floor(zone.x);
            const y = Math.floor(zone.y);
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            const pixel = (y * width + x) * 4;
            colors[index] = [pixels[pixel], pixels[pixel + 1], pixels[pixel + 2]];
            const point = y * width + x;
            if (lookup[point] !== -1) return;
            lookup[point] = index;
            queue.push(point);
        });

        for (let head = 0; head < queue.length; head++) {
            const point = queue[head];
            const zoneId = lookup[point];
            const color = colors[zoneId];
            const x = point % width;
            const y = Math.floor(point / width);
            const neighbors = [];
            if (x > 0) neighbors.push(point - 1);
            if (x + 1 < width) neighbors.push(point + 1);
            if (y > 0) neighbors.push(point - width);
            if (y + 1 < height) neighbors.push(point + width);

            for (const next of neighbors) {
                if (lookup[next] !== -1) continue;
                const pixel = next * 4;
                const difference = Math.abs(pixels[pixel] - color[0]) +
                    Math.abs(pixels[pixel + 1] - color[1]) +
                    Math.abs(pixels[pixel + 2] - color[2]);
                if (difference >= 80) continue;
                lookup[next] = zoneId;
                queue.push(next);
            }
        }

        window.mapTools.zoneLookup = lookup;
        window.mapTools.zoneLookupNames = zones.map(zone => zone.name);
        window.mapTools.zoneLookupSeeds = zones.map(zone => ({x: zone.x, y: zone.y}));
    },

    findPlacementInZone: (zoneName, units) => {
        const canvas = window.mapTools.canvas;
        const lookup = window.mapTools.zoneLookup;
        const zoneId = window.mapTools.zoneLookupNames.indexOf(zoneName);
        const seed = window.mapTools.zoneLookupSeeds[zoneId];
        if (!canvas || !lookup || !seed) return null;

        const occupied = units || [];
        for (let radius = 0; radius <= 240; radius += 32) {
            const points = radius === 0 ? 1 : Math.max(8, Math.ceil(2 * Math.PI * radius / 32));
            for (let point = 0; point < points; point++) {
                const angle = 2 * Math.PI * point / points;
                const x = Math.round(seed.x + radius * Math.cos(angle));
                const y = Math.round(seed.y + radius * Math.sin(angle));
                if (x < 0 || y < 0 || x >= canvas.width || y >= canvas.height) continue;
                if (lookup[y * canvas.width + x] !== zoneId) continue;
                if (occupied.some(unit => Math.hypot(unit.x - x, unit.y - y) < 32)) continue;
                return {x, y};
            }
        }
        return {x: seed.x, y: seed.y};
    },

    getHoveredZone: (clientX, clientY) => {
        const canvas = window.mapTools.canvas;
        const lookup = window.mapTools.zoneLookup;
        if (!canvas || !lookup) return null;
        const canvasRect = canvas.getBoundingClientRect();
        const wrapperRect = canvas.parentElement.getBoundingClientRect();
        const x = Math.floor((clientX - canvasRect.left) * canvas.width / canvasRect.width);
        const y = Math.floor((clientY - canvasRect.top) * canvas.height / canvasRect.height);
        if (x < 0 || y < 0 || x >= canvas.width || y >= canvas.height) return null;

        let zoneId = lookup[y * canvas.width + x];
        if (zoneId === -1) {
            for (let radius = 1; radius <= 8 && zoneId === -1; radius++) {
                for (let dy = -radius; dy <= radius && zoneId === -1; dy++) {
                    for (let dx = -radius; dx <= radius && zoneId === -1; dx++) {
                        if (Math.abs(dx) !== radius && Math.abs(dy) !== radius) continue;
                        const nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < canvas.width && ny < canvas.height)
                            zoneId = lookup[ny * canvas.width + nx];
                    }
                }
            }
        }
        if (zoneId < 0) return null;
        return {name: window.mapTools.zoneLookupNames[zoneId], x: clientX - wrapperRect.left, y: clientY - wrapperRect.top};
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
