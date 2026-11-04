// Called from Blazor via JS interop (AlertSoundService) — plan.md §6.2.
// Real audio files instead of Web Audio tones, so it sounds like an actual alarm on tablet/external speakers.
window.forkliftSound = (function () {
    const elements = {};

    function elementFor(name) {
        if (!elements[name]) {
            const el = new Audio(`sounds/${name}.wav`);
            el.preload = "auto";
            elements[name] = el;
        }
        return elements[name];
    }

    return {
        play: function (name) {
            try {
                const el = elementFor(name);
                el.currentTime = 0;
                el.play().catch(() => {
                    // Autoplay blocked until the first user gesture on this tablet — expected on cold boot.
                });
            } catch (e) {
                console.warn("forkliftSound.play failed", e);
            }
        }
    };
})();
