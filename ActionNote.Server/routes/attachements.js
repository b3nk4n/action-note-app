var express = require('express');
var fs = require('fs')
var fsAutoCreateDirs = require('filendir')
var path = require('path');
var mime = require('mime');
var router = express.Router();

/*
 * GET to retrieve a file.
 */
router.get('/file/:userId/:file', function(req, res) {
	var filePath = path.join(path.resolve('.'),'data', 'attachement', req.params.userId, req.params.file);
	res.download(filePath);
});

/*
 * POST to add a file.
 */
router.post('/file/:userId/:file', function(req, res) {
    fs.readFile(req.files.file.path, function (err, data) {
		var filePath = path.join(path.resolve('.'),'data', 'attachement', req.params.userId, req.params.file);
		fsAutoCreateDirs.writeFile(filePath, data, function (err) {
			res.send((err === null) ? { msg: filePath } : { msg:'error: ' + err });
		});
	});
});


module.exports = router;