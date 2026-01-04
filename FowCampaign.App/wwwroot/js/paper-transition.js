window.paperTransition = {
    
    soundIn: new Audio("/assets/sounds/paper-slide-in.mp3"),
    
    dropIn: (elementId) =>{
        const el = document.getElementById(elementId);
        if(!el) return;
        
        gsap.set(el, {clearProps: "all"});
        
        window.paperTransition.soundIn.currentTime = 0;
        window.paperTransition.soundIn.play().catch(e=>console.log("Audio error: ", e))
        
        gsap.from(el,{
            duration: 0.8,
            y: -800,
            rotation: 0,
            opacity: 0,
            scale: 1.2,
            ease: "power3.out",
            delay: 0.1
        });
        
        gsap.to(el, {
            duration: 0.8,
            rotation: 0,
            ease: "power3.out",
            delay: 0.1
        });
    },
    
    tossOut: (elementId) =>{
        return new Promise((resolve) => {
            const el = document.getElementById(elementId);

            window.paperTransition.soundIn.currentTime = 0;
            window.paperTransition.soundIn.play().catch(e => console.log("Audio error: ", e));
            
            if(!el) {resolve(); return;}
            
            gsap.to(el, {
                duration: 0.5,
                x: 1500,
                y: 50,
                rotation: 0,
                opacity: 0,
                ease: "power2.in",
                onComplete: resolve
            });
        });
    }
};