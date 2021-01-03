#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
dotnet restore
dotnet tool restore
for c in Debug Release; do
    dotnet build --no-restore -c $c
done
