#!/bin/bash
#assumes you've installed jsdoc and docdash via npm i -g docdash jsdoc
jsdoc ./out/js_apidocs/rh3dm_temp.js README.md -c jsdoc.conf -t ~/.npm-global/lib/node_modules/docdash -d ../../docs/javascript/api