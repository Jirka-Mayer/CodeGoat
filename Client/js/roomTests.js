const changesEqual = require("./changesEqual.js")

/**
 * Suite of tests to be executed against the code mirror editor in the browser
 */
class RoomTests
{
    run(controller)
    {
        // MainController in the room.js
        this.controller = controller

        this.clearEditor("")

        console.log("Starting tests...")

        this.itReceivesChangesFromOtherClients()
        this.itCarriesSpeculativeChange()

        console.log("Tests finished.")
    }

    clearEditor(content)
    {
        // kill connection
        if (this.controller.socket)
            this.controller.socket.close()

        // set content
        this.controller.setupInitialDocument(content)
    }

    assertContent(expected)
    {
        let actual = this.controller.editor.cm.getValue()
        
        if (actual != expected)
        {
            console.log("Expected:", JSON.stringify(expected))
            throw new Error("Expected string does not match the editor content.");
        }
    }

    itReceivesChangesFromOtherClients()
    {
        this.clearEditor("Hello world!")

        this.controller.foreignChangeBroadcastReceived({
            from: { line: 0, ch: "Hello world!".length},
            to: { line: 0, ch: "Hello world!".length},
            text: ["", "Lorem ipsum."],
            removed: [""]
        })

        this.assertContent("Hello world!\nLorem ipsum.")

        this.controller.foreignChangeBroadcastReceived({
            from: { line: 1, ch: "Lorem ipsum.".length},
            to: { line: 1, ch: "Lorem ipsum.".length},
            text: [" Dolor amet."],
            removed: [""]
        })

        this.assertContent("Hello world!\nLorem ipsum. Dolor amet.")

        this.controller.foreignChangeBroadcastReceived({
            from: { line: 0, ch: "Hello world!".length},
            to: { line: 1, ch: "Lorem ipsum.".length},
            text: [""],
            removed: ["", "Lorem ipsum."]
        })

        this.assertContent("Hello world! Dolor amet.")
    }

    itCarriesSpeculativeChange()
    {
        /*
            We are client A. Client A will insert "ccc" in between "aaa" and "bbb".
            However client B inserts "ddd" at the end at the same time.
            So the first edit gets rolled back, B is applied and the re-applied.
            And then confirmed.
        */

        this.clearEditor("aaabbb")

        // mock method
        let sentMessage = null
        let originalSend = this.controller.socket.send
        this.controller.socket.send = (msg) => {
            sentMessage = msg
        }

        // user types "ccc"
        let clientAChange = {
            from: { line: 0, ch: 3},
            to: { line: 0, ch: 3},
            text: ["ccc"],
            removed: [""]
        }
        this.controller.applyChange(clientAChange, "+input")

        // unmock
        this.controller.socket.send = originalSend

        // content changes
        this.assertContent("aaacccbbb")

        // message was sent
        if (sentMessage != '{"type":"change","change":{"from":{"line":0,"ch":3},"to":{"line":0,"ch":3},"text":["ccc"],"removed":[""],"origin":"+input"}}')
        {
            console.log("Sent message: ", sentMessage)
            throw new Error("Change message has not been sent.")
        }

        // assert this change is speculative
        if (!changesEqual(this.controller.speculativeChanges[0], clientAChange))
            throw new Error("Speculative change not present.")

        // now the outside change arrives
        this.controller.foreignChangeBroadcastReceived({
            from: { line: 0, ch: 6}, // note: 6 not 9, speculative change has to be rolled back
            to: { line: 0, ch: 6},
            text: ["ddd"],
            removed: [""]
        })

        // content changes
        this.assertContent("aaacccbbbddd")

        // assert speculative change is still there
        if (!changesEqual(this.controller.speculativeChanges[0], clientAChange))
            throw new Error("Speculative change not present.")

        // now the familiar change arrives, finally
        this.controller.familiarChangeBroadcastReceived({
            from: { line: 0, ch: 3},
            to: { line: 0, ch: 3},
            text: ["ccc"],
            removed: [""]
        })

        // assert no speculative changes
        if (this.controller.speculativeChanges.length != 0)
            throw new Error("Speculative change still present.")

        // content stays the same
        this.assertContent("aaacccbbbddd")
    }
}

module.exports = (controller) => {
    let tests = new RoomTests()
    tests.run(controller)
}
