# Buoy spawning scaffold

## Quick setup

1. Open the gameplay scene containing the configured LevelManager and BoardManager.
2. BoardManager creates its plain C# BuoyStackHolderController automatically.
3. Assign prefabs with BuoyVisual, BuoyStackVisual and BuoyStackHolderVisual components on their roots to the three prefab fields on BoardManager.
4. Save the scene. Assign a LevelDataSO with valid ColorData and node data to LevelManager, then enter Play Mode.

## Ownership and data

LevelManager -> BoardManager -> BuoyStackHolderController -> BuoyStackHolder -> BuoyStack -> Buoy.
BoardManager owns the three serialized Visual prefab references and passes them to the controller. Only Visual classes are MonoBehaviours within the buoy system. The controller creates Visual instances and constructs plain C# BuoyStackHolder, BuoyStack and Buoy objects. Runtime objects own their state and command their visuals; visuals never modify gameplay collections.
Each node maps to a holder; each column maps to a stack; each buoy entry maps to a buoy.
Columns keep source order, with index zero as the active stack. Buoys keep source order from bottom to top, matching the editor.
All columns are spawned in this first version. Queue advancement, transfer rules, capacity, special element behavior and animation are not implemented yet.
Buoy and BuoyStack directly hold copied values and element IDs from the level asset; there is no nested RuntimeData wrapper. Spawned object lists are runtime collections. Gameplay must not mutate the level asset.

## Visual configuration

Each prefab has its corresponding Visual component with references to your authored models.
BuoyVisual colors only assigned renderers through the URP _BaseColor property, without changing shared materials.
BuoyStackVisual controls the first buoy offset and vertical spacing.
BuoyStackHolderVisual selects single/multiple decorations and controls the first stack offset and queue direction/spacing. Keep decorative roots separate from stackRoot.
The holder passes OutletDirection to its Visual, which rotates local +Z toward the outlet in board coordinates. Up/right/down/left map to Y rotations of 0/90/180/-90 degrees. Unconfigured nodes keep identity rotation. Keep stackRoot under the holder with identity local rotation to align the stack queue; offset model orientation on a decorative child if needed. The default negative-Z stackStep places waiting stacks behind the outlet-facing stack.
Holder positions and conveyor points share BoardCoordinates and BoardData.CellSize. Keep boardRoot and its ancestors at unit scale for world-unit sizing.
The conveyor channel configuration is untouched.

## Manual verification in Unity

- One column: one holder, one stack, matching buoy count and bottom-to-top colors.
- Three columns including an empty column: three stacks in source order, with no buoys on the empty stack.
- Reinitialize in Play Mode: old spawned holders are hidden immediately and destroyed; counts do not accumulate.
- Restart Play Mode: the source level data is unchanged.
- Change board CellSize: holder cell centers continue to align with conveyor cell centers.

## Migration from component-based gameplay objects

Remove the old Buoy, BuoyStack, BuoyStackHolder and BuoyStackHolderController components from existing prefabs/scenes (Unity may display them as missing scripts after compilation). Keep the Visual components and authored models. Place each Visual component on its prefab root so creation and cleanup own the complete prefab. Reassign the three Visual prefab fields on BoardManager; old gameplay component references are not compatible with the new field types.
BoardManager clears its controller when destroyed. Reinitialization clears old runtime objects and their visuals. Visual.Release hides the object immediately and schedules destruction; Clear is terminal for these objects, not a pooling operation.
