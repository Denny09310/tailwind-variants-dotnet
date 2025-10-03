export default class extends BlazorJSComponents.Component {
    setParameters({ header }) {
        this.header = header;
        this.apply();
        this.setEventListener(window, 'scroll', this.apply);
    }
    apply() {
        if (window.scrollY > 0) {
            this.header.classList.add('backdrop-blur', 'border-b', 'border-gray-200', 'bg-gray-50/70', 'dark:bg-neutral-900/70', 'dark:border-neutral-700');
        } else {
            this.header.classList.remove('backdrop-blur', 'border-b', 'border-gray-200', 'bg-gray-50/70', 'dark:bg-neutral-900/70', 'dark:border-neutral-700');
        }
    };
}