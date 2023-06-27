# studio-mud

# WIP WIP WIP

## Installation 
1. Install Unity 2022.3.x
2. Open Unity Package Manager
3. Click "Add package from git URL" and paste `https://github.com/engine-study/studio-mud.git`

## Usage

For each MUD table you must do the following:

1. Create a new C# script of `MUDComponent` class (ex. `PositionComponent`).
2. Design a prefab and attach the component script to it (ie. a `DamageComponent` might have spark FX).
3. Make a matching `MUDTableManager` class (ex. `PositionManager`).
4. Attach your new **manager** to an object in the scene.
5. Link the **component prefab** to the **manager** in the inspector.
6. Run `pnpm run dev:node` and `pnpm run dev` and enter Play Mode
8. Your **component prefab** should spawn under its Entity.

## Syncing

Inherit from ComponentSync and add to a **component prefab** to easily keep it synced with the other components on the entity.
For example: ___

