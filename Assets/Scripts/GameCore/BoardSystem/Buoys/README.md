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
All columns are spawned. Only the first non-empty stack accepts input. A tap exports the consecutive top color group. Empty stacks advance after all flights finish. Conveyor capacity and special element rules are not implemented.
Buoy and BuoyStack directly hold copied values and element IDs from the level asset; there is no nested RuntimeData wrapper. Spawned object lists are runtime collections. Gameplay must not mutate the level asset.

## Visual configuration

Each prefab has its corresponding Visual component with references to your authored models.
BuoyVisual colors only assigned renderers through MaterialPropertyBlock using the Ring shader's _Color property, without changing shared materials or creating material instances. The Ring shader is unchanged; this does not add GPU instancing support to it.
BuoyStackVisual controls the first buoy offset and vertical spacing.
BuoyStackHolderVisual selects single/multiple decorations and controls the first stack offset and queue direction/spacing. Keep decorative roots separate from stackRoot.
The holder passes OutletDirection to its Visual, which rotates local +Z toward the outlet in board coordinates. Up/right/down/left map to Y rotations of 0/90/180/-90 degrees. Unconfigured nodes keep identity rotation. Keep stackRoot under the holder with identity local rotation to align the stack queue; offset model orientation on a decorative child if needed. The default negative-Z stackStep places waiting stacks behind the outlet-facing stack.
Holder positions and conveyor points share BoardCoordinates and BoardData.CellSize. Keep boardRoot and its ancestors at unit scale for world-unit sizing.
The conveyor channel configuration is untouched.

## Manual verification in Unity

### Input

BoardManager resolves or creates an InputSystem component during initialization. For Inspector configuration, add the component before entering Play Mode and assign its camera, raycast mask and distance. Without an assigned camera it uses Camera.main.

BuoyStackVisual implements IInputReceiver. The controller binds each stack visual to its BuoyStack and InputSystem before spawning its buoys. Assign Input Colliders on the stack visual; an empty array uses colliders on the same GameObject only, so individual buoy colliders are never collected. RingStack.prefab explicitly references its enabled, non-trigger capsule collider on the Buoy layer. Ring.prefab's mesh collider is disabled.

Mouse down or the primary touch beginning dispatches one stack click. UI GraphicRaycaster hits block board input when an EventSystem is present. Disabling the stack visual unregisters its colliders; enabling registers them again. Clearing a stack releases its registration and click subscribers.

The holder subscribes to BuoyStack.Clicked and locks the source for the full transfer. Waiting stacks have their input colliders disabled. BuoyVisual handles movement but does not receive input.

- Verify clicking different parts of a stack produces one Stack clicked log per press, including touch on a device.
- Verify UI blocks the click and CanReceiveInput = false prevents dispatch.
- Disable and re-enable a stack visual, then reinitialize the board: only active, current stacks should receive input.

- One column: one holder, one stack, matching buoy count and bottom-to-top colors.
- Three columns including an empty column: three stacks in source order, with no buoys on the empty stack.
- Reinitialize in Play Mode: old spawned holders are hidden immediately and destroyed; counts do not accumulate.
- Restart Play Mode: the source level data is unchanged.
- Change board CellSize: holder cell centers continue to align with conveyor cell centers.

## Migration from component-based gameplay objects

Remove the old Buoy, BuoyStack, BuoyStackHolder and BuoyStackHolderController components from existing prefabs/scenes (Unity may display them as missing scripts after compilation). Keep the Visual components and authored models. Place each Visual component on its prefab root so creation and cleanup own the complete prefab. Reassign the three Visual prefab fields on BoardManager; old gameplay component references are not compatible with the new field types.
BoardManager clears its controller when destroyed. Reinitialization clears old runtime objects and their visuals. Visual.Release hides the object immediately and schedules destruction; Clear is terminal for these objects, not a pooling operation.

## Stack to conveyor transfers

## Conveyor to stack transfers

The holder's configured OutletCell also serves as its receiving port; there is no separate inlet direction in the current level data. Conveyor movement detects crossing this port along the spline, including loop wrap, and stops an eligible group there. Only fully loaded groups whose buoys all match the active stack's top color can return. Empty, busy, and unconfigured holders do not receive groups. Other groups continue along the conveyor.

The holder locks input while receiving. Buoys leave the group's top and fly to the next free stack position in sequence, using Transfer Speed and Launch Interval. The stopped group retains its conveyor reservation until the last buoy lands, then is removed and the holder unlocks. Reset clears any receiving flight before stacks and conveyor groups are destroyed.

## Stack to conveyor transfer scheduling

BoardManager owns BuoyTransferController and ticks it in LateUpdate, after conveyor movement. This frame-based scheduler launches overlapping flights without asynchronous tasks surviving a level reset. Configure Transfer Speed and Launch Interval on BoardManager. Flight speed is clamped to at least conveyor speed + 0.5 world units/second so a moving destination remains catchable.

ConveyorController projects the holder's OutletCell (GridPosition + OutletDirection) onto its spline to find the entry. Configure Move Speed and Surface Offset on this component. Groups use spline Travel in world distance; closed paths wrap and open paths stop at their end. Group capacity is currently unlimited.

Each tap reserves one group and one slot per buoy, in top-to-bottom launch order. The group stays still until slot zero receives its buoy, then starts moving immediately. Flights chase the current world position of their slot every frame. Parent changes only on arrival. Buoys in flight belong to the transfer controller; source roots stay alive and stationary until the transfer completes. On reset, flights are cleared before source stacks and conveyor groups.

Buoy Height is 0.2 and Buoy Spacing is 0 on RingStack.prefab to preserve its previous 0.2 placement step. Adjust these to the authored mesh dimensions. Pole Transform references Stick. Its mesh bounds determine scaling while preserving the original base. Height is count * buoyHeight + max(0, count - 1) * spacing; empty poles are hidden. Capsule height follows the stack but cannot shrink below its diameter. Queue advancement currently repositions the remaining stacks immediately.

Manual verification:
- Top-to-bottom red/red/blue/red: one tap exports only the first two reds into one group.
- Repeated taps during flight do not export duplicates; rear stacks cannot intercept input.
- The group stays still before the first arrival and moves while later buoys chase it.
- Inspect each buoy parent during flight and after arrival; it changes only at arrival.
- Each launch reduces source height; an empty source survives until its last arrival, then the next stack activates.
- Export from multiple holders, change conveyor speed and cross the closed-path seam.
- Reinitialize or exit Play Mode during overlapping flights: no orphan buoy or group remains.

## Conveyor admission and sampled movement path

LevelManager owns Path Move Slot Spacing (default 0.3 world units) and Conveyor Group Gap (default 0.6 world units). These two settings apply on level initialization. Choose the gap to cover the buoy diameter. Move Speed and Root Y Offset remain adjustable during play.

ConveyorController builds _pathMoveSlots in backward spline order, samples distance/percent pairs once and snaps entry requests to the nearest movement slot. Groups advance through this cached distance map; spline evaluation between samples preserves the spline curve rather than drawing straight chords. These are movement samples, not fixed one-group storage cells. Physical occupancy is a continuous distance reservation around each group.

Entry requests preserve arrival order within overlapping entry regions; blocked requests do not stall independent entry points. No source buoy leaves and no pole shrinks until admission succeeds. A newly admitted stationary group immediately reserves space. Movement constraints propagate backward from blocked groups, including across the loop seam; a full loop of moving groups can still advance together. Open paths stop at the end. Capacity depends on path length and configured gap. The gap is measured along the path, not between nearby parallel tracks in world space.

Verify simultaneous holder taps, a blocked entry, a loading group with followers, a full moving loop, zero speed, and reinitialization with pending requests in Play Mode. Waiting for space intentionally keeps the source locked. Future conveyor-to-stack removal must release the group reservation as well as its visual.
