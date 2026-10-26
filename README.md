There was an idea for a mod I had that would use custom data in a save file, but I realized there's some flaws with it. There's no conventional way of distinguishing which save files have modded data and what mod data they contain.
This is my attempt at fixing these problems. Current features include:

Methods that help with creating new save flags. They must be called before a file is loaded/created.
CreateScriptableDataBool
CreateGameFlagInt
CreateScriptableDataFloat
CreateGameFlagString
All of them require at least 2 parameters: The instance of the plugin calling it, and a unique name for the flag. The API does the rest for you.

A list that shows which mods have data in a save file when selecting it. Clicking on a mod in a list can take you to a download page if it has one.

Warnings for when you attempt to load a save when you don't have a mod in the list, you have a different version of a mod in the list, or you have a mod not in the list.
