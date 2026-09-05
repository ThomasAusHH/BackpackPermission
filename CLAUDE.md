# CLAUDE.md

Arbeitskontext für Claude Code in diesem Repository.

## Was das ist

BepInEx-Mod für **PEAK** (Unity 6, netstandard2.1, Harmony 2, Photon PUN 2). Der Träger eines
Rucksacks/Fannypacks/Jetpacks/Rocketpacks legt fest, welche Mitspieler von außen zugreifen
dürfen. Standard: niemand. Details zur Bedienung und zum Multiplayer-Verhalten stehen in
`README.md`.

## Aufbau

| Pfad | Inhalt |
|---|---|
| `src/Plugin.cs` | BepInEx-Einstieg. Erzeugt `ModConfig`, `LocalPermissions`, `RuleSync`, ruft `Harmony.PatchAll`, tickt `RuleSync` je Frame |
| `src/ModConfig.cs` | Typisierter Zugriff auf die Config-Datei, alle Schlüssel und Beschreibungen (englisch) |
| `src/Localization/Strings.cs` | Alle UI-Texte, EN Standard, DE optional, `Auto` folgt `LocalizedText.CURRENT_LANGUAGE` |
| `src/Permissions/PlayerKey.cs` | Stabile Spieler-Identität `u:<UserId>` (Steam-ID) mit Fallback `a:<ActorNumber>` |
| `src/Permissions/AccessRule.cs` | Unveränderliche Regel eines Trägers, Wire-Format `1|alle|ohnmacht|key,key`, `Grants(player)` |
| `src/Permissions/LocalPermissions.cs` | Freigabeliste des lokalen Spielers, Persistenz über `ModConfig`, Event `Changed` |
| `src/Permissions/RuleSync.cs` | Veröffentlicht die Regel als Photon-Custom-Property `bpk`, liest fremde Regeln (`TryReadRule`) |
| `src/Permissions/LobbyRule.cs` | Unveränderliche Host-Regel (`LobbyMode`, Teams 1..4, Host-ActorNumber), Wire-Format `1|mode|hostActor|ohnmacht|alle|key=team,...` |
| `src/Permissions/HostSettings.cs` | Host-Einstellungen des lokalen Spielers (Modus, Teams, Schalter), Persistenz über `ModConfig` Sektion `Host`, Event `Changed` |
| `src/Permissions/LobbySync.cs` | Veröffentlicht die Host-Regel als **Raum**-Property `bpk_lobby` (nur als Master), liest sie überall; Regel gilt nur, solange ihr Host noch Master ist |
| `src/Permissions/DroppedPackRegistry.cs` | Host-autoritative Zuordnung Bodenrucksack (ViewID) → letzter Träger (ActorNumber) + `DropCause`; Raum-Property `bpk_drops`; Host liest lokal (Property hinkt einen Roundtrip), andere die Property |
| `src/Permissions/AccessPolicy.cs` | `Evaluate(wearer, requester)` liefert `AccessVerdict` mit Begründung. Reihenfolge: Self → Dead → Host-Regel (wenn HostControlled, Trägerregel ignoriert) → Trägerregel → ohne Regel gewährt |
| `src/Patches/BackpackOnBackVisualsPatches.cs` | Prompt „Locked“, kein Haltebalken, kein `Interact_CastFinished` |
| `src/Patches/BackpackWheelPatches.cs` | Panel ein-/ausblenden, Rad schließen bei Entzug; `GuiManagerPatches` für `CloseBackpackWheel` |
| `src/Patches/ItemPickupPatches.cs` | Host-seitig `Item.RequestPickup` ablehnen (`DenyPickupRPC`): Items im getragenen Pack, Items im Bodenpack, Aufsetzen eines geschützten Bodenpacks |
| `src/Patches/DropTrackingPatches.cs` | Master: `CharacterItems.DropItemRpc` (manuell) und `DropItemFromSlotRPC` (Tod/Wiederbelebung) mit Slot 3 → Registry; `SpawnTracker` merkt sich das zuletzt per `PhotonNetwork.InstantiateItemRoom` erzeugte Objekt |
| `src/Patches/BackpackItemPatches.cs` | Bodenrucksack (`Backpack`): Prompt „Locked“, `Interact` und `Stash` client-seitig blocken |
| `src/Patches/BackpackStashPatches.cs` | Client-seitig `StashInBackpack` blocken |
| `src/Patches/StashRollbackPatches.cs` | Master: `RPCAddItemToCharacterBackpack` / `Backpack.RPCAddItemToBackpack` bei Verbot überspringen, Ziel-Pack an Others re-syncen (`SyncInventoryRPC` bzw. `SetItemInstanceDataRPC`), Item vor dem Ablegenden neu spawnen |
| `src/UI/PermissionPanel.cs` | MonoBehaviour unter `BackpackWheel.transform`, drei Ansichten: eigene Liste, Host-Team-Editor, Team-Übersicht (read-only); `Instance`, `HoveredRow`, `ShowFor`/`HideIfOpen` |
| `src/UI/StandalonePanel.cs` | Hotkey-Panel ohne Rad (Config `PanelHotkey`, Unity-InputSystem `Keyboard.current`), hängt unter der HUD-Ebene (`backpackWheel.transform.parent`); `GUIManager.wheelActive`-Getter wird auf true gepatcht, damit Cursor frei und Eingaben gesperrt sind |
| `src/UI/PermissionRow.cs` | Klickbare Zeile (Pointer-Events, Highlight, Caption im Radtext). Klick ist sicher: bei offenem Rad ist `CanDoInput()` false und `CharacterInput` liest keine Item-Eingaben |
| `src/UI/HudStyle.cs` | Vanilla-Look: Radtext-Schrift/-Farbe, Kontur-Sprite `UI_Blur_Outlne` neu gesliced und auf Zeilenhöhe skaliert |
| `src/UI/UiHierarchyDump.cs` | Debug-Dump von Rad und Hotbar ins Log (`Verbose=true`) |
| `libs/` | Spiel-, Unity-, Photon- und BepInEx-DLLs, `Assembly-CSharp-publicized.dll`. **Nicht im Git** (Copyright), Aufbau steht in `README.md` unter „Building“ |
| `.tools/` | `ilspycmd.exe`, `assembly-publicizer.exe`; `.tools/out/full/` ist das vollständige Dekompilat der publizierten `Assembly-CSharp` und die einzige Doku zum Spiel. **Nicht im Git** |
| `thunderstore/` | Thunderstore-Paketquellen: `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png` |
| `release/` | **Nicht im Git.** `release.sh` schreibt hier das Thunderstore-Zip hin; lokal liegen dort auch `RELEASE_NOTES.md` (Upload-Checkliste, Kategorien) und `nexus_description.bbcode` |
| `docs/images/` | Screenshots für README und Store-Seiten, Dateinamen stehen in `docs/images/README.md` |
| `TESTPLAN.md` | Multiplayer-Testfälle |

Git-Repository `ThomasAusHH/BackpackPermission` auf GitHub, keine Solution, keine automatischen
Tests. Die csproj referenziert `..\libs\*.dll` per Wildcard mit `Private=false`; kein NuGet nötig.

## Bauen und Deployen

```bash
./deploy.sh
```

Baut Release und kopiert nach
`C:/Program Files (x86)/Steam/steamapps/common/PEAK/BepInEx/plugins/BackpackPermission/`.
Nur bauen:

```bash
dotnet build src/BackpackPermission.csproj -c Release -v minimal
```

Immer bauen **und** deployen. Die DLL darf nur einmal unter `plugins/` liegen (Plugin-GUID
`com.peakcode.backpackpermission`).

Release: Version in `src/Plugin.cs` (`Version`), `src/BackpackPermission.csproj` und
`thunderstore/manifest.json` gleichziehen, `thunderstore/CHANGELOG.md` ergänzen, dann
`./release.sh`. Upload-Schritte in `release/RELEASE_NOTES.md`.

## Spielmechanik, auf die die Mod aufbaut

- Fremder Zugriff auf einen getragenen Rucksack läuft ausschließlich über
  `BackpackOnBackVisuals.Interact_CastFinished` → `GUIManager.OpenBackpackWheel(GetFromEquippedBackpack)`.
- Entnehmen: `BackpackWheel.Choose` → `Item.Interact` → RPC `RequestPickup` an den **Master-Client**,
  der `Player.AddItem` prüft und mit `OnPickupAccepted` oder `DenyPickupRPC` antwortet. Deshalb
  kann der Host unerlaubte Entnahmen sicher ablehnen.
- Hineinlegen: `CharacterBackpackHandler.StashInBackpack` sendet `RPCAddItemToCharacterBackpack`
  an **alle** und leert den eigenen Slot lokal (Nicht-Master über `RPCRemoveItemFromSlot` an den Master,
  der per `SyncInventoryRPC` verteilt; der Master ist Inventar-Autorität). Der Master kann den Stash
  deshalb rückgängig machen: RPC lokal überspringen, Ziel-Pack an Others syncen, Item neu spawnen.
  Beim Eintreffen des Stash-RPC hat der Master das Item noch im Slot des Ablegenden.
- Der eigene Rucksack wird geöffnet, indem man ihn ablegt (Bodenitem, `BackpackReference.Item`).
  Deshalb erscheint das Panel bei jedem Rad mit Typ `Item` (oder `IsOnMyBack()`).
- Beim Tod fällt der Rucksack zu Boden (`DropAllItems(includeBackpack: true)`), bei Ohnmacht
  bleibt er am Rücken. Daher gibt es die Option „Freigabe bei Ohnmacht“.
- Beide Drop-Wege (`DropItemRpc` manuell, `DropItemFromSlotRPC` bei Tod/Wiederbelebung) laufen auf allen Clients,
  aber nur der Master erzeugt das Bodenitem (`PhotonNetwork.InstantiateItemRoom`, definiert in der PUN-DLL des Spiels).
  Deshalb kann nur der Host Bodenrucksack und Besitzer zusammenbringen.
- Spieleridentität: `Photon.Realtime.Player.UserId` (Steam-ID), Fallback `ActorNumber`.
- Raum-Properties (`PhotonNetwork.CurrentRoom.SetCustomProperties`) überleben einen Master-Wechsel. Deshalb trägt
  die Host-Regel die ActorNumber ihres Hosts und wird ignoriert, sobald ein anderer Spieler Master ist.

## Stolperfalle: publizierte Assembly

`libs/Assembly-CSharp-publicized.dll` macht alles public, das Spiel selbst nicht. Mono prüft
Feldzugriffe zur Laufzeit: Ein Patch, der ein im Original privates/protected **Feld** liest,
wirft `FieldAccessException` und lässt die gepatchte Methode komplett scheitern (so verschwand
der Rucksack beim Aufsetzen über `Item.view`). Vor dem Einsatz eines Members dessen
Sichtbarkeit in der Original-DLL prüfen:

```bash
./.tools/ilspycmd.exe "C:/Program Files (x86)/Steam/steamapps/common/PEAK/PEAK_Data/Managed/Assembly-CSharp.dll" -t Item -r "C:/Program Files (x86)/Steam/steamapps/common/PEAK/PEAK_Data/Managed" | grep -n " view;"
```

Bekannt nicht-public: `Item.view` (protected, stattdessen `photonView`),
`CharacterBackpackHandler.character` und `CharacterBackpackHandler.photonView` (private),
`CharacterItems.photonView` (`private new`, verdeckt die öffentliche Basis-Eigenschaft von
`MonoBehaviourPun`; stattdessen `GetComponent<PhotonView>()`). Vorsicht bei `photonView` generell:
Prüfen, ob die Klasse ein eigenes Feld dieses Namens deklariert. `Character`, `Player`, `Item`
haben nur ein eigenes `view`, dort ist `photonView` die öffentliche Basis-Eigenschaft.
Unity-Exceptions landen nicht in `BepInEx/LogOutput.log` („Unable to start Unity log writer“),
sondern in `%USERPROFILE%/AppData/LocalLow/LandCrab/PEAK/Player.log`.

## Konventionen

- Antworten an den Nutzer auf Deutsch. Der gesamte Quellcode ist englisch: Kommentare, Doku-Kommentare, Bezeichner, Config-Texte, Log-Zeilen, UI-Texte. Das Repo ist Open Source.
- Build mit `TreatWarningsAsErrors`; Warnungen beheben statt unterdrücken.
- Neue Patches als `[HarmonyPatch]`-Klassen im selben Assembly, `PatchAll` findet sie.
- Ohne Regel (Träger hat die Mod nicht) muss immer Vanilla-Verhalten gelten: `AccessPolicy.Evaluate`
  liefert `GrantedNoRule`. Diese Eigenschaft nicht brechen.
- Zustand lebt in Instanzen (`Plugin.Settings`, `Plugin.Permissions`), nicht in verstreuten statischen Feldern.
  Patches greifen nur über `AccessPolicy` und `PermissionPanel.Instance` zu.
