# Class diagram - current implementation

Source snapshot: 2026-09-17. Scope: all Assets/Scripts/GameCore types, related level data, palette and enum types, and QuickOutline. Unity/Dreamteck internals and editor tooling are external. This documents existing code; fixed holder visuals, queue tweening and transfers from a moving group are not implemented yet.

## Legend

`+` public, `-` private (including implicit private), `~` internal, `#` protected, `$` static/const. Fields and properties have no parentheses; C# declarations below retain attributes and property accessors. `*--` lifecycle ownership, `o--` transferable contents, `-->` reference, `..>` dependency, `<|--` inheritance, `<|..` interface implementation. Mermaid generics use `~Type~`; multi-argument generics use `_`. The C# listings are declaration summaries, not compilable class bodies.

## Overview relationships

```mermaid
classDiagram
direction LR
LevelManager --> LevelDataSO : reads
LevelManager --> BoardManager : initializes
LevelManager --> ConveyorController : configures
LevelManager --> InputSystem
BoardManager *-- BuoyStackHolderController
BoardManager *-- BuoyTransferController : ticks in LateUpdate
BoardManager --> ConveyorController
BuoyStackHolderController *-- BuoyStackHolder : creates
BuoyStackHolderController ..> BuoyStackVisual : instantiates
BuoyStackHolderController ..> BuoyVisual : instantiates
BuoyStackHolder *-- BuoyStack : queue
BuoyStackHolder --> BuoyStackHolderVisual
BuoyStackHolder --> BuoyTransferController
BuoyStack *-- Buoy : stack contents
BuoyStack --> BuoyStackVisual
Buoy --> BuoyVisual
BuoyVisual --> BuoyColorConfig
BuoyStackVisual ..|> IInputReceiver
BuoyStackVisual --> InputSystem : registers collider
InputSystem --> IInputReceiver : dispatches click
ConveyorController *-- ConveyorBuilder
ConveyorController *-- ConveyorBuoyGroup
ConveyorController --> BuoyStackHolder : receiver ports
ConveyorBuoyGroup o-- Buoy : current contents
MonoBehaviour <|-- ConveyorBuoyGroup
BuoyTransferController --> BuoyStack : removes or adds buoy
BuoyTransferController --> ConveyorBuoyGroup : receives or takes buoy
BuoyTransferController --> ConveyorController : reserves or removes group
ConveyorBuilder ..> BoardCoordinates
BuoyStackHolderController ..> BoardCoordinates
ConveyorController ..> BoardCoordinates
LevelDataSO *-- BoardData
LevelDataSO *-- PathData
LevelDataSO *-- BuoyNodeData
LevelDataSO --> ColorDataSO
BuoyNodeData *-- BuoyColumnData
BuoyColumnData *-- BuoyData
BuoyColumnData *-- ColumnElementData
BuoyData *-- BuoyElementData
ColorDataSO *-- ColorEntryData
BuoyColorConfig *-- ColorMaterialEntry
BuoyTransferController *-- Transfer
BuoyTransferController *-- ReceiveTransfer
Transfer *-- Flight
ConveyorController *-- PathMoveSlot
ConveyorController *-- GroupPosition
ConveyorController *-- ReceiverPort
ConveyorController *-- EnterRequest
Outline *-- ListVector3
```

## Level, board and input

```mermaid
classDiagram
direction LR
class BoardCoordinates {
    +CellToLocal(BoardData board, Vector2Int cell) Vector3$
}
class BoardManager {
    -Transform boardRoot
    -ConveyorController conveyorController
    -BuoyVisual buoyPrefab
    -BuoyStackVisual buoyStackPrefab
    -BuoyStackHolderVisual buoyStackHolderPrefab
    -InputSystem inputSystem
    -float transferSpeed
    -float launchInterval
    +Configure(Transform root, ConveyorController conveyor, InputSystem input, BuoyVisual buoy, BuoyStackVisual stack, BuoyStackHolderVisual holder) void
    +SetMotionSettings(float speed, float interval) void
    -BuoyTransferController transfers
    -LateUpdate() void
    -BuoyStackHolderController buoyStackHolderController
    +InitBoard(BoardData boardData, PathData pathData, IReadOnlyList~BuoyNodeData~ nodes, ColorDataSO colors) void
    -OnDestroy() void
}
MonoBehaviour <|-- BoardManager
class IInputReceiver {
    <<interface>>
    +bool CanReceiveInput
    +OnClick() void
}
class InputSystem {
    -Camera inputCamera
    -LayerMask raycastMask
    -float maxDistance
    -Dictionary~Collider_IInputReceiver~ receivers
    -List~RaycastResult~ uiHits
    +Register(Collider inputCollider, IInputReceiver receiver) void
    +Unregister(Collider inputCollider, IInputReceiver receiver) void
    -Update() void
    -ProcessPress(Vector2 screenPosition, int pointerId) void
    -IsOverUI(Vector2 screenPosition, int pointerId) bool
    -OnDestroy() void
}
MonoBehaviour <|-- InputSystem
class LevelManager {
    -LevelDataSO levelData
    -BoardManager boardManager
    -Transform boardRoot
    -ConveyorController conveyorController
    -InputSystem inputSystem
    -BuoyVisual buoyPrefab
    -BuoyStackVisual buoyStackPrefab
    -BuoyStackHolderVisual buoyStackHolderPrefab
    -SplineComputer splineComputer
    -SplineMesh splineMesh
    -float moveSpeed
    -float rootYOffset
    -float pathMoveSlotSpacing
    -float conveyorGroupGap
    -float transferSpeed
    -float launchInterval
    -OnValidate() void
    -ApplyMotionSettings() void
    -Start() void
    +InitLevel() void
}
MonoBehaviour <|-- LevelManager
```

### C# declarations and attributes

#### BoardCoordinates

Source: [Assets/Scripts/GameCore/BoardSystem/BoardCoordinates.cs](../Assets/Scripts/GameCore/BoardSystem/BoardCoordinates.cs).

```csharp
public static Vector3 CellToLocal(BoardData board, Vector2Int cell)
```

#### BoardManager

Source: [Assets/Scripts/GameCore/BoardSystem/BoardManager.cs](../Assets/Scripts/GameCore/BoardSystem/BoardManager.cs).

```csharp
[Tooltip("Board center and orientation. Keep its world scale at one for CellSize in world units.")]
private Transform boardRoot
private ConveyorController conveyorController
private BuoyVisual buoyPrefab
private BuoyStackVisual buoyStackPrefab
private BuoyStackHolderVisual buoyStackHolderPrefab
private InputSystem inputSystem
private float transferSpeed = 4f
private float launchInterval = 0.12f
public void Configure(Transform root, ConveyorController conveyor, InputSystem input, BuoyVisual buoy, BuoyStackVisual stack, BuoyStackHolderVisual holder)
public void SetMotionSettings(float speed, float interval)
private BuoyTransferController transfers
private void LateUpdate()
private readonly BuoyStackHolderController buoyStackHolderController = new BuoyStackHolderController()
public void InitBoard(BoardData boardData, PathData pathData, IReadOnlyList<BuoyNodeData> nodes, ColorDataSO colors)
private void OnDestroy()
```

#### IInputReceiver

Source: [Assets/Scripts/GameCore/InputSystem/IInputReceiver.cs](../Assets/Scripts/GameCore/InputSystem/IInputReceiver.cs).

```csharp
bool CanReceiveInput { get; }
void OnClick()
```

#### InputSystem

Source: [Assets/Scripts/GameCore/InputSystem/InputSystem.cs](../Assets/Scripts/GameCore/InputSystem/InputSystem.cs).

```csharp
[SerializeField]
private Camera inputCamera
[SerializeField]
private LayerMask raycastMask = Physics.DefaultRaycastLayers
[SerializeField, Min(0.01f)]
private float maxDistance = 1000f
private readonly Dictionary<Collider, IInputReceiver> receivers = new Dictionary<Collider, IInputReceiver>()
private readonly List<RaycastResult> uiHits = new List<RaycastResult>()
public void Register(Collider inputCollider, IInputReceiver receiver)
public void Unregister(Collider inputCollider, IInputReceiver receiver)
private void Update()
private void ProcessPress(Vector2 screenPosition, int pointerId)
private bool IsOverUI(Vector2 screenPosition, int pointerId)
private void OnDestroy()
```

#### LevelManager

Source: [Assets/Scripts/GameCore/LevelSystem/LevelManager.cs](../Assets/Scripts/GameCore/LevelSystem/LevelManager.cs).

```csharp
[SerializeField]
private LevelDataSO levelData
[SerializeField]
private BoardManager boardManager
[Header("Board Setup")] [SerializeField]
private Transform boardRoot
[SerializeField]
private ConveyorController conveyorController
[SerializeField]
private InputSystem inputSystem
[SerializeField]
private BuoyVisual buoyPrefab
[SerializeField]
private BuoyStackVisual buoyStackPrefab
[SerializeField]
private BuoyStackHolderVisual buoyStackHolderPrefab
[Header("Conveyor Setup")] [SerializeField]
private SplineComputer splineComputer
[SerializeField]
private SplineMesh splineMesh
[SerializeField, Min(0f)]
private float moveSpeed = 1f
[Tooltip("Group root height above the spline, along the board's local up axis, in world units.")] [SerializeField]
private float rootYOffset = 0.2f
[SerializeField, Min(0.01f)]
private float pathMoveSlotSpacing = 0.3f
[SerializeField, Min(0.01f)]
private float conveyorGroupGap = 0.6f
[Header("Transfer Setup")] [SerializeField, Min(0.01f)]
private float transferSpeed = 4f
[SerializeField, Min(0f)]
private float launchInterval = 0.12f
private void OnValidate()
private void ApplyMotionSettings()
private void Start()
public void InitLevel()
```

## Buoys, stacks and holders

```mermaid
classDiagram
direction LR
class Buoy {
    -BuoyVisual visual
    +int ColorCode
    +BuoyElementType Type
    +int TypeCount
    +IReadOnlyList~string~ Elements
    ~BuoyVisual Visual
    -bool cleared
    +Buoy(BuoyData source, ColorDataSO colors, BuoyVisual visual)
    +Clear() void
}
class BuoyColorConfig {
    -List~ColorMaterialEntry~ colors
    -Dictionary~int_Material~ materials
    +GetMaterial(int colorId) Material
    -OnEnable() void
    -OnValidate() void
    -BuildLookup() void
}
ScriptableObject <|-- BuoyColorConfig
class ColorMaterialEntry {
    -int colorId
    -Material material
    +int ColorId
    +Material Material
}
class BuoyStack {
    -BuoyStackVisual visual
    -List~Buoy~ buoys
    -bool cleared
    ~BuoyStackVisual Visual
    -bool canReceiveInput
    +bool CanReceiveInput
    +GetTopGroup() List~Buoy~
    ~RemoveTop(Buoy buoy) void
    +Action~BuoyStack~ Clicked
    +IReadOnlyList~Buoy~ Buoys
    +ColumnElementType Type
    +int TypeCount
    +IReadOnlyList~string~ Elements
    +BuoyStack(BuoyColumnData source, BuoyStackVisual visual)
    ~AddBuoy(Buoy buoy) void
    +OnClick() void
    +Clear() void
}
class BuoyStackHolder {
    -BuoyStackHolderVisual visual
    -List~BuoyStack~ stacks
    +IReadOnlyList~BuoyStack~ Stacks
    +BuoyStack ActiveStack
    +Vector2Int GridPosition
    +Vector2Int OutletDirection
    +Vector2Int OutletCell
    +BuoyStackHolder(BuoyNodeData source, BuoyStackHolderVisual visual)
    ~AddStack(BuoyStack stack, BuoyStackVisual stackVisual) void
    -BuoyTransferController transfer
    -bool busy
    ~CanReceive(ConveyorBuoyGroup group) bool
    ~TryReceive(ConveyorBuoyGroup group) bool
    +InitializeTransfers(BuoyTransferController controller) void
    -OnStackClicked(BuoyStack stack) void
    -FinishTransfer() void
    -AdvanceQueue() void
    +Clear() void
}
class BuoyStackHolderController {
    -List~BuoyStackHolder~ holders
    +IReadOnlyList~BuoyStackHolder~ Holders
    +BuoyStackHolderController()
    +InitHolders(BoardData board, IReadOnlyList~BuoyNodeData~ nodes, ColorDataSO colors, Transform boardRoot, BuoyStackHolderVisual holderPrefab, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab, InputSystem inputSystem) void
    -CreateStack(BuoyStackHolder holder, Transform parent, BuoyColumnData source, int index, ColorDataSO colors, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab, InputSystem inputSystem) void$
    +Clear() void
}
class BuoyStackHolderVisual {
    -GameObject singleStackVisual
    -GameObject multipleStackVisual
    -Transform stackRoot
    -Vector3 firstStackOffset
    -Vector3 stackStep
    +SetOutletDirection(Vector2Int direction) void
    +Refresh(int stackCount) void
    +PlaceStack(Transform stack, int index) void
    -bool released
    +Release() void
}
MonoBehaviour <|-- BuoyStackHolderVisual
class BuoyStackVisual {
    -Transform buoyRoot
    -Vector3 firstBuoyOffset
    -float buoySpacing
    -float buoyHeight
    -Transform poleTransform
    +float Step
    -Vector3 poleScale, polePosition
    -Bounds poleBounds
    -bool poleCached
    -bool inputEnabled
    -int count
    +SetInputEnabled(bool enabled) void
    +RefreshHeight(int buoyCount) void
    -Collider[] inputColliders
    -InputSystem inputSystem
    -BuoyStack owner
    +bool CanReceiveInput
    +BindInput(BuoyStack stack, InputSystem system) void
    +OnClick() void
    -OnEnable() void
    -OnDisable() void
    -RegisterInput() void
    -UnregisterInput() void
    +GetBuoyPosition(int index) Vector3
    +PlaceBuoy(Transform buoy, int index) void
    -bool released
    +Release() void
}
MonoBehaviour <|-- BuoyStackVisual
IInputReceiver <|.. BuoyStackVisual
class BuoyVisual {
    -Renderer[] colorRenderers
    -BuoyColorConfig colorConfig
    -MaterialPropertyBlock propertyBlock
    -int BaseColor$
    +Refresh(int colorId, Color color) void
    +MoveTowards(Vector3 target, float distance) bool
    -bool released
    +Release() void
}
MonoBehaviour <|-- BuoyVisual
```

### C# declarations and attributes

#### Buoy

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/Buoy.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/Buoy.cs).

```csharp
private readonly BuoyVisual visual
public int ColorCode { get; }
public BuoyElementType Type { get; }
public int TypeCount { get; }
public IReadOnlyList<string> Elements { get; }
internal BuoyVisual Visual => visual;
private bool cleared
public Buoy(BuoyData source, ColorDataSO colors, BuoyVisual visual)
public void Clear()
```

#### BuoyColorConfig

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyColorConfig.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyColorConfig.cs).

```csharp
[SerializeField]
private List<ColorMaterialEntry> colors = new List<ColorMaterialEntry>()
private Dictionary<int, Material> materials
public Material GetMaterial(int colorId)
private void OnEnable()
private void OnValidate()
private void BuildLookup()
```

#### ColorMaterialEntry

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyColorConfig.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyColorConfig.cs).

```csharp
[SerializeField]
private int colorId
[SerializeField]
private Material material
public int ColorId => colorId;
public Material Material => material;
```

#### BuoyStack

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStack.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStack.cs).

```csharp
private readonly BuoyStackVisual visual
private readonly List<Buoy> buoys = new List<Buoy>()
private bool cleared
internal BuoyStackVisual Visual => visual;
private bool canReceiveInput
public bool CanReceiveInput
public List<Buoy> GetTopGroup()
internal void RemoveTop(Buoy buoy)
public event Action<BuoyStack> Clicked
public IReadOnlyList<Buoy> Buoys { get; }
public ColumnElementType Type { get; }
public int TypeCount { get; }
public IReadOnlyList<string> Elements { get; }
public BuoyStack(BuoyColumnData source, BuoyStackVisual visual)
internal void AddBuoy(Buoy buoy)
public void OnClick()
public void Clear()
```

#### BuoyStackHolder

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolder.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolder.cs).

```csharp
private readonly BuoyStackHolderVisual visual
private readonly List<BuoyStack> stacks = new List<BuoyStack>()
public IReadOnlyList<BuoyStack> Stacks { get; }
public BuoyStack ActiveStack => stacks.Count > 0 ? stacks[0] : null;
public Vector2Int GridPosition { get; }
public Vector2Int OutletDirection { get; }
public Vector2Int OutletCell => GridPosition + OutletDirection;
public BuoyStackHolder(BuoyNodeData source, BuoyStackHolderVisual visual)
internal void AddStack(BuoyStack stack, BuoyStackVisual stackVisual)
private BuoyTransferController transfer
private bool busy
internal bool CanReceive(ConveyorBuoyGroup group)
internal bool TryReceive(ConveyorBuoyGroup group)
public void InitializeTransfers(BuoyTransferController controller)
private void OnStackClicked(BuoyStack stack)
private void FinishTransfer()
private void AdvanceQueue()
public void Clear()
```

#### BuoyStackHolderController

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolderController.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolderController.cs).

```csharp
private readonly List<BuoyStackHolder> holders = new List<BuoyStackHolder>()
public IReadOnlyList<BuoyStackHolder> Holders { get; }
public BuoyStackHolderController()
public void InitHolders(BoardData board, IReadOnlyList<BuoyNodeData> nodes, ColorDataSO colors, Transform boardRoot, BuoyStackHolderVisual holderPrefab, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab, InputSystem inputSystem)
private static void CreateStack(BuoyStackHolder holder, Transform parent, BuoyColumnData source, int index, ColorDataSO colors, BuoyStackVisual stackPrefab, BuoyVisual buoyPrefab, InputSystem inputSystem)
public void Clear()
```

#### BuoyStackHolderVisual

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolderVisual.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackHolderVisual.cs).

```csharp
[Tooltip("Optional decorative objects, separate from the stack root.")] [SerializeField]
private GameObject singleStackVisual
[SerializeField]
private GameObject multipleStackVisual
[SerializeField]
private Transform stackRoot
[SerializeField]
private Vector3 firstStackOffset
[SerializeField]
private Vector3 stackStep = new Vector3(0f, 0f, -0.8f)
public void SetOutletDirection(Vector2Int direction)
public void Refresh(int stackCount)
public void PlaceStack(Transform stack, int index)
private bool released
public void Release()
```

#### BuoyStackVisual

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackVisual.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyStackVisual.cs).

```csharp
[SerializeField]
private Transform buoyRoot
[SerializeField]
private Vector3 firstBuoyOffset = new Vector3(0f, 0.2f, 0f)
[SerializeField, Min(0f)]
private float buoySpacing = 0f
[SerializeField, Min(0.01f)]
private float buoyHeight = 0.2f
[SerializeField]
private Transform poleTransform
public float Step => buoyHeight + buoySpacing;
private Vector3 poleScale, polePosition
private Bounds poleBounds
private bool poleCached
private bool inputEnabled
private int count
public void SetInputEnabled(bool enabled)
public void RefreshHeight(int buoyCount)
[SerializeField]
private Collider[] inputColliders
private InputSystem inputSystem
private BuoyStack owner
public bool CanReceiveInput => !released && isActiveAndEnabled && owner != null && owner.CanReceiveInput;
public void BindInput(BuoyStack stack, InputSystem system)
public void OnClick()
private void OnEnable()
private void OnDisable()
private void RegisterInput()
private void UnregisterInput()
public Vector3 GetBuoyPosition(int index)
public void PlaceBuoy(Transform buoy, int index)
private bool released
public void Release()
```

#### BuoyVisual

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyVisual.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyVisual.cs).

```csharp
[Tooltip("Only renderers which should receive the buoy color.")] [SerializeField]
private Renderer[] colorRenderers
[SerializeField]
private BuoyColorConfig colorConfig
private MaterialPropertyBlock propertyBlock
private static readonly int BaseColor = Shader.PropertyToID("_Color")
public void Refresh(int colorId, Color color)
public bool MoveTowards(Vector3 target, float distance)
private bool released
public void Release()
```

## Conveyor

```mermaid
classDiagram
direction LR
class ConveyorBuilder {
    -SplineComputer splineComputer
    -SplineMesh splineMesh
    +ConveyorBuilder(SplineComputer splineComputer, SplineMesh splineMesh)
    +BuildConveyor(BoardData boardData, PathData pathData, Transform boardRoot) void
    -Validate(BoardData boardData, PathData pathData, Transform boardRoot) void
}
class ConveyorBuoyGroup {
    -List~Buoy~ buoys
    +IReadOnlyList~Buoy~ Buoys
    ~double Percent
    ~bool Moving
    ~bool IsLoaded
    +HasColor(int colorCode) bool
    ~TakeTop() Buoy
    -float spacing
    +Initialize(double percent, float spacing) void
    +GetSlotPosition(int slot) Vector3
    +Receive(Buoy buoy, int slot) void
    +Clear() void
    -OnDestroy() void
}
MonoBehaviour <|-- ConveyorBuoyGroup
class ConveyorController {
    -SplineComputer splineComputer
    -SplineMesh splineMesh
    -float moveSpeed
    -float rootYOffset
    +Configure(SplineComputer computer, SplineMesh mesh) void
    +SetMotionSettings(float speed, float yOffset) void
    +float MoveSpeed
    -List~ConveyorBuoyGroup~ groups
    -BoardData board
    -Transform root
    -bool closed
    -float length
    -ConveyorBuilder conveyorBuilder
    -List~PathMoveSlot~ _pathMoveSlots
    -List~EnterRequest~ waiting
    -List~GroupPosition~ positions
    -float slotSpacing
    -float groupGap
    -List~ReceiverPort~ receivers
    +ConfigureReceivers(IReadOnlyList~BuoyStackHolder~ holders) void
    ~RemoveGroup(ConveyorBuoyGroup group) void
    +ConfigurePathSlots(float spacing, float minimumGap) void
    +RequestEntry(Vector2Int outletCell, float spacing, Func~Vector3_float~ estimateArrival, Action~ConveyorBuoyGroup~ accepted) void
    -ProcessEntries() void
    +CanReceiveFirst(ConveyorBuoyGroup group) bool
    +GetEntryHoldingOffset() Vector3
    -EntryDistance(float a, float b) float
    -PercentAt(float distance) double
    -PlaceGroup(GroupPosition position) void
    -Update() void
    +ClearGroups() void
    -OnDestroy() void
    +InitConveyor(BoardData boardData, PathData pathData, Transform boardRoot) void
}
MonoBehaviour <|-- ConveyorController
class PathMoveSlot {
    +float Distance
    +double Percent
}
class GroupPosition {
    +ConveyorBuoyGroup Group
    +float Distance, Step
    +ReceiverPort Receiver
    +float ReceiverTravel
}
class ReceiverPort {
    +BuoyStackHolder Holder
    +float Distance
}
class EnterRequest {
    +float Distance, Spacing
    +Func~Vector3_float~ EstimateArrival
    +Action~ConveyorBuoyGroup~ Accepted
}
```

### C# declarations and attributes

#### ConveyorBuilder

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorBuilder.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorBuilder.cs).

```csharp
private readonly SplineComputer splineComputer
private readonly SplineMesh splineMesh
public ConveyorBuilder(SplineComputer splineComputer, SplineMesh splineMesh)
public void BuildConveyor(BoardData boardData, PathData pathData, Transform boardRoot)
private void Validate(BoardData boardData, PathData pathData, Transform boardRoot)
```

#### ConveyorBuoyGroup

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorBuoyGroup.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorBuoyGroup.cs).

```csharp
private readonly List<Buoy> buoys = new List<Buoy>()
public IReadOnlyList<Buoy> Buoys => buoys;
internal double Percent
internal bool Moving
internal bool IsLoaded { get; set; }
public bool HasColor(int colorCode)
internal Buoy TakeTop()
private float spacing
public void Initialize(double percent, float spacing)
public Vector3 GetSlotPosition(int slot)
public void Receive(Buoy buoy, int slot)
public void Clear()
private void OnDestroy()
```

#### ConveyorController

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs).

```csharp
private SplineComputer splineComputer
private SplineMesh splineMesh
private float moveSpeed = 1f
private float rootYOffset = 0.2f
public void Configure(SplineComputer computer, SplineMesh mesh)
public void SetMotionSettings(float speed, float yOffset)
public float MoveSpeed => Mathf.Max(0f, moveSpeed);
private readonly List<ConveyorBuoyGroup> groups = new List<ConveyorBuoyGroup>()
private BoardData board
private Transform root
private bool closed
private float length
private ConveyorBuilder conveyorBuilder
private readonly List<PathMoveSlot> _pathMoveSlots = new List<PathMoveSlot>()
private readonly List<EnterRequest> waiting = new List<EnterRequest>()
private readonly List<GroupPosition> positions = new List<GroupPosition>()
private float slotSpacing = 0.3f
private float groupGap = 0.6f
private readonly List<ReceiverPort> receivers = new List<ReceiverPort>()
public void ConfigureReceivers(IReadOnlyList<BuoyStackHolder> holders)
internal void RemoveGroup(ConveyorBuoyGroup group)
public void ConfigurePathSlots(float spacing, float minimumGap)
public void RequestEntry(Vector2Int outletCell, float spacing, Func<Vector3, float> estimateArrival, Action<ConveyorBuoyGroup> accepted)
private void ProcessEntries()
public bool CanReceiveFirst(ConveyorBuoyGroup group)
public Vector3 GetEntryHoldingOffset()
private float EntryDistance(float a, float b)
private double PercentAt(float distance)
private void PlaceGroup(GroupPosition position)
private void Update()
public void ClearGroups()
private void OnDestroy()
public void InitConveyor(BoardData boardData, PathData pathData, Transform boardRoot)
```

#### PathMoveSlot

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs).

```csharp
public float Distance
public double Percent
```

#### GroupPosition

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs).

```csharp
public ConveyorBuoyGroup Group
public float Distance, Step
public ReceiverPort Receiver
public float ReceiverTravel
```

#### ReceiverPort

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs).

```csharp
public BuoyStackHolder Holder
public float Distance
```

#### EnterRequest

Source: [Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs](../Assets/Scripts/GameCore/BoardSystem/Conveyor/ConveyorController.cs).

```csharp
public float Distance, Spacing
public Func<Vector3, float> EstimateArrival
public Action<ConveyorBuoyGroup> Accepted
```

## Transfers and nested classes

```mermaid
classDiagram
direction LR
class BuoyTransferController {
    -List~Transfer~ transfers
    -List~ReceiveTransfer~ receives
    -ConveyorController conveyor
    -int generation
    -float flightSpeed
    +BuoyTransferController(ConveyorController conveyor)
    ~BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, Action completed) void
    +Begin(BuoyStack stack, Vector2Int outlet, Action completed) void
    +Tick(float deltaTime, float speed, float interval) void
    -TickReceives(float deltaTime, float speed, float interval) void
    +Clear() void
}
class Transfer {
    +BuoyStack Stack
    +List~Buoy~ Selected
    +ConveyorBuoyGroup Group
    +Action Completed
    +List~Flight~ Flights
    +int Next, Arrived
    +float Timer, Elapsed, ExpectedArrival
}
class ReceiveTransfer {
    +BuoyStack Stack
    +ConveyorBuoyGroup Group
    +Action Completed
    +Buoy InFlight
    +float Timer
}
class Flight {
    +Buoy Buoy
    +int Slot
    +bool Arrived
}
```

### C# declarations and attributes

#### BuoyTransferController

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs).

```csharp
private readonly List<Transfer> transfers = new List<Transfer>()
private readonly List<ReceiveTransfer> receives = new List<ReceiveTransfer>()
private readonly ConveyorController conveyor
private int generation
private float flightSpeed = 4f
public BuoyTransferController(ConveyorController conveyor)
internal void BeginReceive(BuoyStack stack, ConveyorBuoyGroup group, Action completed)
public void Begin(BuoyStack stack, Vector2Int outlet, Action completed)
public void Tick(float deltaTime, float speed, float interval)
private void TickReceives(float deltaTime, float speed, float interval)
public void Clear()
```

#### Transfer

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs).

```csharp
public BuoyStack Stack
public List<Buoy> Selected
public ConveyorBuoyGroup Group
public Action Completed
public readonly List<Flight> Flights = new List<Flight>()
public int Next, Arrived
public float Timer, Elapsed, ExpectedArrival
```

#### ReceiveTransfer

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs).

```csharp
public BuoyStack Stack
public ConveyorBuoyGroup Group
public Action Completed
public Buoy InFlight
public float Timer
```

#### Flight

Source: [Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs](../Assets/Scripts/GameCore/BoardSystem/Buoys/BuoyTransferController.cs).

```csharp
public Buoy Buoy
public int Slot
public bool Arrived
```

## Level data and enums

```mermaid
classDiagram
direction LR
class LevelDataSO {
    -ColorDataSO colorData
    -BoardData board
    -PathData path
    -List~BuoyNodeData~ buoyNodes
    +ColorDataSO ColorData
    +BoardData Board
    +PathData Path
    +IReadOnlyList~BuoyNodeData~ BuoyNodes
}
ScriptableObject <|-- LevelDataSO
class BoardData {
    -int width
    -int height
    -float cellSize
    +int Width
    +int Height
    +float CellSize
}
class PathData {
    -bool isClosed
    -List~Vector2Int~ cells
    +bool IsClosed
    +IReadOnlyList~Vector2Int~ Cells
}
class BuoyNodeData {
    -Vector2Int gridPosition
    -Vector2Int outletDirection
    -List~BuoyColumnData~ columns
    +Vector2Int GridPosition
    +Vector2Int OutletDirection
    +Vector2Int OutletCell
    +IReadOnlyList~BuoyColumnData~ Columns
}
class BuoyColumnData {
    +int DefaultBuoyCount$
    -ColumnElementType columnType
    -int columnTypeCount
    -List~ColumnElementData~ elements
    -List~BuoyData~ buoys
    +ColumnElementType ColumnType
    +int ColumnTypeCount
    +IReadOnlyList~ColumnElementData~ Elements
    +IReadOnlyList~BuoyData~ Buoys
    +int BuoyCount
    -CreateDefaultBuoys() List~BuoyData~$
}
class BuoyData {
    -int colorCode
    -BuoyElementType buoyType
    -int buoyTypeCount
    -List~BuoyElementData~ elements
    +int ColorCode
    +BuoyElementType BuoyType
    +int BuoyTypeCount
    +IReadOnlyList~BuoyElementData~ Elements
}
class ColumnElementData {
    -string elementId
    +string ElementId
}
class BuoyElementData {
    -string elementId
    +string ElementId
}
class ColorDataSO {
    -List~ColorEntryData~ colors
    +IReadOnlyList~ColorEntryData~ Colors
    +TryGetColor(int code, out Color color) bool
}
ScriptableObject <|-- ColorDataSO
class ColorEntryData {
    -int code
    -string displayName
    -Color color
    +int Code
    +string DisplayName
    +Color Color
}
class ColumnElementType {
    <<enumeration>>
    NormalPeg = 0
    Crate = 1
    LockedPeg = 2
    IcePeg = 3
    ConveyorCounterBouyTxt = 4
}
class BuoyElementType {
    <<enumeration>>
    NormalBouy = 0
    Hidden = 1
}
```

### C# declarations and attributes

#### LevelDataSO

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private ColorDataSO colorData
[SerializeField]
private BoardData board = new BoardData()
[SerializeField]
private PathData path = new PathData()
[SerializeField]
private List<BuoyNodeData> buoyNodes = new List<BuoyNodeData>()
public ColorDataSO ColorData => colorData;
public BoardData Board => board;
public PathData Path => path;
public IReadOnlyList<BuoyNodeData> BuoyNodes => buoyNodes;
```

#### BoardData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField, Min(1)]
private int width = 10
[SerializeField, Min(1)]
private int height = 10
[SerializeField, Min(0.01f)]
private float cellSize = 1f
public int Width => width;
public int Height => height;
public float CellSize => cellSize;
```

#### PathData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private bool isClosed
[SerializeField]
private List<Vector2Int> cells = new List<Vector2Int>()
public bool IsClosed => isClosed;
public IReadOnlyList<Vector2Int> Cells => cells;
```

#### BuoyNodeData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private Vector2Int gridPosition
[SerializeField]
private Vector2Int outletDirection
[SerializeField]
private List<BuoyColumnData> columns = new List<BuoyColumnData>
public Vector2Int GridPosition => gridPosition;
public Vector2Int OutletDirection => outletDirection;
public Vector2Int OutletCell => gridPosition + outletDirection;
public IReadOnlyList<BuoyColumnData> Columns => columns;
```

#### BuoyColumnData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
public const int DefaultBuoyCount = 5
[SerializeField]
private ColumnElementType columnType = ColumnElementType.NormalPeg
[SerializeField, Min(0)]
private int columnTypeCount
[SerializeField]
private List<ColumnElementData> elements = new List<ColumnElementData>()
[SerializeField]
private List<BuoyData> buoys = CreateDefaultBuoys()
public ColumnElementType ColumnType => columnType;
public int ColumnTypeCount => columnTypeCount;
public IReadOnlyList<ColumnElementData> Elements => elements;
public IReadOnlyList<BuoyData> Buoys => buoys;
public int BuoyCount => buoys.Count;
private static List<BuoyData> CreateDefaultBuoys()
```

#### BuoyData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private int colorCode
[SerializeField]
private BuoyElementType buoyType = BuoyElementType.NormalBouy
[SerializeField, Min(0)]
private int buoyTypeCount
[SerializeField]
private List<BuoyElementData> elements = new List<BuoyElementData>()
public int ColorCode => colorCode;
public BuoyElementType BuoyType => buoyType;
public int BuoyTypeCount => buoyTypeCount;
public IReadOnlyList<BuoyElementData> Elements => elements;
```

#### ColumnElementData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private string elementId = string.Empty
public string ElementId => elementId;
```

#### BuoyElementData

Source: [Assets/Scripts/LevelEditorTools/LevelDataSO.cs](../Assets/Scripts/LevelEditorTools/LevelDataSO.cs).

```csharp
[SerializeField]
private string elementId = string.Empty
public string ElementId => elementId;
```

#### ColorDataSO

Source: [Assets/Scripts/LevelEditorTools/ColorDataSO.cs](../Assets/Scripts/LevelEditorTools/ColorDataSO.cs).

```csharp
[SerializeField]
private List<ColorEntryData> colors = new List<ColorEntryData>()
public IReadOnlyList<ColorEntryData> Colors => colors;
public bool TryGetColor(int code, out Color color)
```

#### ColorEntryData

Source: [Assets/Scripts/LevelEditorTools/ColorDataSO.cs](../Assets/Scripts/LevelEditorTools/ColorDataSO.cs).

```csharp
[SerializeField]
private int code
[SerializeField]
private string displayName = "New Color"
[SerializeField]
private Color color = Color.white
public int Code => code;
public string DisplayName => displayName;
public Color Color => color;
```

#### ColumnElementType

Source: [Assets/Scripts/LevelEditorTools/LevelDataEnums.cs](../Assets/Scripts/LevelEditorTools/LevelDataEnums.cs).

```csharp
NormalPeg = 0
Crate = 1
LockedPeg = 2
IcePeg = 3
ConveyorCounterBouyTxt = 4
```

#### BuoyElementType

Source: [Assets/Scripts/LevelEditorTools/LevelDataEnums.cs](../Assets/Scripts/LevelEditorTools/LevelDataEnums.cs).

```csharp
NormalBouy = 0
Hidden = 1
```

## QuickOutline

```mermaid
classDiagram
direction LR
class Outline {
    -HashSet~Mesh~ registeredMeshes$
    +Mode OutlineMode
    +Color OutlineColor
    +float OutlineWidth
    -Mode outlineMode
    -Color outlineColor
    -float outlineWidth
    -bool precomputeOutline
    -List~Mesh~ bakeKeys
    -List~ListVector3~ bakeValues
    -Renderer[] renderers
    -Material outlineMaskMaterial
    -Material outlineFillMaterial
    -bool needsUpdate
    -int _stencilCounter$
    -int _stencilID
    -Awake() void
    -OnEnable() void
    -OnValidate() void
    -Update() void
    -OnDisable() void
    -OnDestroy() void
    -Bake() void
    -LoadSmoothNormals() void
    -SmoothNormals(Mesh mesh) List~Vector3~
    -CombineSubmeshes(Mesh mesh, Material[] materials) void
    +UpdateMaterialProperties() void
}
MonoBehaviour <|-- Outline
class Mode {
    <<enumeration>>
    OutlineAll
    OutlineVisible
    OutlineHidden
    OutlineAndSilhouette
    SilhouetteOnly
}
class ListVector3 {
    +List~Vector3~ data
}
```

### C# declarations and attributes

#### Outline

Source: [Assets/QuickOutline/Scripts/Outline.cs](../Assets/QuickOutline/Scripts/Outline.cs).

```csharp
private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>()
public Mode OutlineMode
public Color OutlineColor
public float OutlineWidth
[SerializeField]
private Mode outlineMode
[SerializeField]
private Color outlineColor = Color.white
[SerializeField, Range(0f, 10f)]
private float outlineWidth = 2f
[Header("Optional")] [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. " + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
private bool precomputeOutline
[SerializeField, HideInInspector]
private List<Mesh> bakeKeys = new List<Mesh>()
[SerializeField, HideInInspector]
private List<ListVector3> bakeValues = new List<ListVector3>()
private Renderer[] renderers
private Material outlineMaskMaterial
private Material outlineFillMaterial
private bool needsUpdate
private static int _stencilCounter = 1
private int _stencilID
void Awake()
void OnEnable()
void OnValidate()
void Update()
void OnDisable()
void OnDestroy()
void Bake()
void LoadSmoothNormals()
List<Vector3> SmoothNormals(Mesh mesh)
void CombineSubmeshes(Mesh mesh, Material[] materials)
public void UpdateMaterialProperties()
```

#### Mode

Source: [Assets/QuickOutline/Scripts/Outline.cs](../Assets/QuickOutline/Scripts/Outline.cs).

```csharp
OutlineAll
OutlineVisible
OutlineHidden
OutlineAndSilhouette
SilhouetteOnly
```

#### ListVector3

Source: [Assets/QuickOutline/Scripts/Outline.cs](../Assets/QuickOutline/Scripts/Outline.cs).

```csharp
public List<Vector3> data
```

## Current behavior notes


- All stacks are instantiated at initialization. AdvanceQueue removes empty stacks, sets positions directly and refreshes holder decorations based on the remaining count.
- Stack colliders currently serve click input; no trigger callback receives a group.
- ConveyorController checks receiving ports along the spline. BeginReceive sets Moving to false.
- TickReceives transfers buoys in sequence with one receiving buoy in flight per transfer.
- Transfers own flying buoys until arrival, then ownership moves to the destination stack/group.
- Clicked is a local BuoyStack event; completion uses Action callbacks. No Observer is used.
- Nested types use short names in the diagrams: Transfer, ReceiveTransfer, Flight, PathMoveSlot, GroupPosition, ReceiverPort, EnterRequest, ColorMaterialEntry, ListVector3 and Outline.Mode.


ConveyorBuoyGroup is now a MonoBehaviour containing both group state and transform-based visual operations. ConveyorController creates it with AddComponent and calls Initialize; no separate group visual component is required.

## Open on the web

Open [ClassDiagram-Web.html](ClassDiagram-Web.html) and choose the full or overview diagram to edit it in Mermaid Live. For importing into another Mermaid-compatible web tool, use [ClassDiagram.mmd](ClassDiagram.mmd) or [ClassDiagram-Overview.mmd](ClassDiagram-Overview.mmd).
