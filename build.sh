#!/bin/bash
if test "$1" = ''; then
	echo -en "\E[0;37m usage: \033[1;37m./build.sh \E[1;32m<dir>\n\E[0;0m";
	exit 1;
fi

echo "Getting latests on git."
git pull origin master --quiet
git submodule update --init
echo "Stopping Miki service"
service miki stop
echo "Building Miki.csproj"
cd Miki
dotnet publish Miki.csproj -c Release -v m -o $1
cd ../
echo "Starting Miki service"
service miki start
echo "And we're back!"
