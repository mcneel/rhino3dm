#!/bin/bash

set -x

cd ${0%%$(basename $0)}
mkdir build
cd build


if [[ "$OSTYPE" == "linux-gnu" || "$OSTYPE" == "linux" ]]; then
    : # ...
elif [[ "$OSTYPE" == "darwin"* ]]; then
    #cmake -DCMAKE_BUILD_TYPE=DEBUG .. && make && make test
    emcmake cmake ..
    make
    make install
elif [[ "$OSTYPE" == "cygwin" ]]; then
    : # POSIX compatibility layer and Linux environment emulation for Windows
elif [[ "$OSTYPE" == "msys" ]]; then
    : # shell and GNU utilities compiled for Windows as part of MinGW
elif [[ "$OSTYPE" == "win32" ]]; then
    : # good luck
elif [[ "$OSTYPE" == "freebsd"* ]]; then
    : # ...
else
    : # Unknown.
fi
