#!/bin/sh

# MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
# Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
#
# Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo ""
echo "==========================================================="
echo "Build MarkTheRipper"
echo ""

# git clean -xfd

dotnet restore
dotnet build -p:Configuration=Release
dotnet pack -p:Configuration=Release -o artifacts
