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
  
