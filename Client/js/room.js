const Editor = require("./editor.js")

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

        this.editor.registerOnChangeListener(this.onEditorChange.bind(this))
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
        if (content == this.editor.cm.getValue())
            return

        this.editor.cm.replaceRange(
            content,
            {line: 0, ch: 0},
            {line: this.editor.cm.lineCount(), ch: null},
            "*server"
        )

        console.warn("Document state broadcast has made some changes.")
    }

    /**
     * Change broadcast by some foreign client has been received
     * (it's not our change so it's foreign)
     */
    foreignChangeBroadcastReceived(change)
    {
        this.editor.cm.replaceRange(
            change.text.join("\n"),
            change.from,
            change.to,
            "*server"
        )
    }

    /**
     * Change broadcast has been received that we have initiated in the past
     * (so the change is familiar since we initiated it)
     */
    familiarChangeBroadcastReceived(change)
    {
        
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
    
        this.socket.send(JSON.stringify({
            type: "change",
            change: change
        }))
    }
}

// MAIN
window.mainController = new MainController()
