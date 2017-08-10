module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    if (req.query.id) {

        if(Number.isInteger(parseInt(req.query.id))) {
            res = {
                body: {thingId: req.query.id, colour: 'blue', wowFactor:Math.random()},
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