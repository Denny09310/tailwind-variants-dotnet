import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/prism.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-bash.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-csharp.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-cshtml.min.js";

export default class extends BlazorJSComponents.Component {
    attach() {
        this.addCopyButtons();
        this.highlightAll();
    }

    highlightAll() {
        if (window.Prism && typeof window.Prism.highlightAll === 'function') {
            window.Prism.highlightAll();
        } else {
            setTimeout(() => this.highlightAll(), 250);
        }
    }

    addCopyButtons() {
        document.querySelectorAll('pre > code').forEach(codeEl => {
            const pre = codeEl.parentElement;
            if (!pre) return;

            // optional: enable line numbers:
            // pre.classList.add('line-numbers');

            if (pre.querySelector('.docs-copy-btn')) return;

            // ensure pre is positioned
            const cs = window.getComputedStyle(pre);
            if (cs.position === 'static') pre.style.position = 'relative';

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'docs-copy-btn';
            btn.setAttribute('aria-label', 'Copy code');
            btn.textContent = 'Copy';

            btn.addEventListener('click', (e) => {
                e.preventDefault();
                navigator.clipboard.writeText(codeEl.textContent || '')
                    .then(() => this.showTransient(btn, 'Copied!'))
                    .catch(() => this.showTransient(btn, 'Failed'));
            });

            // minimal inline styles so it works before CSS loads
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

    showTransient(btn, text) {
        const orig = btn.textContent;
        btn.textContent = text;
        setTimeout(() => btn.textContent = orig, 1600);
    }

}