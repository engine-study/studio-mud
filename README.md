# studio-mud

## ⚠️ HEAVILY WORK IN PROGRESS ⚠️

## Features 
- This package extends UniMUD, a Unity3D package for [MUD](https://mud.dev/).
- Studio-MUD defines a parent `MUDComponent` class that, when inherited, links a MUD Table to a Unity Prefab.
- All the components of an entity spawn under their respective `MUDEntity`.
- A `MUDComponent` can have "required components" (other components on the same `MUDEntity`), and will wait until all are loaded, after which a `PostInit()` method is called.

## Installation 
1. Create a project with [UniMUD](https://github.com/emergenceland/UniMUD)
2. Open Unity and open the Package Manager
3. Click "Add package from git URL" and add `https://github.com/engine-study/studio-mud.git`

## Usage

For each MUD table you must do the following:

1. In Git Bash, run `pnpm dev`, then, run `pnpm dev:unity` in another Git Bash tab to link the world.
2. Create a C# script of `MUDComponent` class (ex. `HealthComponent`).
3. Design a prefab and attach the component script to it (ex. a `HealthComponent` might have FX particle system for getting hurt on its Prefab).
4. Override `UpdateComponent` and handle how client renders changes.
5. Link the **Prefab** to the **Manager** in the inspector.
6. Your **Prefab** should spawn when you enter Play Mode.

## TODO
- Pooling
- Indexing

![example4](https://github.com/engine-study/studio-mud/assets/7606952/5ddf082c-d84b-41c0-b31f-8cbc560fee1a)
