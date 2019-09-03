
/**
 * Generates a random string of a given length
 */
function str_random(length)
{
    let result = "";
    const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    
    for (let i = 0; i < length; i++)
        result += alphabet.charAt(Math.floor(Math.random() * alphabet.length));
    
    return result;
}

module.exports = str_random
