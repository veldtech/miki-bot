# Miki
![Miki](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade)
[![](https://img.shields.io/badge/invite-miki-7289da?logo=discord)](https://miki.ai/invite?ref=github)
<br>
Your solution to a great Discord Community! Adding levels, role management, achievements, profiles, image search, games, and many more!

## Build status
| Platform | Status |
| --- | --- |
| Ubuntu | ![badge](https://dev.azure.com/mikibot/Miki/_apis/build/status/miki-ubuntu-master) |
| Docker |[![Build Status](https://dev.azure.com/mikibot/Miki/_apis/build/status/Mikibot.bot?branchName=master)](https://dev.azure.com/mikibot/Miki/_build/latest?definitionId=15&branchName=master) |

## Useful links
Bot invite: https://miki.bot/invite?ref=github<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Guides: https://miki.bot/guides<br>
Issues: https://github.com/Mikibot/bot/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/39Xpj7K<br>


## Feature requests
[Go to Suggestions.Miki.ai](https://suggestions.miki.ai)

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
