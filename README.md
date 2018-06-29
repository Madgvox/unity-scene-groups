Unity Multi-Scene Loader Asset 0.1.0
===
Adds a "Multi-Scene" asset type in the project browser that allows you to compose and save multiple scenes to be opened at once.

![Multi-Scene In Action](https://i.imgur.com/axA9AJX.gif)

A unitypackage version can be found in the releases for easy importing to any project.

## Features
* Add, remove and reorder the scenes however you want via the reorderable list in the inspector. 

* Select which scene you want to be active when loaded with the toggle.
* To make adding new scenes easier, you can also drag one or more scenes from the project browser onto anywhere in the inspector to append them. 
* **Bonus Feature**: If you hold `Alt` while opening any scene or multi-scene from the project view, it will load it additively instead of singly. 

## Preset Creation

This utility adds two new menu items with which to create multi-scenes:

1. `Edit > Multi-Scene From Open Scenes` will create a multi-scene from the currently open scenes. This also has the default shortcut of `Ctrl+Shift+Alt+S`.

2. `Assets > Create > Multi-Scene` will make a new empty multi-scene asset.


## Usage

To open a multi-scene, simply open it in the project window like you would a normal scene!