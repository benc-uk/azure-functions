module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    if (req.query.id) {

        if(Number.isInteger(parseInt(req.query.id))) {
            // Return some random properties, to look something like a real API
            var id = parseInt(req.query.id);
            context.log(`### GET Request for: ${id}`)
            var colours = ["red", "green", "blue", "purple", "yellow", "orange", "magenta", "black, white"];
            var colour = colours[Math.floor(random(id)*colours.length)];            
            res = {
                body: {thingId: req.query.id, thingColour: colour, thingSize: (random(id)*100).toFixed(2)},
                headers: {'Content-Type': 'application/json'}
            };
        } else {
            res = {
                status: 500,
                body: {message: "Error! Id must be of type int"},
                headers: {'Content-Type': 'application/json'}
            };
        }
    }
    else {
        res = {
            status: 400,
            body: "Please pass id on the query string or in the request body"
        };
    }
    context.done(null, res);
};

// Seedable random number gen
function random(seed) {
    var x = Math.sin(seed++) * 10000;
    return x - Math.floor(x);
}