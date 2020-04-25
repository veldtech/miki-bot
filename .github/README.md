# Miki
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade)
[![](https://img.shields.io/badge/invite-miki-7289da?logo=discord)](https://miki.ai/invite?ref=github)
<br>
Your solution to a great Discord Community! Adding levels, role management, achievements, profiles, image search, games, and many more!

## Build status
| Platform | Status |
| --- | --- |
| Ubuntu | ![badge](https://dev.azure.com/mikibot/Miki/_apis/build/status/miki-ubuntu-master) |
| Docker |[![Build Status](https://dev.azure.com/mikibot/Miki/_apis/build/status/Mikibot.bot?branchName=master)](https://dev.azure.com/mikibot/Miki/_build/latest?definitionId=15&branchName=master) |

## Useful links
Bot invite: https://miki.ai/invite?ref=github<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Issues: https://github.com/velddev/Miki/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/39Xpj7K<br>


## Feature requests
[Go to Suggestions.Miki.ai](https://suggestions.miki.ai)

## Getting Started 
Yes, in its current state this process is _tedious_. However, there will be an installer in the future that will make getting started much simpler.

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

If you have any questions about the setup process **do not** ask in the support server, as a majority of the people there will not be able to assist you. DM Xetera#9596 for questions instead.

## Installation Steps:
1. Clone the miki bot repository

Windows, Linux
```bash
$ git clone https://github.com/mikibot/bot
```

2) Download [PostgreSQL](https://www.postgresql.org/) and set up a database called `Miki`.

3) Copy `launchSettings.template.json` and fill in your PostgreSQL properties.
```bash
$ cp src/Miki/Properties/launchSettings.template.json src/Miki/Properties/launchSettings.json
```

4) Set your bot token through `psql` to insert your configuration in dbo."Configuration"

5) Run Miki. 🎉

## Possible issues:
These will likely be fixed in the near future (if it's not already by the time you're reading this):

* A lack of API keys might be giving you issues in the `DonatorModule` and `FunModule`, the simplest way to solve it is to just comment out the lines that raise exceptions and  the lines that reference the client **(there shouldn't be more than 2 reference max, if so, you're doing something wrong).**

* If you're having trouble running migrations make sure your `EntityFramework` for both base `Miki` and `Miki.Framework` is on version 2.0.1-2.0.3 **NOT** 2.1.1.
