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
Only the initial one or two visible stacks are spawned. Remaining non-empty columns stay queued as data. Only the front stack accepts input and incoming groups. A tap exports the consecutive top color group. Empty front stacks advance after all flights finish. Conveyor admission respects the configured gap; special element rules are not implemented.
Buoy and BuoyStack directly hold copied values and element IDs from the level asset; there is no nested RuntimeData wrapper. Spawned object lists are runtime collections. Gameplay must not mutate the level asset.

## Visual configuration

Each prefab has its corresponding Visual component with references to your authored models.
BuoyVisual colors only assigned renderers through MaterialPropertyBlock using the Ring shader's _Color property, without changing shared materials or creating material instances. The Ring shader is unchanged; this does not add GPU instancing support to it.
BuoyStackVisual controls the first buoy offset and vertical spacing.
BuoyStackHolderVisual selects permanent single/multiple decorations during initialization and controls slot positions, queue tweening and trigger detection. Keep decorative roots separate from stackRoot.
The holder passes OutletDirection to its Visual, which rotates local +Z toward the outlet in board coordinates. Up/right/down/left map to Y rotations of 0/90/180/-90 degrees. Unconfigured nodes keep identity rotation. Keep stackRoot under the holder with identity local rotation to align the stack queue; offset model orientation on a decorative child if needed. The default negative-Z stackStep places waiting stacks behind the outlet-facing stack.
Holder positions and conveyor points share BoardCoordinates and BoardData.CellSize. Keep boardRoot and its ancestors at unit scale for world-unit sizing.
The conveyor channel configuration is untouched.

## Manual verification in Unity

### Input

BoardManager resolves or creates an InputSystem component during initialization. For Inspector configuration, add the component before entering Play Mode and assign its camera, raycast mask and distance. Without an assigned camera it uses Camera.main.

BuoyStackVisual implements IInputReceiver. The controller binds each stack visual to its BuoyStack and InputSystem before spawning its buoys. Assign Input Colliders on the stack visual; an empty array uses colliders on the same GameObject only, so individual buoy colliders are never collected. RingStack.prefab explicitly references its enabled, non-trigger capsule collider on the BuoyStack layer. Ring.prefab's mesh collider is disabled.

Mouse down or the primary touch beginning dispatches one stack click. UI GraphicRaycaster hits block board input when an EventSystem is present. Disabling the stack visual unregisters its colliders; enabling registers them again. Clearing a stack releases its registration and click subscribers.

The holder subscribes to BuoyStack.Clicked and locks the source for the full transfer. Waiting stacks have their input colliders disabled. BuoyVisual handles movement but does not receive input.

- Verify clicking different parts of a stack produces one Stack clicked log per press, including touch on a device.
- Verify UI blocks the click and CanReceiveInput = false prevents dispatch.
- Disable and re-enable a stack visual, then reinitialize the board: only active, current stacks should receive input.

- One column: one holder, one stack, matching buoy count and bottom-to-top colors.
- Three columns including an empty column: at most two visible stacks in source order; empty data columns are skipped.
- Reinitialize in Play Mode: old spawned holders are hidden immediately and destroyed; counts do not accumulate.
- Restart Play Mode: the source level data is unchanged.
- Change board CellSize: holder cell centers continue to align with conveyor cell centers.

## Migration from component-based gameplay objects

Remove the old Buoy, BuoyStack, BuoyStackHolder and BuoyStackHolderController components from existing prefabs/scenes (Unity may display them as missing scripts after compilation). Keep the Visual components and authored models. Place each Visual component on its prefab root so creation and cleanup own the complete prefab. Reassign the three Visual prefab fields on BoardManager; old gameplay component references are not compatible with the new field types.
BoardManager clears its controller when destroyed. Reinitialization clears old runtime objects and their visuals. Visual.Release hides the object immediately and schedules destruction; Clear is terminal for these objects, not a pooling operation.

## Stack to conveyor transfers

## Fixed holder visuals and lazy queue

The initial column count chooses a permanent one-slot or two-slot holder decoration (two slots when the count is greater than one). Only those visible stacks are instantiated; remaining non-empty columns stay in a data queue. The front stack alone accepts input and groups. Empty data columns are skipped.

After the final outgoing buoy arrives, the empty front stack is disabled and released. The rear stack advances to the front with a coroutine tween; a replacement is spawned in the rear slot at the same time if queued data remains. One-slot holders replace the front directly. The entire holder is disabled after the last stack finishes exporting and no queued column remains. Holders with only empty initial columns are disabled during initialization. Advance Duration on StackHolder.prefab defaults to 0.3 seconds. Holder input and receiving are locked during the tween, which is cancelled on level reset.

## Conveyor to stack transfers

The root trigger collider on StackHolder.prefab is assigned as Receive Collider. Keep the trigger volume extending toward the conveyor and overlapping the group root at the configured Root Y Offset. The group adds a small trigger sphere and kinematic Rigidbody at initialization; its Ignore Raycast layer prevents interference with click input. The project's layer collision matrix currently permits these contacts.

OnTriggerEnter and OnTriggerStay check the active stack's top color. Only fully loaded, unclaimed groups with matching color are accepted. Empty, busy and unconfigured holders do not receive. Stay allows a group that finishes loading or encounters a holder finishing its tween within the volume to be checked again. Only the front stack receives, not the waiting rear stack.

Accepted groups keep moving. The transfer launches overlapping flights from the group's top using LevelManager Receive Launch Delay and Receive Flight Duration, reserving destination indices and committing landings in order. Unlaunched buoys remain parented to the moving group. IsReceiving claims the group so other holders cannot receive it, even after it leaves the trigger. The empty group keeps moving until all flights land, then its conveyor reservation and GameObject are removed. The destination unlocks after completion.

DepartureHolder prevents a freshly exported group from immediately returning to its source; this exclusion clears once the group leaves the source trigger volume. Reset clears all unlanded flights before stacks and groups are destroyed.

Play Mode checks:
- Initialize holders with one, two and more than two columns; confirm at most two stack GameObjects are spawned and decorations remain fixed.
- Empty the front of a two-slot holder; verify the rear tweens forward while the next queued column appears in the rear slot.
- Exhaust all columns; keep the original holder decoration and disable click/receive behavior.
- Pass matching and mismatching groups through the trigger; only the matching group transfers and its root continues moving.
- Pass the same receiving group through another holder; do not duplicate or redirect the transfer.
- Export a group while it overlaps its source trigger; prevent immediate return, then allow return on a later loop.
- Reset during queue tween or overlapping receive flights; leave no orphan buoy, stale callback or group reservation.
- Check high conveyor speeds against the trigger width in Play Mode; physics detects overlaps on its fixed update schedule.

## Stack to conveyor transfer scheduling

BoardManager owns BuoyTransferController and ticks it in LateUpdate, after conveyor movement. This frame-based scheduler launches overlapping flights without asynchronous tasks surviving a level reset. Configure Transfer Speed and Launch Interval on BoardManager. Flight speed is clamped to at least conveyor speed + 0.5 world units/second so a moving destination remains catchable.

ConveyorController projects the holder's OutletCell (GridPosition + OutletDirection) onto its spline to find the entry. Configure Move Speed and Surface Offset on this component. Groups use spline Travel in world distance; closed paths wrap and open paths stop at their end. Group capacity is currently unlimited.

Each tap reserves one group and one slot per buoy, in top-to-bottom launch order. The group stays still until slot zero receives its buoy, then starts moving immediately. Flights chase the current world position of their slot every frame. Parent changes only on arrival. Buoys in flight belong to the transfer controller; source roots stay alive and stationary until the transfer completes. On reset, flights are cleared before source stacks and conveyor groups.

Buoy Height is 0.2 and Buoy Spacing is 0 on RingStack.prefab to preserve its previous 0.2 placement step. Adjust these to the authored mesh dimensions. Pole Transform references Stick. Its mesh bounds determine scaling while preserving the original base. Height is count * buoyHeight + max(0, count - 1) * spacing; empty poles use the prefab's Empty Stack Height (default 0.2 stack-local units; zero hides them). Capsule height follows the stack but cannot shrink below its diameter. Queue advancement uses the fixed visual slots and coroutine tween described above.

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

Verify simultaneous holder taps, a blocked entry, a loading group with followers, a full moving loop, zero speed, and reinitialization with pending requests in Play Mode. Waiting for space intentionally keeps the source locked. Conveyor-to-stack completion releases the group reservation and its GameObject.

## Receive timing fields

Configure holder receiving directly on LevelManager under Conveyor To Stack: Receive Flight Duration defaults to 0.4 seconds and Receive Launch Delay defaults to 0.12 seconds. Queue receiving has its own fields on StackQueueVisual, with the same defaults. No config ScriptableObjects are required. The first buoy launches immediately; buoy i launches at i * delay and lands after its flight duration. Zero delay launches the entire group together. Each transfer copies timing at startup, so Inspector edits apply to subsequent groups.

## Stack queue at conveyor exit

LevelDataSO.MaxStackInStackQueue sets the number of initially empty queue stacks (default 3). The level asset's custom inspector exposes this in Stack Queue / Max Stack In Stack Queue. There is no per-stack buoy limit. After an incoming group finishes landing, both holder and queue stacks consume matching top buoys in batches of five. Consuming removes them from the active list and animates each batch: the upper four shrink toward the bottom buoy, then the bottom buoy punches and shrinks to zero. Consume Punch Duration and Consume Punch Scale apply only to the bottom buoy; Consume Collapse Duration controls the upper four shrinking and Consume Shrink Duration controls the bottom buoy shrinking. All five return to a prefab-specific buoy pool only after the animation finishes. Consume Animation settings on BuoyStackVisual control the durations and punch scale. Animation advances through transfer ticks and respects board pause. Clearing a stack releases any unfinished consume groups. Rented buoys reset their scale, rotation, and flight/head state. Remaining receive reservations are rebased and queue counts are reduced before input resumes.

SampleScene includes StackQueue and StackQueueExit under the board root. StackQueue has a separate StackQueueSpawnRoot centered on the row. StackQueueVisual exposes Stack Spacing, Receive Flight Duration and Receive Launch Delay as serialized fields. Spacing is measured center-to-center along the spawn root's local X. With N stacks, stack i uses x = (i - (N - 1) * 0.5) * spacing. Move/rotate the root to position the entire row. Queue stacks bind to the board InputSystem and retain a short visible pole while empty. Tap exports the consecutive top color group to the last authored conveyor node (the start of backward movement), using normal entry spacing and group capacity. Input is locked during incoming/outgoing flights. Incoming groups can reserve the vacated slots while export is pending; their flights start after export completes. Departure groups ignore the queue exit until they leave its trigger.

StackQueueExit is a root trigger collider; place it at the desired exit for a closed conveyor, or near the final cell for an open conveyor. OnTriggerStay also handles groups that finish loading while still in the exit. Open paths additionally send a ReachedEnd notification when a loaded, unclaimed group reaches the end, so endpoint handling does not depend on trigger sampling. The current backward path ends at PathData.Cells[0]. The SampleScene exit is placed near that cell for LevelDataTest; moving the level path requires moving the exit trigger too. Keep exit and holder receiving volumes separate.

Selection scans queue stacks from index 0. An empty stack accepts any group; a non-empty stack accepts its own color. The first eligible stack wins, even when a later stack matches the color and an earlier one is empty. A reservation counts as occupied before any buoy lands. Groups of another color cannot claim that reservation; same-color groups append their own distinct landing indices and can fly concurrently. The transfer scheduler commits all landings in global stack order.

Accepted queue groups are detached from path occupancy immediately and stop at the exit while their buoys fly to the destination; the GameObject is removed after all landings. Groups already received by a board holder are not selected again. When no stack is eligible, queue failure is raised once. BoardManager pauses input, conveyor and transfers; LevelManager sets HasLost and raises Lost for future UI integration. Reinitializing the level clears reservations and resumes gameplay.

Verify in Play Mode:
- Change Max Stack In Stack Queue and confirm the number of empty stacks and centered spacing.
- Receive red, blue and red groups; fill stack 0 red, stack 1 blue, then append to stack 0.
- With an empty earlier stack and a later matching stack, choose the earlier empty one.
- Send different colors before prior flights land; reserve separate stacks, never mix colors.
- Send same-color groups with different receive timing values; commit without overlapping slots.
- Fill every stack with a different color, then send a new color; trigger Lost once and freeze gameplay.
- Receive 12 buoys of one color; consume two batches of five and leave two. Verify mixed top colors do not consume and simultaneous incoming groups still land after reservations are rebased.
- Reset during multiple queue flights or after a loss; clear all stacks/reservations and resume input.

Empty Stack Height on BuoyStackVisual sets the default pole height only when there are no buoys. Queue spawning uses this prefab value directly. Once buoys land, pole height follows buoy count and spacing. Changing the value in Play Mode refreshes the visual; empty stacks remain non-clickable.

## Conveyor group capacity and counter node

LevelDataSO.MaxBuoyInConveyor is the maximum number of groups on the conveyor (default 5), not the number of individual buoys. Active loading reservations count toward the limit; entry requests waiting for admission do not. New entries wait when the limit is reached. Detaching a group into StackQueue releases capacity immediately; a holder receive releases capacity when its flights finish. Clear/reinitialize resets the count.

In the level inspector, Conveyor Capacity exposes Max Buoy In Conveyor (Groups). MaxBuoyCounterTxt is a separate optional node: enable it and choose a free Grid Position, or click Place Counter On Board and select a free cell. The grid marks it MAX. It does not create a holder or require an outlet direction. Counter placement, path editing and board resizing validate its position. Undo/redo uses the existing serialized data writer.

Create your own counter prefab with MaxBuoyCounterTxt on its root and a TMP Text child assigned to Counter Text. Assign the prefab to LevelManager / Conveyor Counter / Max Buoy Counter Prefab. The system only instantiates that prefab and updates its text; it does not create a GameObject/text fallback or override the prefab's rotation, font, alignment or styling. Height Offset positions the prefab above the selected grid cell. LevelDataTest enables the counter at (4,4); SampleScene's prefab field is intentionally empty for your prefab. Missing prefab/text references produce explicit setup errors before resetting the board.

The counter subscribes to the conveyor's local GroupCountChanged event, displays current/max on binding and unsubscribes on reset/destruction. With four reserved groups and a maximum of five it displays 4/5.

## Rounded conveyor corners

ConveyorBuilder uses Bezier segments. Straight grid sections remain straight; each turn is replaced by an arc with tangents aligned to the incoming and outgoing segments. LevelManager / Conveyor Setup / Corner Radius defaults to 0.7 in board-local units and is applied on level initialization. Zero preserves sharp corners. Collinear cells are merged before rounding, including across closed seams. Corner trims are limited to 45% of each neighboring full straight segment to prevent overlapping turns. Open endpoint handles extend along the endpoint direction so Dreamteck can orient head meshes at exactly percent 1. Open endpoints stay at their authored cells; closed paths round the seam as well. Mesh and buoy movement share the same spline. ConveyorBuilder uses Uniform sampling so equal spline percentages represent approximately equal traveled distances, preventing Middle mesh copies from stretching on long straight segments and compressing around short arcs. Conveyor movement, entry projection and length calculations use the same cached sample collection. For a rounded inner edge, the effective center radius must exceed half the conveyor width; short segments can still limit the radius. Previously saved Inspector radius values are preserved. Inspect mesh tessellation and holder trigger overlap in Play Mode after adjusting the radius.

## Stack polish and flight animation

StackQueueVisual / Fixed Stack Height controls a constant pole height for queue stacks, including empty and consumed stacks. Holder stacks retain dynamic pole height. Queue Receive Flight Duration is the spinning approach duration; Receive Descent Duration controls the non-spinning descent. Receive Top Clearance is measured in world units along stack up, above the higher of the pole top and reserved buoy slot. Receive Launch Delay preserves staggered launch timing. Reservations and consume rebasing remain active throughout both phases.

BuoyVisual / Spin Root optionally references a model child. If omitted, the movement root spins. Each flight rotates 180 degrees around local X and retains its final orientation at landing. Queue approach rotation ends before descent; other transfers spin while flying directly to their destination.

On the holder prefab, assign Remaining Stack Text (TMP), Img Tick and Status Root. Keep the text and tick under this root with a local orientation suitable for a camera-facing world-space display. Status Camera is optional and defaults to Camera.main. No text or canvas is generated at runtime. Only holders initially using the multi-stack decoration show status: text counts all stacks behind the active stack, including pending columns; zero remaining shows the tick. An exhausted holder disables everything.

BuoyVisual / Flight Arc Height controls the upward parabolic lift above the straight flight path (default 1 world unit). Both export and receive flights use this arc; different endpoint heights increase the lift automatically to ensure an upward launch. Queue receives arc to their above-stack approach point, then descend without rotation. Export launch delays remain unchanged and arrivals commit in slot order.

Assign BuoyVisual / Head to the head child in the buoy prefab. BeginFlight hides it and EndFlight shows it after landing, including after the queue descent phase. Leave the reference empty for buoys without a head.

LevelManager / Conveyor Setup / Water Speed controls the water shader _WaterSpeed property independently of group movement. Default 1; zero stops UV X scrolling and negative values reverse direction. Values apply at level initialization and update live in Play Mode through OnValidate. ConveyorController applies per-material property blocks only to water-compatible material slots on the spline mesh renderer, preserving the material asset.

Holder receive height is reserved for the entire group at detection. BuoyStackVisual / Incoming Group / Receive Height Tween Duration controls the single growth tween; individual landings do not resize the pole while the reservation is active. Receive Descent Duration and Receive Top Clearance control holder approach and descent just like queue receiving. Incoming buoy approach points use the final reserved pole top even while growth is in progress. Growth advances through transfer ticks, so level pause freezes it. After the group lands, height follows actual contents again, including consume. Queue poles remain fixed.

Conveyor head facing follows the negative spline tangent because conveyor movement is backward. BuoyVisual caches the prefab head orientation offset at Awake; head world rotation follows travel direction independently of body flip. Newly landed buoys receive the current direction immediately. This changes head orientation, not its attachment position.

QuickOutline shares reference-counted Mask/Fill material pairs for identical mode, color and width, and assigns sharedMaterials without creating per-renderer copies. Both outline shaders support instancing variants. Changing outline settings switches material groups; disable removes passes and destroy releases shared references. Stencil remains the existing shared Ref 1. Verify actual per-pass batching and transparent render ordering in Unity Frame Debugger.

Receive completion callbacks wait until all consume animations on their stack finish. This keeps holders busy and prevents empty-stack cleanup/promotion from releasing the animated buoys early; queue input also remains locked until completion. Reservation counts and incoming destination indices are still rebased immediately. Board teardown cancels the deferred callbacks.

ColorDataSO stores an optional Material per color entry alongside its code, display name, and editor color. Buoys use the level palette directly, without a separate material config asset on the prefab. Assigned materials are shared; entries with no material tint the original prefab materials through MaterialPropertyBlock. Pooled buoys restore the original materials before using this fallback.

Empty holder stacks shrink to zero before returning to their prefab pool. The rear stack advances only after this animation completes. When no visible or pending stacks remain, the holder also shrinks and returns to its pool. Disappear Duration on each visual controls its animation, which respects transfer pause. Level teardown cancels active disappearance animations and releases immediately. Queue stacks remain available when empty. Renting stack and holder visuals resets scale and input/receive state; holder poles keep their final height until shrinking completes.

Consume transform animation and its tick state belong to BuoyStackVisual. BuoyStack owns the consumed groups and releases them when the visual completion callback runs. Clearing the stack cancels visual callbacks before releasing unfinished groups.

Stack and holder disappearance starts with a scale punch, then shrinks to zero. Disappear Punch Duration controls the growth time, Disappear Punch Scale controls the peak relative to the original scale, and Disappear Duration controls the subsequent shrink. Pool release and stack promotion wait for both parts to finish.

Holder stacks orient buoy heads along the holder root forward/up, which represents OutletDirection in board space. Initial and promoted stacks use this facing, and received buoy heads are aligned after placement so the flight rotation and reparenting cannot override it. Queue stacks retain their existing behavior.
