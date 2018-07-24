# Welcome to Miki Repository
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg)
<br>
Below you will find a basic workflow of how the branches should work. Please follow this to prevent bad merge conflicts on Miki and things constantly breaking.

## Table of contents

### [Workflow for branches](#workflow-for-branches-1)
### [Dependencies](#dependencies-1)
### [Useful links](#useful-links-1)
### [Getting Started](#getting-started-1)

## Workflow for Branches

* Pull the latest changes from the **master** branch.
* Create a new working branch based off the last commit from the **master** branch.
  * Branches should be named as follows:
    * **update_name**: For LARGE updates that will take a while to update. Smaller hotfix updates will be merged into this if they are released before the large update.
      * *(e.g., update_v2.0.1)*
    * **hf_name**: For SMALLER hotfix updates that will be pushed out before larger updates are completed. These will be merged back into master AND the update branches whenever they are released.
      * *(e.g., hf_fixmarrycommand)*
    * **helper_name_date**: For helper-related changes (such as cleaning up code, web documentation updates, etc), that need to be approved by Veld before they can be merged into any other branches.
      * *(e.g., helper_veld_201610 or helper_documentation_201610)*
* Make your changes in the branch you have just created.
* Commit those changes, and then push the branch to the remote git repository.
* When changes are approved and are ready for release, the branch will be merged into the **master** branch.
  * *Note: The working branch at this time will be deleted from the repository.*
  
## Dependencies
* AWSSDK.S3
* Entity Framework 6.0
* Imgur.API
* Miki.Anilist
* Miki.Cache
* Miki.Configuration
* Miki.Discord
* Miki.Dsl
* Miki.Logging
* Miki.Rest
* Newtonsoft.Json
* Npgsql

### Optional Dependencies
* CountLib
* Miki.Patreon
* SharpRaven
* SteamKit2
 
## Useful links
Bot invite: https://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Issues: https://github.com/velddev/Miki/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/55sAjsW<br>


## Feature requests
[![Feature Requests](http://feathub.com/Mikibot/Miki?format=svg)](http://feathub.com/Mikibot/Miki)


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

10) Navigate to `Miki.Framework` and run `Update-Database` to apply the migrations there as well. 

11) Clone the [gateway repository](https://github.com/Mikibot/sharder/tree/js) in a separate location.

12) Download [Node.js](https://nodejs.org/en/) if you don't have it installed already.

13) Run `npm install` in the sharder download location to setup the dependencies.

14) Create a `config.js` by copy pasting the format from `config.js.example` and filling in your bot token.

15) Run `node gateway.js`.

16) Run Miki. 🎉

## Possible issues:
These will likely be fixed in the near future (if it's not already by the time you're reading this):

* A lack of API keys might be giving you issues in the `DonatorModule` and `FunModule`, the simplest way to solve it is to just comment out the lines that raise exceptions and  the lines that reference the client **(there shouldn't be more than 2 reference max, if so, you're doing something wrong).**

* If you're having trouble running migrations make sure your `EntityFramework` for both base `Miki` and `Miki.Framework` is on version 2.0.1-2.0.3 **NOT** 2.1.1.
