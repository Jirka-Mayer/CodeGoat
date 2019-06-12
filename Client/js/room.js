const CodeMirror = require("codemirror/lib/codemirror.js")
require("codemirror/mode/javascript/javascript.js")
require("codemirror/addon/edit/matchbrackets.js")
require("codemirror/addon/edit/closebrackets.js")

require("codemirror/lib/codemirror.css")

let editor = CodeMirror(document.querySelector("#editor"), {
    lineNumbers: true,
    mode: { name: "javascript" },
    indentUnit: 4,
    indentWithTabs: false,
    viewportMargin: Infinity,
    autoCloseBrackets: true,
    matchBrackets: true,
    showCursorWhenSelecting: true
})

editor.on("change", (instance, change) => {
    console.log(change)
})
