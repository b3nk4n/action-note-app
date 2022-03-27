
# Action Note ![GitHub](https://img.shields.io/github/license/b3nk4n/action-note-app)

This innovative note taking app is freely available for the Windows 10 platform.

Action Note is not just another note taking app. It integrates with your Action Center and gives you the fastest access to all of your notes. Even with photos and across all your Windows 10 devices.

Check out the must-have app for your Windows device.

![Action Note](https://b3nk4n.github.io/assets/img/posts/2022/action-note-teaser.png)

### Features
- Action Center integration
- Attachements, even in your Action Center
- Categorization and ordering
- Live Tiles
- Sharing features
- Voice-to-text and text-to-voice
- Personalization
- Intuitive gestures
- Beautiful design
- Cross-device synchronization _(Pro)_
- Full offline support _(Pro)_
- Read QR codes _(Pro)_

### Reviews

What did users think about Action Note? Here are just a few out of 5000+ reviews:

> "The first great 3rd party app in windows. Maybe the new paradigm of windows note apps."
>
> _hyuntae, USA_

> "Finally some innovative use for the action center! Good idea!"
>
> _Jukka, USA_

> "Great app, Great idea :) I love the way you use the action center. The add quick note in the action center is great, and I love the fact that we can add images in it. I would really love to do more stuff like sharing web pages into the notes, and have a little preview for it. Also shearing map into a note and have it as image. It would be really cool to select which of the notes I like to see in the action center and to select if by default new note is add to the action center or not (when shearing or creating from the quick note in the action center). Please keep up the great work. I'm really considering to buy this app for me and my girl friend :)."
>
> _Yossi, USA_

## Basic setup

This README describes the local test environment setup

### Installation ###

The following is required to be installed:

* NodeJS
* MongoDB

### Start MongoDB ###

Start MongoDB Server with authentication and connect to it:
```bash
$ cd ~/action-note/ActionNote.Server
$ mkdir data
$ mongod --auth --dbpath=./data
```

Then, start the MongoDB client in a separate console using the `monogo` command and create the test user and test DB (do **not** use `--auth` on the server here):

```javascript
use actionnote
db.createUser(
   {
     user: "actionnote-user",
     pwd: "PASSWORD",
     roles: [ {role: "readWrite", db: "actionnote"} ]
   }
)
```

Run the NodeJS REST-Service and install all dependencies:
```bash
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
Server is now running on http://localhost:64302.

To get all notes of user `XXXXXXXXXX`, simply navigate to
http://localhost:64302/notes/list/XXXXXXXXXX

## License

This work is published under [MIT][mit] License.

[mit]: https://github.com/b3nk4n/action-note-app/blob/master/LICENSE