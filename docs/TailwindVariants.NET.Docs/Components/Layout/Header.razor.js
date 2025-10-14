export default class extends BlazorJSComponents.Component {
	attach() {
		this._threshold = 10;
		this._update = this.update.bind(this);
	}

	setParameters({ header }) {
		this._header = header;

		this._header.classList.add('bg-transparent', 'dark:bg-transparent');

		this.setEventListener(window, 'scroll', this._update, { passive: true });
		this.setEventListener(window, 'resize', this._update);
		this.setEventListener(document, 'DOMContentLoaded', this._update);

		if (window.matchMedia) {
			this._mql = window.matchMedia('(prefers-color-scheme: dark)');
			try {
				this.setEventListener(this._mql, 'change', this._update);
			} catch (e) {
				this.setEventListener(this._mql, this._update);
			}
		}

		this._observer = new MutationObserver(this._update);
		this._observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

		this.update();
	}

	update() {
		const scrolled = window.scrollY > this._threshold;

		if (scrolled) {
			this._header.classList.add(
				'bg-white/60',
				'dark:bg-neutral-900/60',
				'backdrop-blur-sm',
				'shadow-sm'
			);
			this._header.classList.remove('bg-transparent', 'dark:bg-transparent');
		} else {
			this._header.classList.remove(
				'bg-white/60',
				'dark:bg-neutral-900/60',
				'backdrop-blur-sm',
				'shadow-sm'
			);
			this._header.classList.add('bg-transparent', 'dark:bg-transparent');
		}
	}

	dispose() {
		if (this._observer) {
			this._observer.disconnect();
			this._observer = null;
		}
	}
}
