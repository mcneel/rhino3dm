#!/bin/bash
#assumes you've installed jsdoc and docdash via npm i -g docdash jsdoc
#might need to run it from npm global folder:  ~/.npm-global/bin/jsdoc
jsdoc ./out/js_apidocs/rh3dm_temp.js README.md -c jsdoc.conf -t ~/.npm-global/lib/node_modules/docdash -d ../../docs/javascript/api