# Miki
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade)
<br>
Below you will find a basic workflow of how the branches should work. Please follow this to prevent bad merge conflicts on Miki and things constantly breaking.

## Table of contents

### [Workflow for branches](#workflow-for-branches-1)
### [Useful links](#useful-links-1)
### [Getting Started](#getting-started-1)
 
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

3) Add your bot token in Miki/miki/settings.json.

4) Download [RabbitMQ](https://www.rabbitmq.com/download.html) and have the service running.

5) Download [Redis](https://redis.io/download) and get that running too.

6) Download [PostgreSQL](https://www.postgresql.org/) and set up a database called `Miki`.

7) Configure your connection string in Miki/miki/settings.json as such (if using localhost):

```js
"connection_string": "Server=127.0.0.1;Port=5432;User Id=postgres;Database=Miki;"
```

8) Install the `uuid-ossp` postgres extensions on the `Miki` database.

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

9) Run existing migrations inside the base Miki solution through the NuGet Package Manager Console with `Update-Database`

    * Tools -> NuGet Package Manager -> Package Manager Console

10) Clone the [gateway repository](https://github.com/Mikibot/sharder/tree/js) in a separate location.

11) Download [Node.js](https://nodejs.org/en/) if you don't have it installed already.

12) Run `npm install` in the sharder download location to setup the dependencies.

13) Create a `config.js` by copy pasting the format from `config.js.example` and filling in your bot token.

14) Run `node gateway.js`.

15) Run Miki. 🎉

## Possible issues:
These will likely be fixed in the near future (if it's not already by the time you're reading this):

* A lack of API keys might be giving you issues in the `DonatorModule` and `FunModule`, the simplest way to solve it is to just comment out the lines that raise exceptions and  the lines that reference the client **(there shouldn't be more than 2 reference max, if so, you're doing something wrong).**

* If you're having trouble running migrations make sure your `EntityFramework` for both base `Miki` and `Miki.Framework` is on version 2.0.1-2.0.3 **NOT** 2.1.1.
