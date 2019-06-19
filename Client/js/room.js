const Editor = require("./editor.js")
const invertChange = require("./invertChange.js")
const changesEqual = require("./changesEqual.js")
const orderPositions = require("./orderPositions.js")

/**
 * Main controller for the web page
 */
class MainController
{
    constructor(debug)
    {
        /**
         * Are we in debug mode?
         * So log every single minor thing
         */
        this.DEBUG = !!debug;

        /**
         * The text editor
         */
        this.editor = new Editor("#editor")

        /**
         * The web socket connection
         */
        this.socket = new WebSocket("ws://" + window.location.hostname + ":" + CodeGoatConfig.webSocketPort)
        
        /**
         * Is the socket connected and we are inside a room?
         */
        this.isConnected = false

        /**
         * List of speculative changes
         * The oldest changes are first, newest last
         */
        this.speculativeChanges = []

        /**
         * For each client id there's a list of markers defining their selection
         */
        this.selectionMarkers = {}

        this.editor.registerOnChangeListener(this.onEditorChange.bind(this))
        this.editor.registerOnSelectionListener(this.onEditorSelection.bind(this))
        
        this.socket.addEventListener("open", (e) => this.onSocketOpen())
        this.socket.addEventListener("close", (e) => this.onSocketClose())
        this.socket.addEventListener("error", this.onSocketError.bind(this))
        this.socket.addEventListener("message", (e) => this.onSocketMessage(JSON.parse(e.data)))
    }

    onSocketOpen()
    {
        if (this.DEBUG)
            console.log("Socket openned.")

        this.socket.send(JSON.stringify({
            type: "join-room",
            room: CodeGoatConfig.roomId
        }))
    }

    onSocketClose()
    {
        this.isConnected = false

        if (this.DEBUG)
            console.log("Socket has been closed.")
    }

    onSocketError(e)
    {
        this.isConnected = false

        alert("WebSocket connection failed...")
        console.error(e)
    }

    onSocketMessage(message)
    {
        if (this.DEBUG)
            console.log("Received message:", message)

        switch (message.type)
        {
            case "document-state":
                if (message.initial)
                {
                    if (this.DEBUG)
                        console.log("Room has been joined.")
                    
                    this.setupInitialDocument(message.document)
                    this.isConnected = true // now we are inside a room
                }
                else
                {
                    this.handleDocumentStateBroadcast(message.document)
                }
                break

            case "change-broadcast":
                if (message.familiar)
                    this.familiarChangeBroadcastReceived(message.change)
                else
                    this.foreignChangeBroadcastReceived(message.change)
                break

            case "selection-broadcast":
                this.handleSelectionBroadcast(message.clientId, message.selection)
                break

            default:
                console.error("Server sent message that wasn't understood:", message)
                break
        }
    }

    /**
     * Setup the document content right after the connection succeeded
     */
    setupInitialDocument(content)
    {
        this.speculativeChanges = []

        this.editor.cm.replaceRange(
            content,
            {line: 0, ch: 0},
            {line: this.editor.cm.lineCount(), ch: null},
            "*server"
        )

        this.editor.cm.clearHistory()
    }

    /**
     * Update document state
     * We received a document state broadcast to fix any possible errors that might have accumulated
     */
    handleDocumentStateBroadcast(content)
    {
        if (this.DEBUG)
            console.log("Received document state broadcast.")

        this.speculativeChanges = []

        if (content != this.editor.cm.getValue())
        {
            this.editor.cm.replaceRange(
                content,
                {line: 0, ch: 0},
                {line: this.editor.cm.lineCount(), ch: null},
                "*server"
            )

            console.warn("Document state broadcast has made some changes.")
        }
    }

    /**
     * Change broadcast by some foreign client has been received
     * (it's not our change so it's foreign)
     */
    foreignChangeBroadcastReceived(change)
    {
        // rollback speculative changes
        for (let i = this.speculativeChanges.length - 1; i >= 0; i--)
        {
            this.applyChange(
                invertChange(this.speculativeChanges[i]),
                "*server"
            )
        }

        // apply foreign change
        this.applyChange(change, "*server")

        // re-apply speculative changes
        for (let i = 0; i < this.speculativeChanges.length; i++)
        {
            this.applyChange(
                this.speculativeChanges[i],
                "*server"
            )
        }
    }

    /**
     * Change broadcast has been received that we have initiated in the past
     * (so the change is familiar since we initiated it)
     */
    familiarChangeBroadcastReceived(change)
    {
        if (this.speculativeChanges.length == 0)
        {
            console.warn("No speculative changes yet we received a familiar change.")
            console.warn("Maybe document broadcast happened? Applying the change.")
            this.foreignChangeBroadcastReceived(change)
            return
        }

        if (!changesEqual(change, this.speculativeChanges[0]))
        {
            console.error("Received familiar change that is not the first speculative one.")
            console.error("Last speculative:", this.speculativeChanges[0])
            console.error("Received change:", change)
            this.requestDocumentBroadcast()
            return
        }

        // the change is not speculative anymore
        this.speculativeChanges.splice(0, 1)
    }

    /**
     * Applies a change object to the editor on behalf some origin
     */
    applyChange(change, origin)
    {
        if (this.editor.cm.getRange(change.from, change.to) != change.removed.join("\n"))
        {
            console.warn("Applying a change that removed different text at source client than is being removed now.")
            this.requestDocumentBroadcast()
        }

        this.editor.cm.replaceRange(
            change.text.join("\n"),
            change.from,
            change.to,
            origin
        )
    }

    /**
     * Called when a selection broadcast is received
     * (someone else changed their selection)
     */
    handleSelectionBroadcast(clientId, selection)
    {
        // clear old markers

        if (!this.selectionMarkers[clientId])
            this.selectionMarkers[clientId] = []

        for (let i = 0; i < this.selectionMarkers[clientId].length; i++)
            this.selectionMarkers[clientId][i].clear()

        this.selectionMarkers[clientId] = []

        // insert new markers

        for (let i = 0; i < selection.ranges.length; i++)
        {
            let range = selection.ranges[i];

            let orderedRange = orderPositions(range.head, range.anchor)
            let from = orderedRange[0]
            let to = orderedRange[1]

            if (from.line == to.line && from.ch == to.ch)
            {
                // Caret

                let cursorCoords = this.editor.cm.cursorCoords(from)
                let elem = document.createElement("span")
                elem.style.borderLeftStyle = 'solid'
                elem.style.borderLeftWidth = '2px'
                elem.style.borderLeftColor = 'tomato'
                elem.style.height = (cursorCoords.bottom - cursorCoords.top) + "px"
                elem.style.padding = 0
                elem.style.marginLeft = "-2px"
                elem.style.zIndex = 0

                let marker = this.editor.cm.setBookmark(from, { widget: elem })

                this.selectionMarkers[clientId].push(marker)
            }
            else
            {
                // Selection

                let marker = this.editor.cm.markText(from, to, {
                    inclusiveRight: true,
                    inclusiveLeft: false,
                    css: `background: tomato`
                })
                
                this.selectionMarkers[clientId].push(marker)
            }
        }
    }

    /**
     * Called when the text editor change event fires
     */
    onEditorChange(change)
    {
        if (this.DEBUG)
            console.log("Editor change:", change)

        if (!this.isConnected)
            return

        if (change.origin.match(/server$/))
            return
    
        this.speculativeChanges.push(change)
        this.socket.send(JSON.stringify({
            type: "change",
            change: change
        }))

        // selection
        this.socket.send(JSON.stringify({
            type: "selection-change",
            selection: {
                ranges: this.editor.cm.listSelections()
            }
        }))
    }

    /**
     * Called when the editor selection changes
     */
    onEditorSelection(selection)
    {
        if (this.DEBUG)
            console.log("Selection change:", selection)

        this.socket.send(JSON.stringify({
            type: "selection-change",
            selection: selection
        }))
    }

    /**
     * Request a document broadcast, because something is going off the rails
     */
    requestDocumentBroadcast()
    {
        console.warn("Requesting document broadcast.")

        this.socket.send(JSON.stringify({
            type: "request-document-broadcast"
        }))
    }

    /**
     * Call this from the console to run the editor-dependant javascript tests
     */
    runTests()
    {
        require("./roomTests.js")(this)
    }
}

// MAIN
window.mainController = new MainController(true)
