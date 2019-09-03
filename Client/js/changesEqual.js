
/**
 * Returns true if two changes are equal
 */
function changesEqual(a, b)
{
    // new implementation with IDs
    
    return a.id == b.id

    // old implementation by value

    // return a.from.line == b.from.line &&
    //     a.from.ch == b.from.ch &&
    //     a.to.line == b.to.line &&
    //     a.to.ch == b.to.ch &&
    //     JSON.stringify(a.text) == JSON.stringify(b.text) &&
    //     JSON.stringify(a.removed) == JSON.stringify(b.removed)
}

module.exports = changesEqual
