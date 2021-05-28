#!/bin/sh -l

echo "Hello $1"
time=$(date)
echo "::set-output name=time::$time"
python3 --version
emcc --version
cmake --version