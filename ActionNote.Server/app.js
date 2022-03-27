var express = require('express');
var path = require('path');
var favicon = require('serve-favicon');
var logger = require('morgan');
var cookieParser = require('cookie-parser');
var bodyParser = require('body-parser');
var methodOverride = require('method-override');
var multipart = require('connect-multiparty');
// database
var mongo = require('mongodb');
var monk = require('monk');

// LOCAL
var db = monk('actionnote-user:PASSWORD@localhost:27017/actionnote');

// PRODUCTIVE
//var db = monk('actionnote-user:cyactionnote89@localhost:20984/actionnote');

var routes = require('./routes/index');
var notes = require('./routes/notes');
var attachements = require('./routes/attachements');

var app = express();

var http = require('http').Server(app);
http.listen(64302, function() {
	console.log('listening on *:64302');
});

// view engine setup
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'jade');

// uncomment after placing your favicon in /public
//app.use(favicon(path.join(__dirname, 'public', 'favicon.ico')));
app.use(logger('dev'));
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: false }));
app.use(cookieParser());
app.use(express.static(path.join(__dirname, 'public')));

// file upload
app.use(methodOverride());
app.use(multipart());

// make our db accessible to our router
app.use(function(req, res, next){
    req.db = db;
    next();
});

app.use('/', routes);
app.use('/notes', notes);
app.use('/attachements', attachements);

// catch 404 and forward to error handler
app.use(function(req, res, next) {
  var err = new Error('Not Found');
  err.status = 404;
  next(err);
});

// error handlers

// development error handler
// will print stacktrace
if (app.get('env') === 'development') {
  app.use(function(err, req, res, next) {
    res.status(err.status || 500);
    res.render('error', {
      message: err.message,
      error: err
    });
  });
}

// production error handler
// no stacktraces leaked to user
app.use(function(err, req, res, next) {
  res.status(err.status || 500);
  res.render('error', {
    message: err.message,
    error: {}
  });
});


module.exports = app;
