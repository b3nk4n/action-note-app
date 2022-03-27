# README #

This README describes the local test environment setup

### Installation ###

* NodeJS
* MongoDB

### Start MongoDB ###

Start MongoDB Server with authentication and connect to it:
```
#!python#
$ cd ~/action-note/ActionNote.Server
$ mkdir data
$ mongod --auth --dbpath=./data

# Open a new console
$ mongo
  # note: you might press CTRL+D on the server console (MongoDB-Windows bug?)
```

Create the test user and test DB (use **not** --auth on the server here):
```
#!python#
use actionnote
db.createUser(
   {
     user: "actionnote-user",
     pwd: "PASSWORD",
     roles: [ {role: "readWrite", db: "actionnote"} ]
   }
)
```

Run the NodeJS REST-Service and get all notes of user XXXXXXXXXX
```
$ npm install express
$ npm install serve-favicon
$ npm install morgan
$ npm install cookie-parser
$ npm install body-parser
$ npm install method-override
$ npm install connect-multiparty
$ npm install mongodb # gives warnings!
$ npm install monk
$ npm install filendir
$ npm install mime
$ npm install jade
$ node app.js
```

Server is now running on [http://localhost:64302/notes/list/XXXXXXXXXX](Link URL)
