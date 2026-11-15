// Calendly owns a single popup and badge per browser page.
let popupOwner = null;
let badgeOwner = null;
let resizeOwner = null;
const containers = new WeakMap();
const events = new Set([
    "calendly.profile_page_viewed", "calendly.event_type_viewed",
    "calendly.date_and_time_selected", "calendly.event_scheduled", "calendly.page_height"
]);

export function create(dotnet) {
    const owner = {};
    const inline = new Map();
    let disposed = false;
    const ensureActive = () => { if (disposed) throw new Error("Calendly session is disposed."); };
    const optionsFrom = json => {
        const options = JSON.parse(json);
        const url = new URL(options.url);
        if (url.protocol !== "https:" || (url.hostname !== "calendly.com" && !url.hostname.endsWith(".calendly.com")) ||
            url.username || url.password || (url.port && url.port !== "443"))
            throw new Error("Expected an HTTPS Calendly scheduling URL.");
        return options;
    };
    const claimPopup = () => {
        if ((popupOwner && popupOwner !== owner) || (badgeOwner && badgeOwner !== owner))
            throw new Error("Another Calendly session owns the page's popup or badge.");
    };
    const matchesFrame = (frame, event) => {
        if (!frame || frame.contentWindow !== event.source) return false;
        try { return new URL(frame.src).origin === event.origin; } catch { return false; }
    };
    const onMessage = event => {
        if (disposed || !event.data || !events.has(event.data.event)) return;
        // Check both origin and source: unrelated Calendly frames must not trigger callbacks.
        let elementId = null;
        let matched = false;
        let inlineEntry = null;
        for (const [id, entry] of inline) {
            if (entry.element.isConnected && [...entry.element.querySelectorAll("iframe")].some(frame => matchesFrame(frame, event))) {
                elementId = id;
                inlineEntry = entry;
                matched = true;
                break;
            }
        }
        if (!matched && (popupOwner === owner || badgeOwner === owner))
            matched = [...document.querySelectorAll(".calendly-overlay iframe")].some(frame => matchesFrame(frame, event));
        if (!matched) return;
        if (event.data.event === "calendly.page_height") {
            const height = Number.parseFloat(event.data.payload?.height);
            if (inlineEntry?.resize && Number.isFinite(height) && height > 0 && height <= 100000)
                inlineEntry.element.style.height = `${height}px`;
            return;
        }
        dotnet.invokeMethodAsync("ReceiveEvent", event.data.event,
            JSON.stringify({ elementId, payload: event.data.payload ?? {} }))
            .catch(error => { if (!disposed) console.error("Calendly callback failed", error); });
    };
    window.addEventListener("message", onMessage);

    const destroyInline = id => {
        const entry = inline.get(id);
        if (!entry) return;
        entry.element.replaceChildren();
        for (const [name, value] of Object.entries(entry.attributes)) {
            if (value === null) entry.element.removeAttribute(name);
            else entry.element.setAttribute(name, value);
        }
        containers.delete(entry.element);
        inline.delete(id);
        if (resizeOwner === entry) resizeOwner = null;
    };
    const closePopup = () => {
        if (popupOwner === owner || badgeOwner === owner) window.Calendly.closePopupWidget();
        if (popupOwner === owner) popupOwner = null;
    };
    const destroyBadge = () => {
        if (badgeOwner !== owner) return;
        closePopup();
        window.Calendly.destroyBadgeWidget();
        badgeOwner = null;
    };
    return {
        createInline(id, json) {
            ensureActive();
            const options = optionsFrom(json);
            const element = document.getElementById(id);
            if (!element) throw new Error(`Calendly container '${id}' was not found.`);
            if (containers.has(element) && containers.get(element) !== owner)
                throw new Error("The container belongs to another Calendly session.");
            const previous = inline.get(id);
            if (options.resize && resizeOwner && resizeOwner !== previous)
                throw new Error("Only one auto-resizing Calendly embed is supported per page.");
            if (!previous && element.childNodes.length)
                throw new Error("Calendly requires an empty, dedicated container.");
            destroyInline(id);
            const entry = { element, resize: options.resize, attributes: {
                style: element.getAttribute("style"), class: element.getAttribute("class")
            } };
            inline.set(id, entry);
            containers.set(element, owner);
            if (options.resize) resizeOwner = entry;
            try { // Handle resize ourselves so its source is validated and its listener can be removed.
                window.Calendly.initInlineWidget({ ...options, resize: false, parentElement: element }); }
            catch (error) { destroyInline(id); throw error; }
        },
        destroyInline,
        openPopup(json) {
            ensureActive();
            const options = optionsFrom(json);
            claimPopup();
            closePopup();
            popupOwner = owner;
            try { window.Calendly.initPopupWidget(options); }
            catch (error) { popupOwner = null; throw error; }
        },
        closePopup,
        createBadge(json) {
            ensureActive();
            const options = optionsFrom(json);
            claimPopup();
            closePopup();
            destroyBadge();
            badgeOwner = owner;
            try { window.Calendly.initBadgeWidget(options); }
            catch (error) { badgeOwner = null; throw error; }
        },
        destroyBadge,
        dispose() {
            if (disposed) return;
            disposed = true;
            window.removeEventListener("message", onMessage);
            for (const id of [...inline.keys()]) destroyInline(id);
            closePopup();
            destroyBadge();
            dotnet = null;
        }
    };
}
