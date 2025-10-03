import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/prism.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-bash.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-csharp.min.js";
import "https://cdn.jsdelivr.net/npm/prismjs@1.30.0/components/prism-cshtml.min.js";

import "https://cdn.jsdelivr.net/npm/anchor-js/anchor.min.js";

export default class extends BlazorJSComponents.Component {
    attach() {
        anchors.add('h2');
        window.Prism.highlightAll();
        this.addDecorations();
    }

    dispose() {
        document.querySelectorAll('.code-block').forEach(wrapper => {
            wrapper.remove();
        });
    }

    addDecorations() {
        document.querySelectorAll('pre > code').forEach(codeEl => {
            const pre = codeEl.parentElement;
            if (!pre) return;

            // check if already wrapped
            if (pre.parentElement?.classList.contains('code-block')) return;

            // create wrapper
            const wrapper = document.createElement('div');
            wrapper.className = 'code-block';
            wrapper.style.position = 'relative';

            // insert wrapper before <pre> and move pre inside it
            pre.parentNode.insertBefore(wrapper, pre);
            wrapper.appendChild(pre);

            // create button
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

            var isSingleLine = codeEl.textContent?.split('\n').length <= 2;
            if (isSingleLine) {
                btn.style.top = '50%';
                btn.style.transform = 'translateY(-50%)';
            }
            else {
                btn.style.top = '8px';
            }

            wrapper.appendChild(btn);
        });
    }

    showTransient(btn, text) {
        const orig = btn.textContent;
        btn.textContent = text;
        setTimeout(() => btn.textContent = orig, 1600);
    }
}