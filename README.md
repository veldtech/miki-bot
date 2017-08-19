# Welcome to Miki Repository
[![CodeFactor](https://www.codefactor.io/repository/github/velddev/miki/badge)](https://www.codefactor.io/repository/github/velddev/miki)
<br>
Below you will find a basic workflow of how the branches should work. Please follow this to prevent bad merge conflicts on Miki and things constantly breaking.

## Table of contents

### Workflow for branches
### Dependencies
### Useful links

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
* Make your changes in your branch that you have just created.
* Commit those changes, and then push the branch to the remote git repository.
* When changes are approved, and are ready for release, the branch will be merged into the **master** branch.
  * *Note: The working branch at this time will be deleted from the repository.*
  
## Dependencies
The following dependencies are mostly gotten from nuget, I'll link a github source repo otherwise.

* Entity Framework 6.0
* Discord.Net
* IA ([source](https://github.com/velddev/IA))
* IA.SDK ([source](https://github.com/velddev/IA.SDK))
* Imgur.API
* Newtonsoft.Json
  
## Useful links
Bot invite: https://discordapp.com/oauth2/authorize?&client_id=160185389313818624&scope=bot<br>
Documentation: https://github.com/velddev/Miki/wiki<br>
Issues: https://github.com/velddev/Miki/issues<br>
Patreon: https://www.patreon.com/mikibot<br>
Support server: https://discord.gg/55sAjsW<br>
Trello: https://trello.com/b/4Mgl8nBa/miki<br>
