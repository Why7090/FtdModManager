Automated installation and update of From The Depths mods.
Paths are relative to the path set in --parent

Commands:
  help
    * show this message again
  install <uri> <path>
    * install a new mod from with the manifest.json from <uri> into directory specified by <path>
  update <path>
    * update a mod managed by FtdModManager
  update all
    * update all mods
  remove <path>
    * uninstall a mod
  list
    * show all mods managed by FtdModManager
  setup
    * install or update FtdModManager
  (run exe without command)
    * alias of setup

Options:
  -p --parent
    * Set the parent directory of mods (default is "Documents/From The Depths/Mods")
  -y --accept-all --yes
    * Accept and skip all confirmations

Aliases:
  help = --help = h = -h = ?
  install = i
  update = upgrade = u
  remove = delete = uninstall = r = d = u
  list = l
  setup = (run exe without command) = self-update = s