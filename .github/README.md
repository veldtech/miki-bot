# Miki
Your solution to a great Discord Community! Adding levels, role management, achievements, profiles, image search, games, and many more!

# ⚠️ Notice: This repository is no longer used and will no longer be updated. Miki has been moved private source into a monorepo to further develop and integrate all modules. The repository will be left up for any learning purposes. IT should still work, but you may have a bad time getting it in a good place.

## Build status
| Platform | Status |
| --- | --- |
| Ubuntu | ![badge](https://dev.azure.com/mikibot/Miki/_apis/build/status/miki-ubuntu-master) |
| Docker |[![Build Status](https://dev.azure.com/mikibot/Miki/_apis/build/status/Mikibot.bot?branchName=master)](https://dev.azure.com/mikibot/Miki/_build/latest?definitionId=15&branchName=master) |

## Getting Started 

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

Note that it is currently **not** possible to build the entire project without access to private dependencies. If you wish to contribute and need help to circumvent the private dependencies, DM `Zenny#0001`.

**Do not** ask related questions in the support server, as a majority of the people there will not be able to assist you. 

## Installation Steps:


### Source

1. Clone the miki bot repository
Windows, Linux
```bash
$ git clone https://github.com/mikibot/bot && cd bot
```

2) Download [PostgreSQL](https://www.postgresql.org/) and set up a database called `Miki`.

3) Copy `launchSettings.template.json` and fill in your PostgreSQL properties.
```bash
$ cp src/Miki/Properties/launchSettings.template.json src/Miki/Properties/launchSettings.json
```

4) Run tools/setup.sh and follow the settings.
```bash
$ tools/setup.sh
```

### Docker
1. Clone the Miki bot repository
```bash
$ git clone https://github.com/mikibot/bot && cd bot
```

2. Copy example.env and fill in your properties.
```bash
$ cp example.env .env
```

3. Ensure you have the environment variable `PRIVATE_NUGET_PAT` set in your environment.
This requires a valid PAT token for Miki's private dependencies, which can only be granted by the Miki team.

4. Docker-compose
```bash
$ docker-compose up
```

## Possible issues:
These will likely be fixed in the near future:

* A lack of API keys might be giving you issues in the `DonatorModule` and `FunModule`, the simplest way to solve it is to just comment out the lines that raise exceptions and  the lines that reference the client **(there shouldn't be more than 2 reference max.**

* Setting up your development environment requires two manual steps in the database.
1. Add a configuration row in the `Configuration` table, filling in at least the `Id` and `Token` fields according to your test Discord bot.
2. Within the `Users` table, add a new User with an ID of `1`. This user serves as your global Miki bank, so make sure that you set its currency to a high amount.
