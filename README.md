# README #

This README describes the local test environment setup

### Installation ###

* NodeJS
* MongoDB

### Start MongoDB ###


```
#!python#
$ cd ~/action-note/ActionNote.Server
$ mkdir data
$ mongod --auth --dbpath=./data

# Open a new console
$ mongo
  # note: you might press CTRL+D on the server console (MongoDB-Windows bug?)
```

Create the test user and test DB:
#!python#
use actionnote
db.createUser(
   {
     user: "actionnote-user",
     pwd: "***REMOVED***",
     roles: [ "readWrite", "dbAdmin" ]
   }
)
```