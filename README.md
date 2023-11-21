# studio-mud
- This package extends UniMUD, a Unity package for [MUD](https://mud.dev/).

## ⚠️ ALMOST PRODUCTION READY ⚠️

## Tutorial
- Follow this [guide](https://gaulll.notion.site/Tankmud-Tutorial-studio-mud-03b74081dac14b998caddbd6c3db9e46?pvs=4) to make a basic MUD game with Unity.

## Features 
- Studio-MUD defines a parent `MUDComponent` class that, when inherited, links a MUD Table to a Unity Prefab.
- All the components of an entity spawn under their respective `MUDEntity`.
- A `MUDComponent` can have "required components" (other components on the same `MUDEntity`), and will wait until all are loaded, after which a `PostInit()` method is called.

## Installation 
1. Create a project with [UniMUD](https://github.com/emergenceland/UniMUD)
2. Open Unity and open the Package Manager
3. Click "Add package from git URL" and add `https://github.com/engine-study/studio-mud.git`

## TODO
- Pooling
- Indexing

![example4](https://github.com/engine-study/studio-mud/assets/7606952/5ddf082c-d84b-41c0-b31f-8cbc560fee1a)
