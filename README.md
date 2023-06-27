# studio-mud

# WIP WIP WIP

## Installation 
1. Install Unity 2022.3.x
2. Open Unity Package Manager
3. Click "Add package from git URL" and paste `https://github.com/engine-study/studio-mud.git`

## Usage

For each MUD table you must do the following:

1. Inherit the MUDComponent script
2. Create a prefab with the new script attached.
3. Place the prefab into a `Resources/Components` subdirectory anywhere in the Assets folder.
5. Create your inherited MUDTableManager and attach it to a gameobject.
6. Deploy your contracts with `pnpm run dev:node` and `pnpm run dev` and enter Play Mode
7. Your MUDComponent should spawn under its MUDEntity.

## Syncing

Inherit from ComponentSync and add to a component prefab to easily keep it synced with the other components the entity has.
For example: ___

