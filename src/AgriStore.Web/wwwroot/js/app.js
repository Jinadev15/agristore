// AgriStore behaviour: header, navigation, buttons, toasts, forms and the command menu.
// Plain JavaScript, no framework. Everything here also works with motion turned off.
(() => {
    "use strict";
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const finePointer = window.matchMedia("(pointer: fine)").matches;

    // ---------- Header: hide on scroll down, return on scroll up ----------
    const header = document.querySelector(".site-header");
    if (header) {
        let lastY = window.scrollY;
        let ticking = false;
        window.addEventListener("scroll", () => {
            if (ticking) return;
            ticking = true;
            requestAnimationFrame(() => {
                const y = window.scrollY;
                const menuOpen = document.documentElement.classList.contains("nav-open");
                header.classList.toggle("is-scrolled", y > 8);
                header.classList.toggle("is-hidden", !menuOpen && y > 160 && y > lastY);
                lastY = y;
                ticking = false;
            });
        }, { passive: true });
        // Keyboard users tabbing into a hidden header bring it back.
        header.addEventListener("focusin", () => header.classList.remove("is-hidden"));
    }

    // ---------- Mobile menu ----------
    const toggle = document.querySelector(".nav-toggle");
    toggle?.addEventListener("click", () => {
        const open = document.documentElement.classList.toggle("nav-open");
        toggle.setAttribute("aria-expanded", String(open));
    });

    // ---------- Sliding underline under the active nav link ----------
    const nav = document.querySelector(".nav");
    const ink = nav?.querySelector(".nav-ink");
    if (nav && ink) {
        nav.classList.add("has-ink");
        const active = nav.querySelector("a.on");
        const moveTo = (link) => {
            if (!link) { ink.style.opacity = "0"; return; }
            const navBox = nav.getBoundingClientRect();
            const box = link.getBoundingClientRect();
            ink.style.opacity = "1";
            ink.style.width = `${box.width}px`;
            ink.style.transform = `translateX(${box.left - navBox.left}px)`;
        };
        moveTo(active);
        nav.querySelectorAll("a").forEach((a) => {
            a.addEventListener("pointerenter", () => moveTo(a));
            a.addEventListener("focus", () => moveTo(a));
        });
        nav.addEventListener("pointerleave", () => moveTo(active));
        window.addEventListener("resize", () => moveTo(active));
        document.fonts?.ready.then(() => moveTo(active));
    }

    // ---------- Buttons: the fill grows from where the pointer came in ----------
    document.addEventListener("pointerover", (e) => {
        const btn = e.target.closest?.(".btn");
        if (!btn || btn.contains(e.relatedTarget)) return;
        const box = btn.getBoundingClientRect();
        btn.style.setProperty("--x", `${e.clientX - box.left}px`);
        btn.style.setProperty("--y", `${e.clientY - box.top}px`);
        btn.style.setProperty("--sweep", String(Math.ceil((Math.hypot(box.width, box.height) * 2) / 10) + 2));
    });

    // Prevent double submits and show the button is working.
    document.addEventListener("submit", (e) => {
        const form = e.target;
        if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) { e.preventDefault(); return; }
        if (window.jQuery && window.jQuery(form).valid && !window.jQuery(form).valid()) return;
        const button = form.querySelector("button[type=submit], button:not([type])");
        if (button) {
            setTimeout(() => button.classList.add("is-busy"), 0);
            setTimeout(() => button.classList.remove("is-busy"), 8000); // never stuck if the page stays
        }
    });

    // ---------- Card spotlight follows the pointer ----------
    if (finePointer && !reduceMotion) {
        document.addEventListener("pointermove", (e) => {
            const card = e.target.closest?.(".spot");
            if (!card) return;
            const box = card.getBoundingClientRect();
            card.style.setProperty("--mx", `${e.clientX - box.left}px`);
            card.style.setProperty("--my", `${e.clientY - box.top}px`);
        }, { passive: true });
    }

    // ---------- Toasts ----------
    document.querySelectorAll(".toast").forEach((toast) => {
        const close = () => {
            toast.classList.add("is-leaving");
            toast.addEventListener("animationend", () => toast.remove(), { once: true });
        };
        toast.querySelector("button")?.addEventListener("click", close);
        let timer = setTimeout(close, 6000);
        toast.addEventListener("pointerenter", () => { clearTimeout(timer); toast.querySelector(".timer")?.style.setProperty("animation-play-state", "paused"); });
        toast.addEventListener("pointerleave", () => { timer = setTimeout(close, 2500); });
    });

    // ---------- Forms: shake fields the server rejected ----------
    const shake = (el) => {
        if (reduceMotion) return;
        el.classList.remove("shake");
        void el.offsetWidth;
        el.classList.add("shake");
    };
    document.querySelectorAll(".input-validation-error").forEach(shake);
    if (window.jQuery) {
        window.jQuery(document).on("invalid-form.validate", "form", function () {
            setTimeout(() => this.querySelectorAll(".input-validation-error").forEach(shake), 0);
        });
    }

    // ---------- Command menu (Ctrl+K / Cmd+K) ----------
    const dialog = document.getElementById("cmdk");
    if (dialog && typeof dialog.showModal === "function") {
        const input = dialog.querySelector("input");
        const list = dialog.querySelector("ul");
        const pages = [
            { group: "Pages", label: "Overview", href: "/Dashboard" },
            { group: "Pages", label: "Stock in storage", href: "/StockLots" },
            { group: "Pages", label: "Record a new lot", href: "/StockLots/Create" },
            { group: "Pages", label: "Storage units", href: "/StorageUnits" },
            { group: "Pages", label: "Sensor readings", href: "/Readings" },
            { group: "Pages", label: "Add a sensor reading", href: "/Readings/Create" },
            { group: "Pages", label: "Crop storage guide", href: "/Crops" },
            { group: "Pages", label: "API documentation", href: "/swagger" },
        ];
        let items = pages;
        let loaded = false;
        let selected = 0;

        const load = async () => {
            if (loaded) return;
            loaded = true;
            try {
                const [units, lots] = await Promise.all([
                    fetch("/api/units").then((r) => (r.ok ? r.json() : [])),
                    fetch("/api/lots/at-risk?minLevel=Low").then((r) => (r.ok ? r.json() : [])),
                ]);
                items = [
                    ...pages,
                    ...lots.map((l) => ({ group: "Stock lots", label: `${l.quantityTonnes} t ${l.crop.toLowerCase()}, ${l.farmer}`, hint: `${l.riskLevel} ${l.riskScore}`, href: `/StockLots/Details/${l.lotId}` })),
                    ...units.map((u) => ({ group: "Storage units", label: u.name, hint: u.location, href: `/StorageUnits/Details/${u.id}` })),
                ];
                render();
            } catch { /* offline: pages still work */ }
        };

        const escape = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
        const matches = () => {
            const q = input.value.trim().toLowerCase();
            if (!q) return items.slice(0, 30);
            return items.filter((i) => `${i.label} ${i.hint ?? ""} ${i.group}`.toLowerCase().includes(q)).slice(0, 30);
        };
        const render = () => {
            const found = matches();
            selected = Math.min(selected, Math.max(found.length - 1, 0));
            if (!found.length) { list.innerHTML = `<li class="none">Nothing matches "${escape(input.value)}".</li>`; return; }
            let html = "";
            let group = "";
            found.forEach((item, i) => {
                if (item.group !== group) { group = item.group; html += `<li class="group" role="presentation">${escape(group)}</li>`; }
                html += `<li><a href="${escape(item.href)}" role="option" aria-selected="${i === selected}" data-i="${i}">${escape(item.label)}${item.hint ? `<small>${escape(item.hint)}</small>` : ""}</a></li>`;
            });
            list.innerHTML = html;
            list.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: "nearest" });
        };
        const open = () => { dialog.showModal(); input.value = ""; selected = 0; render(); load(); input.focus(); };

        document.addEventListener("keydown", (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") { e.preventDefault(); dialog.open ? dialog.close() : open(); }
        });
        document.querySelectorAll("[data-cmdk]").forEach((b) => b.addEventListener("click", open));
        input.addEventListener("input", () => { selected = 0; render(); });
        input.addEventListener("keydown", (e) => {
            const count = matches().length;
            if (e.key === "ArrowDown") { e.preventDefault(); selected = (selected + 1) % Math.max(count, 1); render(); }
            if (e.key === "ArrowUp") { e.preventDefault(); selected = (selected - 1 + count) % Math.max(count, 1); render(); }
            if (e.key === "Enter") { e.preventDefault(); const link = list.querySelector('[aria-selected="true"]'); if (link) window.location.href = link.getAttribute("href"); }
        });
        dialog.addEventListener("click", (e) => { if (e.target === dialog) dialog.close(); });
    }
})();
