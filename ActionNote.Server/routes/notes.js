var express = require('express');
var ObjectId = require('mongodb').ObjectID;
var router = express.Router();

/*
 * GET to retrieve all notes (even deleted)
 */
router.get('/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    collection.find({
        'userId' : req.params.userId,
        'deleted' : true
    },{},function(e,docs){
        res.json(docs);
    });
});

/*
 * GET to retrieve all non-deleted notes
 */
router.get('/list/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    collection.find({
        'userId' : req.params.userId,
        'deleted' : false
    },{},function(e,docs){
        res.json(docs);
    });
});

/*
 * POST to add a note.
 */
router.post('/add/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    
    var data = toDocument(req.params.userId, req.body);
    
    collection.insert(data, function(err, result) {
        res.send(
            (err === null) ? { msg: 'OK' } : { msg: err }
        );
    });
});

/*
 * POST to add a range of notes.
 */
router.post('/addrange/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    
    var datas = [];
    
     req.body.forEach(function(noteItem) {
        datas.push(toDocument(req.params.userId, noteItem))
    });

    collection.insert(datas, { 
            'ordered': false // do not stop when one isert fails.
        }, function(err, result) {
        res.send(
            (err === null) ? { msg: 'OK' } : { msg: err }
        );
    });
});

/*
 * POST to sync notes.
 * Pushes local notes with id and timestamp, returns changes.
 */
router.post('/sync/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    
    var syncNotes = req.body.data;
    var idList = [];
    var idToChangedTimeMap = {};
    
    var syncDataResult = {
        'added' : [],
        'changed' : [],
        'deleted' : [],
        'missingIds' : []
    };
    
    syncNotes.forEach(function(entry) {
        var id = ObjectId(entry._id);
        idList.push(id);
        idToChangedTimeMap[id] = entry.date;
        syncDataResult.missingIds.push(entry._id);
    });
    
    // get all items that have changed
    collection.find({
        '_id' : { $in : idList },
        'userId' : req.params.userId,
        'deleted' : false,
    }, {}, function(e, docs) { 
        docs.forEach(function(entry) {
            var uploadedChangedDate = toUtcTime(idToChangedTimeMap[entry._id]);
            if (uploadedChangedDate != null &&
                entry.timeUtc > uploadedChangedDate)
            {
                syncDataResult.changed.push(entry.data);
            }
            
            removeFromArray(syncDataResult.missingIds, entry._id);         
        });
        
        // get all new that the current client does not have
        collection.find({
            '_id' : { $nin: idList },
            'userId' : req.params.userId,
            'deleted' : false,
        }, {}, function(e, docs) {
            docs.forEach(function(entry) {
                syncDataResult.added.push(entry.data);
            });
            
            // get all items that have been deleted
            collection.find({
                '_id' : { $in: idList },
                'userId' : req.params.userId,
                'deleted' : true,
            }, {}, function(e, docs) {
                docs.forEach(function(entry) {
                    syncDataResult.deleted.push(entry.data);
                    removeFromArray(syncDataResult.missingIds, entry._id);  
                });

                res.json(syncDataResult);
            });
        });
    });
});

/*
 * PUT to update a note.
 */
router.put('/update/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    var utcDate = toUtcTime(req.body.date)  
    
    collection.update({
        '_id': ObjectId(req.body._id),
        'userId' : req.params.userId,
        'timeUtc' : { $lt: utcDate }
    }, {
        $set: {
            'timeUtc': utcDate,
            'data' : req.body }
    }, {
        //'upsert' : true // insert if not existing --> no, because when its older does not mean it does not exist
    }, function(err, result) {
        
        var message;
        if (err === null)
        {
            if (result == 1)
                message = 'OK';
            else
                message = 'DELETED';
        }
        else
        {
            message = err;
        }
        res.send({ msg: message });
    });
});

/*
 * DELETE to mark delete single note.
 */
router.delete('/delete/:userId/:id', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    collection.update({
        '_id': ObjectId(req.params.id),
        'userId' : req.params.userId,
    }, {
        $set : { 'deleted' : true }
    }, function(err, result) {
        res.send(
            (err === null) ? { msg: 'OK' } : { msg: err }
        );
    });
});

/*
 * POST (multiple IDs) to mark delete multiple note.
 */
router.post('/delete/:userId', function(req, res) {
    var db = req.db;
    var collection = db.get('notelist');
    
    var idList = [];
    
    console.log(req.body);
    
    req.body.forEach(function(noteId) {
        var id = ObjectId(noteId);
        idList.push(id);
    });
    
    console.log(idList);
    
    collection.update({
        '_id' : { $in : idList },
        'userId' : req.params.userId
    }, {
        $set : { 'deleted' : true }
    }, {
       'multi' : true 
    }, function(err, result) {
        
        console.log(result);
        res.send(
            (err === null) ? { msg: 'OK' } : { msg: err }
        );
    });
});

/*
 * PUT to restore a note.
 */
router.put('/restore/:userId/:id', function(req, res) {
    /*
    var db = req.db;
    var collection = db.get('notelist');
    collection.update({
        '_id': ObjectId(req.params.id),
        'userId' : req.params.userId,
    }, {
        $set : { 'deleted' : false }
    }, function(err, result) {
        res.send(
            (err === null) ? { msg: 'OK' } : { msg: err }
        );
    });*/
    var db = req.db;
    var collection = db.get('notelist');
    var utcDate = toUtcTime(req.body.date)  
    
    collection.update({
        '_id': ObjectId(req.body._id),
        'userId' : req.params.userId,
    }, {
        $set: {
            'timeUtc': utcDate,
            'data' : req.body,
            'deleted' : false }
    }, {
        //'upsert' : true // insert if not existing --> no, because when its older does not mean it does not exist
    }, function(err, result) {
        
        var message;
        if (err === null)
        {
            message = 'OK';
        }
        else
        {
            message = err;
        }
        res.send({ msg: message });
    });
});

/**
 * Helper function to get the utc out of a DateTimeOffset.
 */
function toUtcTime(dateTimeOffset)
{
    var dateTime = dateTimeOffset.DateTime;
    var start = dateTime.indexOf("(");
    var end = dateTime.indexOf(")");
    return parseInt(dateTime.substring(start + 1, end));
}

/**
 * Helper function to remove an item from an array.
 */
function removeFromArray(arr, item) {
    var index = arr.indexOf(item.toHexString()); // indexOf is not working on ObjectID => strings
    if (index > -1) {
        arr.splice(index, 1);
    }
}

/**
 * Helper function to convert a serialized NoteItem object to a document.
 */
function toDocument(userId, noteItem) {
    return {
        'userId' : userId,
        '_id' : ObjectId(noteItem._id),
        'timeUtc': toUtcTime(noteItem.date),
        'deleted' : false,
        'data' : noteItem
    };
}

module.exports = router;
