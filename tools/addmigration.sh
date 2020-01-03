#!/bin/bash

if [ "$#" -ne 2 ]; then
    echo "Usage: ./addmigration.sh <migrationName> <connectionString>"
    exit 1
fi

echo setting up...

echo note: migration name $1
echo note: connection string $2

export MIKI_CONNSTRING=$2
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
CWD=$PWD

# Please don't move this file.
cd $DIR/..

echo starting migration...
dotnet ef migrations add $1 --project submodules/miki.bot.models --startup-project src/Miki

echo cleaning up...
unset MIKI_CONNSTRING

echo done!

