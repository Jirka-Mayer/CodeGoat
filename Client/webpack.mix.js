let mix = require('laravel-mix');

let publicPath = "../Server/bin/Debug";
mix.setPublicPath(publicPath)

mix.copy("html/index.html", publicPath + "/html")
mix.copy("html/room.html", publicPath + "/html")

mix.stylus("stylus/room.styl", "css")
mix.js("js/room.js", "js")
