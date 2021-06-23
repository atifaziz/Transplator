#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
./build.sh
dotnet pack --no-build -c Release "$@" src
