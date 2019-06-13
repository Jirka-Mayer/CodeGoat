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






let socket = new WebSocket("ws://" + window.location.hostname + ":" + CodeGoatConfig.webSocketPort)
window.socket = socket

socket.addEventListener("open", (e) => {
    editor.setValue(editor.getValue() + "Socket open.\n")

    let message = {
        type: "join-room",
        room: CodeGoatConfig.roomId
    }
    
    socket.send(JSON.stringify(message));
});

socket.addEventListener("message", (e) => {
    editor.setValue(editor.getValue() + "Server sent: " + e.data + "\n")
    //console.log(JSON.parse(e.data))
});

socket.addEventListener("error", (e) => {
    alert("WebSocket connection failed...")
    console.error(e)
});

socket.addEventListener("close", (e) => {
    editor.setValue(editor.getValue() + "Socket closed.\n")
});
