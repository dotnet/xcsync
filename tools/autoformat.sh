#!/bin/bash -ex

# Go to the top level directory
cd "$(git rev-parse --show-toplevel)"
SRC_DIR=$(pwd)

# Go one directory up, to avoid any global.json in maccore
cd ..

# Start formatting!
dotnet restore "$SRC_DIR/tools/apple-doc-reader/Xamarin.AppleDocs.sln"
dotnet format "$SRC_DIR/tools/apple-doc-reader/Xamarin.AppleDocs.sln" --no-restore
###
# Disable xcsync until we can authenticate properly with private NuGet feeds
###
# dotnet restore "$SRC_DIR/tools/xcsync/xcsync.sln"
# dotnet format "$SRC_DIR/tools/xcsync/xcsync.sln" --no-restore
###

# dotnet format "$SRC_DIR/[...]"
# add more projects here...

cd "$SRC_DIR"
