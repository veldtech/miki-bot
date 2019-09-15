# Miki
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade) 
<br>
Your solution to a great Discord Community! Adding levels, role management, achievements, profiles, image search, games, and many more!

## Build status
| Platform | Status |
| --- | --- |
| Ubuntu 16.04 | ![badge](https://dev.azure.com/mikibot/Miki/_apis/build/status/miki-ubuntu-master) |
| Windows | ![badge](https://dev.azure.com/mikibot/Miki/_apis/build/status/miki-windows-master) |

## Useful links
Bot invite: https://miki.ai/invite?ref=github<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Issues: https://github.com/velddev/Miki/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/39Xpj7K<br>


## Feature requests
[Go to Suggestions.Miki.ai](https://suggestions.miki.ai)


## Installation Steps:
1) Clone the [Miki repository](https://github.com/Mikibot/Miki.git).

2) Install [Miki.Framework](https://github.com/Mikibot/Miki.Framework.git) and [Miki.Rest](https://github.com/Mikibot/Miki.Rest.git) through NuGet or clone them as well, along with the [other dependencies](https://github.com/Mikibot/Miki#dependencies).

3) Add your bot token in Miki/miki/settings.json

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

If you have any questions about the setup process **do not** ask in the support server, as a majority of the people there will not be able to assist you.

1. Download docker and docker-compose. [For Windows users](https://docs.docker.com/docker-for-windows/install/),
[For Mac users](https://docs.docker.com/docker-for-mac/install/), and if you're on Linux you already know what you're doing.

2. Add your bot token in `selfhost.json` under "token"

3. `docker-compose up -d miki_postgres`

<<<<<<< HEAD

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

If you have any questions about the setup process **do not** ask in the support server, as a majority of the people there will not be able to assist you.

1. Download docker and docker-compose. [For Windows users](https://docs.docker.com/docker-for-windows/install/),
[For Mac users](https://docs.docker.com/docker-for-mac/install/), and if you're on Linux you already know what you're doing.

2. `docker-compose up -d miki_postgres`

3. `docker build -t miki_bot . --network=mikinet`

These steps are only required **once** for installation. After that 
you can just do `docker-compose -d up` and `docker-compose down`
to start and stop Miki.
