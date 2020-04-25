#!/bin/bash
# Quick setup script to set up your bot.

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
CWD=$PWD

# Please don't move this file.
cd $DIR/..

echo -e "PostgreSQL IP? (default: localhost)\n"
read db_ip
if [ -z "$db_ip" ]; then
	db_ip="localhost"
fi

echo -e "PostgreSQL Database? (default: miki)\n"
read db_database
if [ -z "$db_database" ]; then
	db_database="miki"
fi

echo -e "PostgreSQL Username? (default: postgres)\n"
read db_user
if [ -z "$db_user" ]; then
	db_user="postgres"
fi

echo -e "PostgreSQL Password? (default: none)\n"
read db_pass

echo -e "Discord Token? (required)\n"
read discord_token
if [ -z "$discord_token" ]; then
	echo "required value 'token' not received."
	exit 1
fi

db_connstring="Server=$db_ip;Database=$db_database;Username=$db_user;"
if ! [ -z "$db_pass" ]; then
	db_connstring="Password=$db_pass;$db_connstring"
fi

echo Writing environment file...
echo -e MIKI_CONNSTRING=$db_connstring\nMIKI_LOGLEVEL=Information\nMIKI_SELFHOSTED=true >> .env

echo Updating database...
tools/updatedb.sh $db_connstring

echo $db_connstring

echo "Inserting config... (might ask for your DB password again.)"
psql -U $db_user -h $db_ip -d $db_database -c "INSERT dbo.\"Configuration\" (\"Token\") VALUES ('$discord_token')"