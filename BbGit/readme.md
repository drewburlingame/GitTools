# Purpose
BbGit is a console app providing tools for managing a collection of BitBucket git repositories.

This tool should be run from the folder that contains the set of repositories.  

for example, if you have the following paths:

`c:\src\repo1`
`c:\src\repo2`

you would run this tool from the `c:\src` directory

# Setup
run the command `BbGit config-global init-app`, creating your local config file.
Edit the config file, given by the command, with your BitBucket account info.
Follow [this guide](https://confluence.atlassian.com/bitbucket/app-passwords-828781300.html) to create your app password

After building/downloading bbgit, add the .exe to your path for easy use anywhere.

## Configuring existing repositories
BbGit stores configs and caches within each repository directory, within a .bbgit folder.  Add this to your .gitignore file
Use the `BbGit repo-config update-all-configs` command to update existing repositories

Use `BbGit config-repo local-configs-clear` from the root folder to remove the folders from all repos

# Usage
Type `-h` with any command and sub-command to see the options available.

Use the 'bb' command to run queries against the BitBucket api
Use the 'local' command to run queries & commands against the local repositories

Most of the commands accept piped-input so you can filter repositories using the list command and pipe them into clone, pull, etc.


# Credits
The following are some of the open source projects that have made it creating this application significantly easier.
This is not an exhaustive list, and I'll probably forget to add some as more are brought in.  

see [paket.dependecies] (paket.dependencies) for the complete list.

These stand out has having made the big differences for me.

[CommandDotNet] (https://github.com/bilal-fazlani/commanddotnet) Bilal Fazlani created a great framework for creating console apps with multiple commands.  This is now my favorite console framework.

[Colorful.Console] (https://github.com/tomakita/Colorful.Console) Tom Akita created a handy library for formatting console output, making it so much easier to grok the output.

[LibGit2Sharp] (https://github.com/libgit2/libgit2sharp) The most complete library for interacting with git.

[Paket] (https://github.com/fsprojects/Paket) sane dependency management

[SharpBucket] (https://github.com/MitjaBezensek/SharpBucket/) Mitja Bezenšek created a great library for interacting with the BitBucket api
