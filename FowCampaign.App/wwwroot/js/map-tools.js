window.mapTools = {
    canvas: null,
    ctx: null,
    img: null,
    
    initMap: (canvasId, imageSrc) => {
        const canvas = document.getElementById(canvasId);
        if(!canvas) return;
        
        const ctx = canvas.getContext('2d', {willReadFrequently: true});
        const img = new Image();
        
        img.onload = () => {
            canvas.width = img.width;
            canvas.height = img.height;
            ctx.drawImage(img, 0, 0)
            
            window.mapTools.canvas = canvas;
            window.mapTools.ctx = ctx;
            window.mapTools.img = img;
        };
        img.src = imageSrc;
    },
    
    floodFill: (startX, startY, fillColorHex) => {
        const ctx = window.mapTools.ctx;
        const canvas = window.mapTools.canvas;
        if (!ctx) return;
        
        const r = parseInt(fillColorHex.slice(1, 3), 16);
        const g = parseInt(fillColorHex.slice(3, 5), 16);
        const b = parseInt(fillColorHex.slice(5, 7), 16);
        const fillRgb = { r, g, b, a: 150};
        
        const imageData = ctx.getImageData(0,0, canvas.width, canvas.height);
        
        const data = imageData.data;
        
        const getPixelColor = (index) =>{
            return {r: data[index], g: data[index+1], b: data[index+2], a: data[index+3]};
        };
        
        const colorsMatch = (c1, c2, tolerance=50) =>{
            return Math.abs(c1.r - c2.r) < tolerance && 
                Math.abs(c1.g - c2.g) < tolerance && 
                Math.abs(c1.b - c2.b) < tolerance;
        };
        
        const stack = [[startX, startY]];
        const startPos = (startY * canvas.width + startX) * 4;
        const startColor = getPixelColor(startPos);
        
        if (colorsMatch(startColor, fillRgb)) return;
        
        while (stack.length) {
            const [x, y] = stack.pop();
            const pixelIndex = (y * canvas.width + x) * 4;
            
            if(x<0 || y<0 || x>= canvas.width || y>= canvas.height) continue;
            
            const currentColor = getPixelColor(pixelIndex);
            
            if (colorsMatch(currentColor, startColor))
            {
                data[pixelIndex] = fillRgb.r;
                data[pixelIndex+1] = fillRgb.g;
                data[pixelIndex+2] = fillRgb.b;
                data[pixelIndex+3] = fillRgb.a;
                
                stack.push([x+1, y]);
                stack.push([x-1, y]);
                stack.push([x, y+1]);
                stack.push([x, y-1]);
            }
            
        }
        
        ctx.putImageData(imageData, 0, 0);
        
    },
    
    resetMap: () =>
    {
        const ctx = window.mapTools.ctx;
        const img = window.mapTools.img;
        if (ctx && img) ctx.drawImage(img, 0, 0);
    }
    
};