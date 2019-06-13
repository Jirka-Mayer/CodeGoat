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

    if (!window.socket)
        return

    if (window.socket.readyState == window.socket.OPEN)
    {
        window.socket.send(JSON.stringify({
            type: "change",
            change: change
        }))
    }
})

window.editor = editor





let socket = new WebSocket("ws://" + window.location.hostname + ":" + CodeGoatConfig.webSocketPort)
window.socket = socket

socket.addEventListener("open", (e) => {
    editor.setValue(editor.getValue() + "Socket open.\n")

    let message = {
        type: "join-room",
        room: CodeGoatConfig.roomId
    }

    socket.send(JSON.stringify(message))
});

socket.addEventListener("message", (e) => {
    
    let message = JSON.parse(e.data)
    
    switch (message.type)
    {
        case "document-state":
            editor.setValue(message.document)
            if (message.initial)
                editor.clearHistory()
            break

        default:
            editor.setValue(editor.getValue() + "Server sent: " + e.data + "\n")
            break
    }
    
    console.log(message)
});

socket.addEventListener("error", (e) => {
    alert("WebSocket connection failed...")
    console.error(e)
});

socket.addEventListener("close", (e) => {
    editor.setValue(editor.getValue() + "Socket closed.\n")
});
