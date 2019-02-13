# Miki
![lol oily fucked up](https://discordbots.org/api/widget/status/160105994217586689.svg) [![Codacy Badge](https://api.codacy.com/project/badge/Grade/0181e7d12f8344fd9950067e80f53f74)](https://www.codacy.com/app/velddev/Miki?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=velddev/Miki&amp;utm_campaign=Badge_Grade)
<br>
Below you will find a basic workflow of how the branches should work. Please follow this to prevent bad merge conflicts on Miki and things constantly breaking.

## Table of contents

### [Workflow for branches](#workflow-for-branches-1)
### [Useful links](#useful-links-1)
### [Getting Started](#getting-started-1)
 
## Useful links
Bot invite: https://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Issues: https://github.com/velddev/Miki/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/39Xpj7K<br>


## Feature requests
[Go to Suggestions.Miki.ai](https://suggestions.miki.ai)


## Installation Steps:

#### Important:
Currently the Miki API is __private__, meaning you won't have access to the leaderboards until the API is released publicly. More information will be available [here](https://github.com/mikibot/miki/wiki/API-Leaderboards) when that happens.

If you have any questions about the setup process **do not** ask in the support server, as a majority of the people there will not be able to assist you.

1. Download docker and docker-compose. [For Windows users](https://docs.docker.com/docker-for-windows/install/),
[For Mac users](https://docs.docker.com/docker-for-mac/install/), and if you're on Linux you already know what you're doing.

2. `docker-compose up -d miki_postgres`

3. `docker build -t miki_bot . --network=mikinet`

4. `docker-compose -d up`

These steps are only required **once** for installation. After that 
you can just do `docker-compose -d up` and `docker-compose down`
to start and stop Miki.
