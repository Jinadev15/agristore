// AgriStore motion, built on GSAP + ScrollTrigger.
// Markup opts in with data attributes, so views never call animation code directly:
//   data-split        headline words rise out of masks
//   data-reveal       fades up when scrolled into view
//   data-stagger      children appear one after another
//   data-count="471"  number counts up (data-decimals, data-suffix optional)
//   data-bar-to="62"  bar grows to 62%
//   data-ring="84"    risk ring draws to 84/100
//   data-rain         monsoon rain canvas behind a band
//   data-magnetic     element leans towards the pointer
//   data-bg="#hex"    landing band that opens from an inset card to full width on scroll
(() => {
    "use strict";
    const root = document.documentElement;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const finePointer = window.matchMedia("(pointer: fine)").matches;
    const gsap = window.gsap;
    const ScrollTrigger = window.ScrollTrigger;

    const formatNumber = (value, decimals) =>
        value.toLocaleString("en-IN", { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

    // Without GSAP or with reduced motion: show final states and stop.
    const settle = () => {
        document.querySelectorAll("[data-count]").forEach((el) => {
            el.textContent = formatNumber(parseFloat(el.dataset.count), parseInt(el.dataset.decimals || "0", 10)) + (el.dataset.suffix || "");
        });
        document.querySelectorAll("[data-bar-to]").forEach((el) => (el.style.width = `${el.dataset.barTo}%`));
        document.querySelectorAll("[data-ring]").forEach((el) => setRing(el, parseFloat(el.dataset.ring)));
        root.classList.remove("js-motion", "first-visit");
    };

    function setRing(el, value) {
        const circle = el.querySelector(".value");
        if (!circle) return;
        const length = 2 * Math.PI * parseFloat(circle.getAttribute("r"));
        circle.style.strokeDasharray = `${length}`;
        circle.style.strokeDashoffset = `${length * (1 - Math.min(value, 100) / 100)}`;
    }

    // Rain runs even without GSAP, but never with reduced motion.
    const startRain = () => {
        if (reduceMotion) return;
        document.querySelectorAll("[data-rain]").forEach((band) => {
            const canvas = document.createElement("canvas");
            canvas.className = "rain-layer";
            canvas.setAttribute("aria-hidden", "true");
            band.prepend(canvas);
            const ctx = canvas.getContext("2d");
            let drops = [];
            let width = 0, height = 0, visible = true, lean = 0, targetLean = 0, frame = 0;

            const resize = () => {
                const ratio = Math.min(window.devicePixelRatio || 1, 2);
                width = band.clientWidth; height = band.clientHeight;
                canvas.width = width * ratio; canvas.height = height * ratio;
                canvas.style.width = `${width}px`; canvas.style.height = `${height}px`;
                ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
                const count = Math.round((width * height) / 9000);
                drops = Array.from({ length: count }, () => ({
                    x: Math.random() * width, y: Math.random() * height,
                    len: 10 + Math.random() * 22, speed: 5 + Math.random() * 9, alpha: .05 + Math.random() * .16,
                }));
            };
            const draw = () => {
                frame = 0;
                if (!visible) return;
                lean += (targetLean - lean) * .05;
                ctx.clearRect(0, 0, width, height);
                ctx.lineWidth = 1;
                for (const d of drops) {
                    const dx = (.22 + lean) * d.len;
                    ctx.strokeStyle = `rgba(232, 235, 244, ${d.alpha})`;
                    ctx.beginPath(); ctx.moveTo(d.x, d.y); ctx.lineTo(d.x - dx, d.y + d.len); ctx.stroke();
                    d.y += d.speed; d.x -= d.speed * (.22 + lean);
                    if (d.y > height) { d.y = -d.len; d.x = Math.random() * (width + 100); }
                    if (d.x < -40) d.x = width + 20;
                }
                frame = requestAnimationFrame(draw);
            };
            new ResizeObserver(resize).observe(band);
            new IntersectionObserver(([entry]) => {
                visible = entry.isIntersecting;
                if (visible && !frame) frame = requestAnimationFrame(draw);
            }).observe(band);
            if (finePointer) {
                band.addEventListener("pointermove", (e) => {
                    const box = band.getBoundingClientRect();
                    targetLean = ((e.clientX - box.left) / box.width - .5) * .5;
                });
            }
            resize();
            frame = requestAnimationFrame(draw);
        });
    };

    startRain();
    if (!gsap || !ScrollTrigger || reduceMotion) { settle(); return; }

    gsap.registerPlugin(ScrollTrigger);
    gsap.defaults({ ease: "expo.out", duration: 1 });
    root.classList.add("motion-ready");

    // ---------- Curtain on the first visit of a session ----------
    const curtain = document.querySelector(".curtain");
    let introDelay = 0;
    if (curtain && root.classList.contains("first-visit")) {
        introDelay = .9;
        gsap.timeline({ onComplete: () => { root.classList.remove("first-visit"); curtain.remove(); } })
            .from(curtain.querySelector("span"), { yPercent: 100, opacity: 0, duration: .6 })
            .to(curtain, { yPercent: -100, duration: 1.1, ease: "expo.inOut" }, "+=.2");
        // Never let the curtain block the page, even if animation frames are throttled.
        setTimeout(() => { if (document.contains(curtain)) { curtain.remove(); root.classList.remove("first-visit"); } }, 4000);
    }

    // ---------- Split headlines into masked words ----------
    document.querySelectorAll("[data-split]").forEach((el) => {
        const words = el.textContent.trim().split(/\s+/);
        el.setAttribute("aria-label", el.textContent.trim());
        el.innerHTML = words.map((w) => `<span class="split-word" aria-hidden="true"><span>${w.replace(/</g, "&lt;")}</span></span>`).join(" ");
        el.style.visibility = "visible";
        const inner = el.querySelectorAll(".split-word > span");
        const inView = el.getBoundingClientRect().top < window.innerHeight;
        gsap.from(inner, {
            yPercent: 115, rotate: 3, duration: 1.2, stagger: .07,
            delay: inView ? introDelay + .1 : 0,
            scrollTrigger: inView ? undefined : { trigger: el, start: "top 85%" },
        });
    });

    // ---------- Fade up on scroll ----------
    ScrollTrigger.batch("[data-reveal]", {
        start: "top 90%",
        once: true,
        onEnter: (batch) => gsap.fromTo(batch, { opacity: 0, y: 32 }, { opacity: 1, y: 0, stagger: .1, delay: introDelay ? introDelay + .4 : 0, overwrite: true }),
    });
    introDelay && setTimeout(() => (introDelay = 0), 1500);

    // ---------- Staggered children ----------
    document.querySelectorAll("[data-stagger]").forEach((group) => {
        gsap.fromTo(group.children, { opacity: 0, y: 24 }, {
            opacity: 1, y: 0, stagger: .08, duration: .9,
            scrollTrigger: { trigger: group, start: "top 88%", once: true },
        });
    });

    // ---------- Counters ----------
    document.querySelectorAll("[data-count]").forEach((el) => {
        const target = parseFloat(el.dataset.count);
        const decimals = parseInt(el.dataset.decimals || "0", 10);
        const suffix = el.dataset.suffix || "";
        const state = { v: 0 };
        el.textContent = formatNumber(0, decimals) + suffix;
        gsap.to(state, {
            v: target, duration: 2, ease: "power3.out",
            scrollTrigger: { trigger: el, start: "top 92%", once: true },
            onUpdate: () => (el.textContent = formatNumber(state.v, decimals) + suffix),
        });
    });

    // ---------- Bars ----------
    document.querySelectorAll("[data-bar-to]").forEach((el) => {
        gsap.fromTo(el, { width: "0%" }, {
            width: `${el.dataset.barTo}%`, duration: 1.4, ease: "power3.out",
            scrollTrigger: { trigger: el, start: "top 95%", once: true },
        });
    });

    // ---------- Risk ring ----------
    document.querySelectorAll("[data-ring]").forEach((el) => {
        const circle = el.querySelector(".value");
        const label = el.querySelector("b");
        const target = parseFloat(el.dataset.ring);
        const length = 2 * Math.PI * parseFloat(circle.getAttribute("r"));
        circle.style.strokeDasharray = `${length}`;
        const state = { v: 0 };
        gsap.to(state, {
            v: target, duration: 1.8, ease: "power3.out", delay: .2,
            onUpdate: () => {
                circle.style.strokeDashoffset = `${length * (1 - state.v / 100)}`;
                if (label) label.textContent = Math.round(state.v);
            },
        });
    });

    // ---------- Magnetic calls to action ----------
    if (finePointer) {
        document.querySelectorAll("[data-magnetic]").forEach((el) => {
            const xTo = gsap.quickTo(el, "x", { duration: .6, ease: "elastic.out(1, .4)" });
            const yTo = gsap.quickTo(el, "y", { duration: .6, ease: "elastic.out(1, .4)" });
            el.addEventListener("pointermove", (e) => {
                const box = el.getBoundingClientRect();
                xTo((e.clientX - box.left - box.width / 2) * .25);
                yTo((e.clientY - box.top - box.height / 2) * .35);
            });
            el.addEventListener("pointerleave", () => { xTo(0); yTo(0); });
        });
    }

    // ---------- Landing: each colour band opens from an inset card to full bleed as it arrives ----------
    const bands = [...document.querySelectorAll("[data-bg]")].slice(1);
    if (bands.length) {
        document.body.style.backgroundColor = "#161E45";
        bands.forEach((band) => {
            gsap.fromTo(band,
                { clipPath: "inset(0% 4% 0% 4% round 28px)" },
                { clipPath: "inset(0% 0% 0% 0% round 0px)", ease: "none", scrollTrigger: { trigger: band, start: "top bottom", end: "top 30%", scrub: .6 } });
        });
    }

    // ---------- Big number parallax on the story band ----------
    document.querySelectorAll("[data-drift]").forEach((el) => {
        gsap.fromTo(el, { yPercent: 12 }, { yPercent: -12, ease: "none", scrollTrigger: { trigger: el, start: "top bottom", end: "bottom top", scrub: true } });
    });

    // Let fonts settle before measuring triggers.
    document.fonts?.ready.then(() => ScrollTrigger.refresh());
})();
