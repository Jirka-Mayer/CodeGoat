const Editor = require("./editor.js")
const invertChange = require("./invertChange.js")
const changesEqual = require("./changesEqual.js")

/**
 * Main controller for the web page
 */
class MainController
{
    constructor()
    {
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

        this.editor.registerOnChangeListener(this.onEditorChange.bind(this))
        this.editor.registerOnSelectionListener(this.onEditorSelection.bind(this))
        
        this.socket.addEventListener("open", (e) => this.onSocketOpen())
        this.socket.addEventListener("close", (e) => this.onSocketClose())
        this.socket.addEventListener("error", this.onSocketError.bind(this))
        this.socket.addEventListener("message", (e) => this.onSocketMessage(JSON.parse(e.data)))
    }

    onSocketOpen()
    {
        console.log("Socket openned.")

        this.socket.send(JSON.stringify({
            type: "join-room",
            room: CodeGoatConfig.roomId
        }))
    }

    onSocketClose()
    {
        this.isConnected = false

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
        switch (message.type)
        {
            case "document-state":
                if (message.initial)
                {
                    console.log("Room has been joined.")
                    
                    this.setupInitialDocument(message.document)
                    this.isConnected = true // now we are inside a room
                }
                else
                {
                    console.log("Received document state broadcast.")

                    this.handleDocumentStateBroadcast(message.document)
                }
                break

            case "change-broadcast":
                if (message.familiar)
                    this.familiarChangeBroadcastReceived(message.change)
                else
                    this.foreignChangeBroadcastReceived(message.change)
                break

            default:
                console.error("Server sent message that wasn't understood:", message)
                break
        }
        
        console.log("Received:", message)
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
     * Called when the text editor change event fires
     */
    onEditorChange(change)
    {
        console.log("Change:", change)

        if (!this.isConnected)
            return

        if (change.origin.match(/server$/))
            return
    
        this.speculativeChanges.push(change)
        this.socket.send(JSON.stringify({
            type: "change",
            change: change
        }))
    }

    /**
     * Called when the editor selection changes
     */
    onEditorSelection(selection)
    {
        // TODO

        //console.log(selection)
    }

    /**
     * Request a document broadcast, because something is going off the rails
     */
    requestDocumentBroadcast()
    {
        console.warn("Requesting document broadcast.")

        // TODO
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
window.mainController = new MainController()
