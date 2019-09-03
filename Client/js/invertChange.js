/**
 * Creates a change that rolls back the given change when applied
 */
function invertChange(change)
{
    return {
        id: change.id,
        invertedChange: true,
        from: change.from,
        to: {
            line: change.from.line + change.text.length - 1,
            ch: change.text.length == 1 ?
                change.from.ch + change.text[0].length :
                change.text[change.text.length - 1].length
        },
        text: change.removed,
        removed: change.text
    }
}

module.exports = invertChange
