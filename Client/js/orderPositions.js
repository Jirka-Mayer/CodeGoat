
/**
 * Helper method; compares two positions
 */
function comparePositions(a, b)
{
    if (a.line < b.line)
        return -1

    if (a.line > b.line)
        return 1

    if (a.ch < b.ch)
        return -1

    if (a.ch > b.ch)
        return 1

    return 0
}

/**
 * Return two positions ordered so that the first one is eralier in the document
 * (position = one end of a range, for example)
 * (position = {line: ?, ch: ?})
 */
function orderPositions(a, b)
{
    if (comparePositions(a, b) == 1)
        return [b, a]

    return [a, b]
}

module.exports = orderPositions
