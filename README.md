Unity Scene Groups 0.2.0
===
Adds a "Scene Group" asset type in the project browser that allows you to compose and save multiple scenes to be opened at once.

![Scene Groups In Action](https://i.imgur.com/axA9AJX.gif)

A unitypackage version can be found in the releases for easy importing to any project.

## Features
* Add, remove and reorder the scenes however you want via the reorderable list in the inspector.
* Specify which scene you want to be active on load, as well as which scenes you want to be loaded. The active scene will always be loaded.

* Select which scene you want to be active when loaded with the toggle.
* To make adding new scenes easier, you can also drag one or more scenes from the project browser onto anywhere in the inspector to append them. 
* **Bonus Feature**: If you hold `Alt` while opening any scene or multi-scene from the project view, it will load it additively instead of singly. 

## Preset Creation

This utility adds two new menu items with which to create scene groups:

1. `Edit > Scene Group From Open Scenes` will create a scene groups from the currently open scenes. This also has the default shortcut of `Ctrl+Shift+Alt+S`.

2. `Assets > Create > Scene Group` will make a new empty scene group asset.


## Usage

To open a scene group, simply open it in the project window like you would a normal scene!

## Known Limitations

- Scene Groups cannot be dragged directly into the hierarchy to be added additively like a normal scene. It's unclear if this is even possible using available APIs.

## Changelog

### 0.2.0

- **Breaking Change**: Now supports adding unloaded scenes to your SceneGroup assets. This is a breaking change to previous versions.
- Some additional labels added for clarity.

### 0.1.1

- Properly mark scene group assets as dirty when changed

### 0.1.0

- Initial release
