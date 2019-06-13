const CodeMirror = require("codemirror/lib/codemirror.js")
require("codemirror/mode/javascript/javascript.js")
require("codemirror/addon/edit/matchbrackets.js")
require("codemirror/addon/edit/closebrackets.js")

require("codemirror/lib/codemirror.css")

/**
 * The CodeMirror text editor
 */
class Editor
{
    constructor(elementSelector)
    {
        this.cm = CodeMirror(document.querySelector(elementSelector), {
            lineNumbers: true,
            mode: { name: "javascript" },
            indentUnit: 4,
            indentWithTabs: false,
            viewportMargin: Infinity,
            autoCloseBrackets: true,
            matchBrackets: true,
            showCursorWhenSelecting: true
        })
    }

    registerOnChangeListener(listener)
    {
        this.cm.on("change", (instance, change) => {
            listener(change)
        })
    }
}

module.exports = Editor
