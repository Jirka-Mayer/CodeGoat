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








let socket = new WebSocket("ws://localhost:8181")
window.socket = socket

socket.addEventListener("open", (e) => {
    console.log("WebSocket connection established.")

    socket.send("Hello server!");
});

socket.addEventListener("message", (e) => {
    console.log(e)
    //console.log(JSON.parse(e.data))
});
