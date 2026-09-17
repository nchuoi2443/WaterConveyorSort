"""Generate Markdown, Mermaid sources and web links from the current C# declarations."""
from pathlib import Path
import base64
import html
import json
import re
import zlib

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / "Docs"


def parse_types(path):
    source = path.read_text(encoding="utf-8-sig")
    masked = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@?"(?:""|\\.|[^"\\])*"',
                    lambda match: " " * len(match.group()), source)
    records = []
    for match in re.finditer(r'\b(class|interface|enum)\s+(\w+)([^{}]*)\{', masked):
        kind, name, bases = match.groups()
        start = match.end()
        end, depth = start, 1
        while depth:
            depth += (masked[end] == "{") - (masked[end] == "}")
            end += 1
        members = []
        if kind == "enum":
            members = [(item.strip(), "", "enum") for item in source[start:end - 1].split(",") if item.strip()]
        else:
            position = start
            while position < end - 1:
                while position < end - 1 and masked[position].isspace():
                    position += 1
                if position >= end - 1:
                    break
                attribute_start = position
                while masked[position] == "[":
                    position = masked.index("]", position) + 1
                    while masked[position].isspace():
                        position += 1
                declaration_start = position
                parentheses = 0
                while position < end - 1:
                    character = masked[position]
                    parentheses += (character == "(") - (character == ")")
                    if not parentheses and (character in ";{" or masked[position:position + 2] == "=>"):
                        break
                    position += 1
                signature = " ".join(source[declaration_start:position].split())
                attributes = " ".join(source[attribute_start:declaration_start].split())
                delimiter = masked[position:position + 2]
                method = "(" in signature and "=" not in signature.split("(")[0]
                member_kind = "method" if method else "property" if delimiter.startswith("{") or delimiter == "=>" else "field"
                suffix = ""
                if delimiter == "=>":
                    expression_end = masked.index(";", position) + 1
                    if not method:
                        suffix = " " + " ".join(source[position:expression_end].split())
                    position = expression_end
                elif delimiter.startswith("{"):
                    body_start = position
                    position += 1
                    nested = 1
                    while nested:
                        nested += (masked[position] == "{") - (masked[position] == "}")
                        position += 1
                    if member_kind == "property":
                        body = source[body_start:position]
                        suffix = " { get;" + (" set;" if re.search(r'\bset\b', body) else "") + " }"
                else:
                    position += 1
                    suffix = ";"
                if signature and not re.search(r'\b(class|enum|interface)\b', signature):
                    members.append((signature, attributes, member_kind, suffix))
        records.append({"name": name, "kind": kind, "bases": bases.strip(), "members": members,
                        "path": path.relative_to(ROOT).as_posix()})
    return records


def diagram(records, relations=()):
    lines = ["classDiagram", "direction LR"]
    for record in records:
        lines.append("class " + record["name"] + " {")
        if record["kind"] != "class":
            lines.append("    <<" + ("enumeration" if record["kind"] == "enum" else "interface") + ">>")
        for member in record["members"]:
            signature, attributes, kind = member[:3]
            if kind == "enum":
                lines.append("    " + signature)
                continue
            visibility = "+" if signature.startswith("public ") or record["kind"] == "interface" else "~" if signature.startswith("internal ") else "#" if signature.startswith("protected ") else "-"
            clean = re.sub(r'^(?:(?:public|private|internal|protected|readonly|static|const|event)\s+)+', "", signature)
            if kind == "method":
                prefix, parameters = clean.split("(", 1)
                tokens = prefix.split()
                clean = tokens[-1] + "(" + parameters + (" " + " ".join(tokens[:-1]) if len(tokens) > 1 else "")
            else:
                clean = clean.split(" = ")[0]
            clean = re.sub(r'<([^<>]*)>', lambda match: "~" + match.group(1).replace(", ", "_") + "~", clean)
            clean = clean.replace("<", "~").replace(">", "~")
            lines.append("    " + visibility + clean + ("$" if re.search(r'\b(static|const)\b', signature) else ""))
        lines.append("}")
        if record["bases"].startswith(":"):
            for base in record["bases"][1:].split(","):
                lines.append(base.strip() + (" <|.. " if base.strip() == "IInputReceiver" else " <|-- ") + record["name"])
    lines.extend(relations)
    return "\n".join(dict.fromkeys(lines)) + "\n" if not records else "\n".join(lines) + "\n"


def live_link(code):
    state = {"code": code, "mermaid": {"theme": "default"}, "autoSync": False, "updateDiagram": True}
    token = base64.urlsafe_b64encode(zlib.compress(json.dumps(state).encode())).decode().rstrip("=")
    return "https://mermaid.live/edit#pako:" + token


def main():
    sources = sorted((ROOT / "Assets/Scripts/GameCore").rglob("*.cs"))
    sources += [ROOT / "Assets/Scripts/LevelEditorTools" / name for name in ("LevelDataSO.cs", "ColorDataSO.cs", "LevelDataEnums.cs")]
    sources.append(ROOT / "Assets/QuickOutline/Scripts/Outline.cs")
    records = [record for path in sources for record in parse_types(path)]
    relations = """LevelManager --> LevelDataSO : reads
LevelManager --> BoardManager : initializes
LevelManager --> ConveyorController : configures
BoardManager *-- BuoyStackHolderController
BoardManager *-- BuoyTransferController : ticks flights
BoardManager --> ConveyorController
BoardManager *-- StackQueueController
BoardManager --> StackQueueVisual
BoardManager --> StackQueueExit
LevelManager --> StackQueueVisual
LevelManager --> StackQueueExit
LevelManager --> BoardManager : queue failure to Lost
StackQueueController *-- BuoyStack : queue contents
StackQueueController *-- StackReservation
StackQueueController --> StackQueueVisual
StackQueueController --> ConveyorController : detach at exit
StackQueueController --> BuoyTransferController : reserved landings
StackQueueExit --> StackQueueController : trigger entry
BuoyStackHolderController *-- BuoyStackHolder
BuoyStackHolderController ..> BuoyStack : lazy factory
BuoyStackHolder *-- BuoyStack : visible slots
BuoyStackHolder o-- BuoyColumnData : pending queue
BuoyStackHolder --> BuoyStackHolderVisual
BuoyStackHolderVisual --> BuoyStackHolder : trigger detection
BuoyStackHolderVisual ..> ConveyorBuoyGroup : collider lookup
BuoyStackHolder --> BuoyTransferController
BuoyStack *-- Buoy
BuoyStack --> BuoyStackVisual
Buoy --> BuoyVisual
BuoyVisual --> BuoyColorConfig
BuoyStackVisual ..|> IInputReceiver
BuoyStackVisual --> InputSystem
InputSystem --> IInputReceiver : dispatches clicks
ConveyorController *-- ConveyorBuilder
ConveyorController *-- ConveyorBuoyGroup
MonoBehaviour <|-- ConveyorBuoyGroup
ConveyorBuoyGroup o-- Buoy : moving contents
ConveyorBuoyGroup --> BuoyStackHolder : departure exclusion
BuoyTransferController --> BuoyStack : landings
BuoyTransferController --> ConveyorBuoyGroup : launch and claim
BuoyTransferController --> ConveyorController : admission and removal
ConveyorBuilder ..> BoardCoordinates
ConveyorController ..> BoardCoordinates
BuoyStackHolderController ..> BoardCoordinates
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
ReceiveTransfer *-- ReceiveFlight
ConveyorController *-- PathMoveSlot
ConveyorController *-- GroupPosition
ConveyorController *-- EnterRequest
Outline *-- ListVector3""".splitlines()
    sections = [
        ("Board", "Level, board and input", {"LevelManager", "BoardManager", "BoardCoordinates", "InputSystem", "IInputReceiver"}),
        ("Buoys", "Buoys, stacks and holders", {"Buoy", "BuoyStack", "BuoyStackHolder", "BuoyStackHolderController", "BuoyStackHolderVisual", "BuoyStackVisual", "BuoyVisual", "BuoyColorConfig", "ColorMaterialEntry"}),
        ("Conveyor", "Conveyor", {"ConveyorController", "ConveyorBuilder", "ConveyorBuoyGroup", "PathMoveSlot", "GroupPosition", "EnterRequest"}),
        ("StackQueue", "Stack queue and exit", {"StackQueueController", "StackQueueVisual", "StackQueueExit", "StackReservation"}),
        ("Transfer", "Transfers", {"BuoyTransferController", "Transfer", "ReceiveTransfer", "ReceiveFlight", "Flight"}),
        ("Data", "Level data and enums", {"LevelDataSO", "BoardData", "PathData", "BuoyNodeData", "BuoyColumnData", "BuoyData", "ColumnElementData", "BuoyElementData", "ColorDataSO", "ColorEntryData", "ColumnElementType", "BuoyElementType"}),
        ("Outline", "QuickOutline", {"Outline", "ListVector3", "Mode"}),
    ]
    overview = diagram([], relations)
    full = diagram(records, relations)
    exports = [("Overview", "Overview relationships", overview), ("", "Full diagram (large)", full)]
    markdown = ["# Class diagram - current implementation", "\nScope: GameCore, related level data and QuickOutline. Generated from the C# source declarations. Unity and Dreamteck internals are external.",
                "\n## Legend", "\n`+` public, `-` private, `~` internal, `#` protected, `$` static/const. Properties have no parentheses. Attribute and accessor summaries appear in the C# listings. Multi-argument Mermaid generics use `_`; the C# listings retain exact generic types. Class bodies and method bodies are omitted. Nested types use short names.",
                "\n## Overview relationships", "\n```mermaid\n" + overview + "```"]
    for key, title, names in sections:
        subset = [record for record in records if record["name"] in names]
        local_relations = [line for line in relations if line.split()[0] in names and line.split()[2] in names]
        code = diagram(subset, local_relations)
        exports.append((key, title, code))
        markdown.extend(["\n## " + title, "\n```mermaid\n" + code + "```"])
        for record in subset:
            markdown.extend(["\n### " + record["name"], "\nSource: [" + record["path"] + "](../" + record["path"] + ")."])
            signatures = []
            for member in record["members"]:
                signature, attributes, kind = member[:3]
                if attributes:
                    signatures.append(attributes)
                signatures.append(signature + (member[3] if len(member) > 3 else ""))
            markdown.append("\n```csharp\n" + "\n".join(signatures) + "\n```")
    markdown.extend(["\n## Current behavior", "\n- Holder VisibleCapacity and decorations are fixed at initialization. Only visible stacks are spawned; pending columns remain data.\n- The empty front stack is disabled after its outgoing transfer completes. The rear stack tweens forward while the replacement appears in the rear slot. Only the front accepts input and groups.\n- The holder root trigger detects a group using Enter/Stay. Group detection uses a trigger sphere and kinematic Rigidbody.\n- Matching loaded groups are claimed with IsReceiving and continue moving while overlapping flights launch to reserved stack indices. Landings commit in order. Serialized fields on LevelManager (holder receiving) and StackQueueVisual (queue receiving) control flight duration and launch delay; each transfer snapshots its timing settings.\n- DepartureHolder prevents immediate return to the source until its trigger volume has been left.\n- StackQueue initializes LevelDataSO.MaxStackInStackQueue empty stacks, centered on its spawn root along local X. It chooses the first empty or same-color stack without a buoy limit.\n- Queue reservations claim colors and landing indices immediately; overlapping same-color transfers commit in global stack order.\n- Open conveyors notify ReachedEnd once per loaded group. StackQueueExit provides a configurable trigger for open or closed paths. Accepted queue groups release path occupancy immediately and launch from the exit.\n- No eligible queue stack triggers LevelManager.HasLost/Lost once and pauses input, conveyor and transfers. Consume is not implemented.\n- Reset cancels queue tweens and clears flying buoys before destroying stacks/groups.",
                     "\n## Web viewing", "\nOpen [ClassDiagram-Web.html](ClassDiagram-Web.html). Choose a subsystem diagram to reduce layout cost. Import any `.mmd` file into Mermaid-compatible tools. Regenerate with `python Docs/Tools/GenerateClassDiagram.py`."])
    (DOCS / "ClassDiagram.md").write_text("\n".join(markdown) + "\n", encoding="utf-8")
    buttons = []
    for key, title, code in exports:
        filename = "ClassDiagram" + ("-" + key if key else "") + ".mmd"
        (DOCS / filename).write_text(code, encoding="utf-8")
        buttons.append('<a target="_blank" rel="noopener" href="' + html.escape(live_link(code), quote=True) + '">' + title + '</a>')
    page = '<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Class diagrams</title><style>body{font:17px system-ui;max-width:760px;margin:48px auto;padding:20px;background:#f5f7fb;color:#182333}a{display:block;padding:18px;margin:12px 0;background:white;border:1px solid #d4ddea;border-radius:10px;color:#1756a9;text-decoration:none}</style></head><body><h1>Water Conveyor Sort</h1><p>Choose a subsystem to view or edit in Mermaid Live. The full diagram has a higher layout cost. Auto sync is disabled; use the editor refresh control after changing code.</p>' + "\n".join(buttons) + '</body></html>'
    (DOCS / "ClassDiagram-Web.html").write_text(page, encoding="utf-8")
    assert len(records) == len({record["name"] for record in records})
    print("Generated", len(records), "types and", len(exports), "web diagrams.")


if __name__ == "__main__":
    main()
