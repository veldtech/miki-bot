#!/bin/bash

# Cleans up environment values set by script
function clean {
  echo "cleaning up..."
  unset MIKI_CONNSTRING
}

if [ "$#" -ne 1 ]; then
    echo "usage: updatedb.sh <connectionString> [id]"
	exit 1
fi

echo setting up...
export MIKI_CONNSTRING=$1
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
CWD=$PWD

# Please don't move this file.
cd $DIR/..

echo starting migration...

dotnet ef > /dev/null
if [ $? -ne 0 ]; then
  echo "installing dotnet-ef tool..."
  dotnet tool install dotnet-ef -g
fi

dotnet ef database update --project submodules/miki.bot.models --startup-project src/Miki $2
if [ $? -ne 0 ]; then
  clean
  echo "update failed."
  exit 1
fi

clean
echo done!
exit 0
