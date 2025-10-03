export default class extends BlazorJSComponents.Component {
    setParameters(open) {
        document.body.classList.toggle("overflow-hidden", open)
    }
}