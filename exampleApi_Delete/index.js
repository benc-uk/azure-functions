module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    if (req.query.id) {
        if(Number.isInteger(parseInt(req.query.id))) {
            res = {
                body: {message: "Thing "+req.query.id+" deleted OK"},
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
            body: {message: "Please pass id on call"},
            headers: {'Content-Type': 'application/json'}
        };
    }
    context.done(null, res);
};