
/**
 * Returns true if two changes are equal by value
 */
function changesEqual(a, b)
{
    return a.from.line == b.from.line &&
        a.from.ch == b.from.ch &&
        a.to.line == b.to.line &&
        a.to.ch == b.to.ch &&
        JSON.stringify(a.text) == JSON.stringify(b.text) &&
        JSON.stringify(a.removed) == JSON.stringify(b.removed)
}

module.exports = changesEqual
