# Miki
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade)
[![](https://img.shields.io/badge/invite-miki-7289da?logo=discord)](https://miki.ai/invite?ref=github)
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

## Getting Started 
Yes, in its current state this process is _tedious_. However, there will be an installer in the future that will make getting started much simpler.

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

If you have any questions about the setup process **do not** ask in the support server, as a majority of the people there will not be able to assist you. DM Xetera#9596 for questions instead.

## Installation Steps:
1) Clone the [Miki repository](https://github.com/Mikibot/Miki.git).

2) Install [Miki.Framework](https://github.com/Mikibot/Miki.Framework.git) and [Miki.Rest](https://github.com/Mikibot/Miki.Rest.git) through NuGet or clone them as well, along with the [other dependencies](https://github.com/Mikibot/Miki#dependencies).

3) Download [Redis](https://redis.io/download) and get that running too.

4) Download [PostgreSQL](https://www.postgresql.org/) and set up a database called `Miki`.

5) Configure your connection string in `launchSettings.json` as such (if using localhost):

| Key | Value |
| --- | --- |
| MIKI_CONNSTRING | "Server=127.0.0.1;Port=5432;User Id=postgres;Database=Miki;Password={YOUR_PASSWORD}" |
| MIKI_SELFHOSTED | "true" |
| MIKI_LOGLEVEL | "Debug" |
| MIKI_MESSAGEWORKER | "1" |

6) Run existing migrations inside the base Miki solution through the NuGet Package Manager Console with `Update-Database`

    * Tools -> NuGet Package Manager -> Package Manager Console

7) Set your bot token through `psql` to insert your configuration in dbo."Configuration"

8) Run Miki. 🎉

## Possible issues:
These will likely be fixed in the near future (if it's not already by the time you're reading this):

* A lack of API keys might be giving you issues in the `DonatorModule` and `FunModule`, the simplest way to solve it is to just comment out the lines that raise exceptions and  the lines that reference the client **(there shouldn't be more than 2 reference max, if so, you're doing something wrong).**

* If you're having trouble running migrations make sure your `EntityFramework` for both base `Miki` and `Miki.Framework` is on version 2.0.1-2.0.3 **NOT** 2.1.1.
