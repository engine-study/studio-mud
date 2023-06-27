# studio-mud

# WIP WIP WIP

## Installation 
1. Install Unity 2022.3.x
2. Open Unity Package Manager
3. Click "Add package from git URL" and paste `https://github.com/engine-study/studio-mud.git`

## Usage

For each MUD table you must do the following:

1. Inherit the MUDComponent class (ex. PositionComponent).
2. Create a prefab and attach the component script to it.
3. Make a matching MUDTableManager class (ex. PositionManager).
4. Attach your `TableManager` to an object in the scene.
5. Link the *component prefab* to the TableManager in the inspector.
6. Run `pnpm run dev:node` and `pnpm run dev` and enter Play Mode
8. Your MUDComponent should spawn under its MUDEntity.

## Syncing

Inherit from ComponentSync and add to a *component prefab* to easily keep it synced with the other components on the entity.
For example: ___

