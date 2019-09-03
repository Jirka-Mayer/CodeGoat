
/**
 * Tracks where a location in the document moves, when a change gets executed
 * (copied from C# Location.UpdateByChange(...))
 */
function updateLocationByChange(location, change)
{
    let ret = {
        line: location.line,
        ch: location.ch,
        sticky: location.sticky
    };

    // location is before everything so it doesn't move
    if (location.line < change.from.line)
        return ret;
    
    if (location.line == change.from.line)
    {
        if (location.ch < change.from.ch)
            return ret;
        
        if (location.ch == change.from.ch && location.sticky == "before")
            return ret;
    }

    /////////////////
    // Delete text //
    /////////////////
    
    // location is inside the deleted region so move it to the "from" (keep stickiness)
    if (
        location.line < change.to.line || // inside by line
        (location.line == change.to.line && (location.ch < change.to.ch || // or the same line but inside by char
            (location.ch == change.to.ch && location.sticky == "before") // or same char but inside by stickiness
        ))
    )
    {
        ret.line = change.from.line;
        ret.ch = change.from.ch;
    }

    // location is after the deleted region
    else
    {
        // if the location is on the last line of deletion, perform leftwise movement
        if (ret.line == change.to.line)
        {
            ret.ch -= change.to.ch - change.from.ch;
        }

        // move line up by the number of deleted lines
        ret.line -= change.to.line - change.from.line;
    }
    
    /////////////////
    // Insert text //
    /////////////////

    // the case when location is before the edited region is already handled here
    // so the location has to be after the region

    // unless it was inside and is now sticking before
    if (ret.line == change.from.line && ret.ch == change.from.ch && location.sticky == "before")
        return ret;

    // now it's after the inserted region so just perform the movement

    // if the location is on the line of insertion, perform rightwise movement
    if (ret.line == change.from.line)
    {
        // multiline
        if (change.text.length > 1)
        {
            ret.ch += change.text[change.text.length - 1].length - change.from.ch;
        }
        else // inline
        {
            ret.ch += change.text[change.text.length - 1].length;
        }
    }

    // move line down by the number of inserted lines
    ret.line += change.text.length - 1;

    return ret;
}

module.exports = updateLocationByChange
