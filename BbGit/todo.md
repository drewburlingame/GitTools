* specify profile for all commands.  (profiles defined in .bbgit\config)
	* .bbgit folder in a repo's parent folder could default profile
	* store RemoteRepo info in .bbgit in repo folder
* find branch across repos
* on clone-all or update-metadata, for each repo
  * if .bbgit not in .gitignore
    * checkout new branch
	* add .bbgit to .gitignore
	* add option to push branch to remote
* fluent-validation for repo search
  * some options don't work together
* integration tests
  * create 3 BB repos w/ 2 projects in free personal account
* parent folder .bbgit configs
  * useSsh