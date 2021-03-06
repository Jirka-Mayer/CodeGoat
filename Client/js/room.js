const Editor = require("./editor.js")
const invertChange = require("./invertChange.js")
const orderPositions = require("./orderPositions.js")
const str_random = require("./str_random.js")
const updateLocationByChange = require("./updateLocationByChange.js")

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
        this.DEBUG = debug; // false | true | verbose

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
         * State of the document
         * = Id of the last known committed change (or initial state)
         */
        this.documentState = null

        /**
         * For each client id there's a list of markers defining their selection
         */
        this.selectionMarkers = {}

        /**
         * Field with this client's name
         */
        this.nameField = document.querySelector("#your-name")

        /**
         * List of other clients
         * {
         *      id: 45,
         *      color: "#456",
         *      name: "John"
         * }
         */
        this.otherClients = []

        /**
         * List with other clients
         */
        this.otherClientsList = document.querySelector("#other-clients")

        this.nameField.addEventListener("change", this.onNameChange.bind(this))

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
        if (this.DEBUG === "verbose")
            console.log("Received message:", message)

        switch (message.type)
        {
            case "document-state":
                if (message.initial)
                {
                    if (this.DEBUG)
                        console.log("Room has been joined.")
                    
                    this.setupInitialDocument(message.document, message["document-state"])
                    this.isConnected = true // now we are inside a room

                    this.onNameChange() // broadcast name
                }
                else
                {
                    this.handleDocumentStateBroadcast(message.document, message["document-state"])
                }
                break

            case "change-broadcast":
                if (this.speculativeChanges.filter(x => x.id == message.change.id).length > 0)
                {
                    // this change is inside the speculative changes

                    // is not the first on (error detection)
                    if (this.speculativeChanges[0].id != message.change.id)
                    {
                        console.error("Received speculative change that is not the first one.")
                        console.error("Speculative changes:", this.speculativeChanges)
                        console.error("Received change:", message.change)
                        
                        // clear speculatives which will cause document broadcast to make changes
                        // (and if not, then we've just fixed the problem)
                        this.speculativeChanges = []
                        this.requestDocumentBroadcast()
                    }
                    else
                    {
                        // the change is not speculative anymore
                        this.speculativeChanges.splice(0, 1)
                    }
                }
                else
                {
                    // this change is a foreign one, apply it
                    this.changeDocumentBeforeSpeculativeChanges(message.change, "*server")
                }

                // update document state because the change is a newer commited change
                this.documentState = message.change.id

                break

            case "selection-broadcast":
                this.handleSelectionBroadcast(message.clientId, message.selection)
                break

            case "client-update":
                let client = this.getOrCreateClient(message.clientId)
                client.name = message.name
                client.color = message.color
                this.updateOtherClientsUI()
                break

            case "client-left":
                for (let i = 0; i < this.otherClients.length; i++)
                {
                    if (this.otherClients[i].id == message.clientId)
                    {
                        // remove from client list
                        this.otherClients.splice(i, 1)

                        // clear selection markers
                        if (!this.selectionMarkers[message.clientId])
                            this.selectionMarkers[message.clientId] = []
                        for (let i = 0; i < this.selectionMarkers[message.clientId].length; i++)
                            this.selectionMarkers[message.clientId][i].clear()

                        break
                    }
                }
                this.updateOtherClientsUI()
                break

            default:
                console.error("Server sent message that wasn't understood:", message)
                break
        }
    }

    /**
     * Setup the document content right after the connection succeeded
     */
    setupInitialDocument(content, state)
    {
        this.speculativeChanges = []
        this.documentState = state;

        this.editor.cm.replaceRange(
            content,
            {line: 0, ch: 0},
            {line: this.editor.cm.lineCount(), ch: null},
            "*server"
        )

        this.editor.cm.clearHistory()

        this.editor.cm.setCursor(0, 0)
    }

    /**
     * Update document state
     * We received a document state broadcast to fix any possible errors that might have accumulated
     */
    handleDocumentStateBroadcast(content, state)
    {
        if (this.DEBUG)
            console.log("Received document state broadcast.")

        // check document state
        let documentDiffers = false
        this.unrollSpeculativeChangesAndDo(() => {
            if (content != this.editor.cm.getValue())
                documentDiffers = true
        })

        // if something weird is happening
        if (documentDiffers)
        {
            // update document state
            let ranges = this.editor.cm.listSelections()
            this.editor.cm.replaceRange(
                content,
                {line: 0, ch: 0},
                {line: this.editor.cm.lineCount(), ch: null},
                "*server"
            )
            this.editor.cm.setSelections(ranges)

            // Remove all speculative changes.
            // These changes have been sent already and will be received from the server and
            // will seem like changes performed by other clients. But that's ok. Consistency is saved.
            this.speculativeChanges = []

            // the broadcasted document was definitely obtained by a comitted change
            this.documentState = state;

            console.warn("Document state broadcast has made some changes.")
        }
    }

    /**
     * Performs document change with speculative changes unrolled
     * Updates locations of speculative changes accordingly
     */
    changeDocumentBeforeSpeculativeChanges(change, origin)
    {
        this.unrollSpeculativeChangesAndDo(() => {
            // perform the change
            this.applyChange(change, origin)

            // update locations of speculative changes
            for (let i = 0; i < this.speculativeChanges.length; i++)
            {
                this.speculativeChanges[i].from = updateLocationByChange(
                    this.speculativeChanges[i].from,
                    change
                )

                this.speculativeChanges[i].to = updateLocationByChange(
                    this.speculativeChanges[i].to,
                    change
                )
            }

            // NOTE: "removed" property of the change should be updated as well.
            // This is if the old change inserts some text into the middle of a speculative change.
            // But it is not a common situation, because two people don't want to write over each other
            // because it's difficult to keep track of for the human users.
            // However if it was to happen, the only result is a triggering of document broadcast
            // and a warning in the console. No inconsistency in the document gets created.
        })
    }

    /**
     * Perform some actions on the document, but rollback speculative changes before
     * the action and re-apply them afterwards
     * 
     * If these actions modify the document, then locations of speculative changes have to be updated
     * othrwise inconsistency gets created
     */
    unrollSpeculativeChangesAndDo(action)
    {
        // rollback speculative changes
        for (let i = this.speculativeChanges.length - 1; i >= 0; i--)
        {
            this.applyChange(
                invertChange(this.speculativeChanges[i]),
                "*server"
            )
        }

        // perform action
        action()

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
     * Applies a change object to the editor on behalf of some origin
     */
    applyChange(change, origin)
    {
        if (this.editor.cm.getRange(change.from, change.to) != change.removed.join("\n"))
        {
            console.warn(
                `Applying a change that removed different ` +
                `text at source client than is being removed now.`,
                change
            )
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
        let client = this.getOrCreateClient(clientId)

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
                elem.style.borderLeftColor = client.color
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
                    css: `background: ${client.color}`
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
        if (!this.isConnected)
            return

        if (change.origin.match(/server$/))
            return

        // assign an ID to the change so that it can be tracked though the system
        // IDs are not handled by the codemirror, they are entirely a slapped on feature
        change.id = str_random(16)

        // debug log
        if (this.DEBUG === "verbose")
            console.log("Editor change:", change)

        // list of all speculative change ids this change depends on
        let dependencies = this.speculativeChanges.map(c => c.id)

        this.speculativeChanges.push(change)
        this.socket.send(JSON.stringify({
            "type": "change",
            "change": change,
            "document-state": this.documentState,
            "dependencies": dependencies
        }))

        // selection
        this.socket.send(JSON.stringify({
            "type": "selection-change",
            "selection": {
                "ranges": this.editor.cm.listSelections()
            }
        }))
    }

    /**
     * Called when the editor selection changes
     */
    onEditorSelection(selection)
    {
        if (this.DEBUG === "verbose")
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
     * When this client's name changes
     */
    onNameChange()
    {
        if (!this.isConnected)
            return

        this.socket.send(JSON.stringify({
            type: "name-changed",
            name: this.nameField.value
        }))
    }

    getOrCreateClient(clientId)
    {
        for (let i = 0; i < this.otherClients.length; i++)
        {
            if (this.otherClients[i].id == clientId)
                return this.otherClients[i]
        }

        let client = {
            id: clientId,
            color: "black",
            name: "anonymous"
        }

        this.otherClients.push(client)

        return client
    }

    updateOtherClientsUI()
    {
        let html = ""

        for (let i = 0; i < this.otherClients.length; i++)
        {
            html += `
                <li>
                    <span
                        class="color-disc"
                        style="background: ${this.otherClients[i].color}">
                    </span>
                    ${this.otherClients[i].name}
                </li>
            `;
        }

        this.otherClientsList.innerHTML = html
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
