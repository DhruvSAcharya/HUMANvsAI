window.initScrollSnapOnScroll = () => {
    const container = document.getElementById('scrollContainer');
    const sections = container.querySelectorAll('.snap-section');
    const headerOffset = 4.5 * 16; // 4.5rem = 72px

    let isSnapping = false;

    container.addEventListener('scroll', () => {
        if (isSnapping) return;

        clearTimeout(container.snapTimeout);
        container.snapTimeout = setTimeout(() => {
            const containerTop = container.scrollTop;
            let closest = null;
            let minDist = Infinity;

            sections.forEach(section => {
                const offset = section.offsetTop;
                const distance = Math.abs(offset - containerTop);
                if (distance < minDist) {
                    minDist = distance;
                    closest = section;
                }
            });

            if (closest) {
                isSnapping = true;
                container.scrollTo({
                    top: closest.offsetTop - headerOffset,
                    behavior: 'smooth'
                });
                setTimeout(() => isSnapping = false, 500); // prevent multiple triggers
            }
        }, 100); // debounce delay
    });
};


window.registerOnUnload = function (dotNetRef) {
    window.addEventListener('beforeunload', function () {
        dotNetRef.invokeMethodAsync('OnBrowserUnload');
    });
};

window.scrollToBottom = function (elementId) {
    var elem = document.getElementById(elementId);
    if (elem) {
        elem.scrollTop = elem.scrollHeight;
    }
};
