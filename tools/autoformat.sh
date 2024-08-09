#!/bin/bash -ex

# Go to the top level directory
cd "$(git rev-parse --show-toplevel)"
SRC_DIR=$(pwd)

# Go one directory up, to avoid any global.json in maccore
cd ..

# Start formatting!
dotnet restore "$SRC_DIR/xcsync.sln"
dotnet format "$SRC_DIR/xcsync.sln" --no-restore

# dotnet format "$SRC_DIR/[...]"
# add more projects here...

cd "$SRC_DIR"
