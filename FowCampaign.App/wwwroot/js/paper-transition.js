

    const soundIn =  new Audio("/assets/sounds/paper-slide-in.mp3");

    export function dropIn(el) {
        if (!el) return;

        soundIn.currentTime = 0;
        soundIn.play().catch(() => {});

        gsap.fromTo(el, 
        { autoAlpha: 0, y: -800, scale: 1.2,rotation: 0,duration: 0.8, ease: "power3.out" },  
        { autoAlpha: 1, y: 0, scale: 1,rotation: 0,  }  
        );
    }

    export function tossOut(el) {
        return new Promise(resolve => {
            if (!el) {
                resolve();
                return;
            }

            soundIn.currentTime = 0;
            soundIn.play().catch(() => {});
            
            gsap.to(el, {
                duration: 0.5,
                x: 1500,
                opacity: 0,
                ease: "power2.in",
                onComplete: resolve
            });
        });
    }
