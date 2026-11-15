import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

async function setup() {
    const listeners = new Set();
    const elements = new Map();
    let popup = [];
    const calls = [];
    const frame = () => ({ src: 'https://calendly.com/demo', contentWindow: {} });
    const widget = {
        initInlineWidget(options) { calls.push(['inline', options]); options.parentElement.frames = [frame()]; },
        initPopupWidget(options) { calls.push(['popup', options]); popup = [frame()]; },
        closePopupWidget() { calls.push(['close']); popup = []; },
        initBadgeWidget(options) { calls.push(['badge', options]); },
        destroyBadgeWidget() { calls.push(['destroyBadge']); }
    };
    const context = vm.createContext({ URL, console, window: {
        Calendly: widget,
        addEventListener: (_, fn) => listeners.add(fn),
        removeEventListener: (_, fn) => listeners.delete(fn)
    }, document: {
        getElementById: id => elements.get(id),
        querySelectorAll: () => popup
    }});
    const source = await readFile(new URL('../../src/Soenneker.Calendly.Blazor/wwwroot/js/calendlyinterop.js', import.meta.url), 'utf8');
    vm.runInContext(source.replace('export function create', 'function create'), context);
    const received = [];
    const create = () => context.create({ invokeMethodAsync: (...args) => { received.push(args); return Promise.resolve(); } });
    const element = id => {
        const attrs = new Map([['style', 'height:700px']]);
        const result = { frames: [], style: { height: '700px' }, isConnected: true, childNodes: [],
            querySelectorAll() { return this.frames; },
            replaceChildren() { this.frames = []; this.childNodes = []; },
            getAttribute: name => attrs.get(name) ?? null,
            setAttribute: (name, value) => attrs.set(name, value),
            removeAttribute: name => attrs.delete(name)
        };
        elements.set(id, result); return result;
    };
    const emit = (source, origin = 'https://calendly.com', name = 'calendly.event_scheduled', payload = {}) => {
        for (const listener of listeners) listener({ source, origin, data: { event: name, payload } });
    };
    return { create, element, emit, received, listeners, calls, widget, popup: () => popup };
}
const options = extra => JSON.stringify({ url: 'https://calendly.com/demo', ...extra });

test('routes only supported messages from the owned frame and exact origin', async () => {
    const env = await setup();
    const session = env.create();
    const element = env.element('one');
    session.createInline('one', options({ prefill: { email: 'invitee@example.com' }, utm: { utmSource: 'demo' } }));
    const source = element.frames[0].contentWindow;
    env.emit({}, 'https://calendly.com');
    env.emit(source, 'https://evil.test');
    env.emit(source, 'https://calendly.com', 'calendly.unknown');
    assert.equal(env.received.length, 0);
    env.emit(source, 'https://calendly.com', 'calendly.event_scheduled', { event: { uri: 'booking' } });
    assert.equal(env.received.length, 1);
    assert.deepEqual(JSON.parse(env.received[0][2]), { elementId: 'one', payload: { event: { uri: 'booking' } } });
    assert.equal(env.calls[0][1].prefill.email, 'invitee@example.com');
    session.destroyInline('one');
    env.emit(source);
    assert.equal(env.received.length, 1);
    session.dispose();
    assert.equal(env.listeners.size, 0);
});

test('enforces ownership and releases inline, popup, badge, and resize resources', async () => {
    const env = await setup();
    const first = env.create(), second = env.create();
    const one = env.element('one'); env.element('two');
    first.createInline('one', options({ resize: true }));
    assert.throws(() => second.createInline('one', options()), /another/i);
    assert.throws(() => second.createInline('two', options({ resize: true })), /auto-resizing/);
    assert.equal(env.calls[0][1].resize, false);
    env.emit(one.frames[0].contentWindow, 'https://calendly.com', 'calendly.page_height', { height: '850px' });
    assert.equal(one.style.height, '850px');
    first.openPopup(options());
    env.emit(env.popup()[0].contentWindow);
    assert.equal(env.received.length, 1);
    assert.throws(() => second.openPopup(options()), /another/i);
    first.createBadge(options({ text: 'Book' }));
    assert.throws(() => second.createBadge(options()), /another/i);
    first.dispose(); first.dispose();
    second.createInline('two', options({ resize: true }));
    second.openPopup(options());
    second.createBadge(options());
    second.dispose();
    assert.equal(env.listeners.size, 0);
    assert.equal(one.frames.length, 0);
    assert.equal(env.popup().length, 0);
    assert.throws(() => first.openPopup(options()), /disposed/);
});

test('does not erase unrelated content and rolls back failed widget creation', async () => {
    const env = await setup();
    const first = env.create(), second = env.create();
    const element = env.element('one');
    element.childNodes.push({});
    assert.throws(() => first.createInline('one', options()), /empty/);
    assert.equal(element.childNodes.length, 1);
    element.childNodes = [];
    assert.throws(() => first.createInline('missing', options()), /not found/);
    assert.throws(() => first.openPopup(options({ url: 'https://calendly.com.evil.test/' })), /HTTPS/);
    env.widget.initInlineWidget = () => { throw new Error('SDK error'); };
    assert.throws(() => first.createInline('one', options({ resize: true })), /SDK error/);
    env.widget.initInlineWidget = () => {};
    second.createInline('one', options({ resize: true }));
    first.dispose(); second.dispose();
});
