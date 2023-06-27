# studio-mud

# WIP WIP WIP


## Installation 
1. Install Unity 2022.3.x
2. Open Unity Package Manager
3. Click "Add package from git URL" and paste `https://github.com/engine-study/studio-mud.git`

## Usage
1. For each MUD table you must override MUDTableManager and MUDComponent
2. Create a prefab that has the new MUDComponent script on it and place it under a Resources/Components/ subdirectory anywhere in the project.
3. Deploy your contracts with `pnpm run dev:node` and `pnpm run dev` and enter Play Mode, your MUDComponent will spawn under a MUDEntity with its key.
4. Inherit from ComponentSync and add to the component prefab to easily keep it synced to the table and with other components the entity has.

