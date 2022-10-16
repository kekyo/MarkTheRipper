@echo off

rem MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.
rem Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
rem
rem Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0

echo.
echo "==========================================================="
echo "Build MarkTheRipper"
echo.

rem git clean -xfd

dotnet restore
dotnet build -p:Configuration=Release
dotnet pack -p:Configuration=Release -o artifacts
