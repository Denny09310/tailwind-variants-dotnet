export function init() {
    addCopyButtons();
    highlightAll();
}
export function dispose() {
}

/* --- helper: copy buttons --- */
function highlightAll() {
    if (window.Prism && typeof window.Prism.highlightAll === 'function') {
        window.Prism.highlightAll();
    }
    else {
        setTimeout(() => {
            highlightAll();
        }, 250);
    }
}

function addCopyButtons() {
    document.querySelectorAll('pre > code').forEach(codeEl => {
        const pre = codeEl.parentElement;
        if (!pre) return;

        // Optionally enable line numbers by uncommenting:
         pre.classList.add('line-numbers');

        if (pre.querySelector('.docs-copy-btn')) return; // already added

        // ensure pre is positioned
        const cs = window.getComputedStyle(pre);
        if (cs.position === 'static') {
            pre.style.position = 'relative';
        }

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'docs-copy-btn';
        btn.setAttribute('aria-label', 'Copy code');
        btn.textContent = 'Copy';

        btn.addEventListener('click', (e) => {
            e.preventDefault();
            navigator.clipboard.writeText(codeEl.textContent || '')
                .then(() => showTransient(btn, 'Copied!'))
                .catch(() => showTransient(btn, 'Failed'));
        });

        // Minimal inline styles so it's usable until site CSS is applied.
        btn.style.position = 'absolute';
        btn.style.top = '8px';
        btn.style.right = '8px';
        btn.style.padding = '4px 8px';
        btn.style.fontSize = '12px';
        btn.style.borderRadius = '6px';
        btn.style.cursor = 'pointer';
        btn.style.zIndex = '20';
        pre.appendChild(btn);
    });
}

function showTransient(btn, text) {
    const orig = btn.textContent;
    btn.textContent = text;
    setTimeout(() => {
        btn.textContent = orig;
    }, 1600);
}