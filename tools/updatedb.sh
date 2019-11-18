#!/bin/bash

if [ "$#" -ne 1 ]; then
    echo "usage: updatedb.sh <connectionString>"
	exit 1
fi


echo setting up...
export MIKI_CONNSTRING=$1
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
CWD=$PWD

# Please don't move this file.
cd $DIR/..

echo starting migration...
dotnet ef database update --project submodules/miki.bot.models --startup-project src/Miki

echo cleaning up...
unset MIKI_CONNSTRING

echo done!
