# BackpackPermission

**Your backpack, your rules.** Choose which players may open the backpack, fannypack,
jetpack or rocketpack on your back. Everyone else sees *Locked* and cannot take anything out,
put anything in, or light your rocketpack. **Default: nobody is allowed.**

![Backpack Access panel next to the backpack wheel](https://raw.githubusercontent.com/ThomasAusHH/BackpackPermission/main/docs/images/panel.png)

## Features

- **Per-player permissions** right inside the backpack wheel. Drop your pack, open it and click
  the player rows. Each click toggles between *Allowed* and *Locked* while the wheel stays open.
- **Native look.** The panel is built from the game's own hotbar style and font.
- **Allow everyone** switch for when you trust the whole lobby.
- **Unlock while passed out** (on by default) so teammates can still grab that heart or
  energy drink when you are down.
- **Remembers your friends.** Permissions are stored by Steam ID and apply again next session.
- **Host-side enforcement.** If the host runs the mod, unauthorized pickups are denied even for
  players who do not have the mod installed.
- **Vanilla behaviour for everyone else.** Packs of players without the mod stay open, dropped
  packs on the ground stay open, and a dead player's pack drops as usual.

## Multiplayer

The rule is shared as a Photon player property. Late joiners see it immediately. No custom
RPCs are sent.

| Who has the mod | Result |
|---|---|
| Only you, and you are the host | Taking items is blocked: as host you deny unauthorized pickups yourself. Stashing and lighting by players without the mod cannot be blocked. |
| Only you, someone else hosts | No protection. Nobody evaluates your rule. |
| You and the host | Taking items is blocked. Stashing and lighting by players without the mod cannot be blocked. |
| Everyone | Full protection. |

For the full experience every player in the lobby should install the mod.

![Locked prompt on a teammate's backpack](https://raw.githubusercontent.com/ThomasAusHH/BackpackPermission/main/docs/images/locked-prompt.png)

## Configuration

`BepInEx/config/com.peakcode.backpackpermission.cfg`

- `UnlockWhilePassedOut` (true), `RememberAllowedPlayers` (true), `Language` (English / Deutsch / Auto)
- `PanelOffsetX`, `PanelWidth`, `PanelScale` to move or resize the panel
- `Verbose` for detailed logging

## Known limitations

- The panel is mouse only. Gamepad wheel navigation does not reach the rows.

## Links

Source, issues and screenshots: https://github.com/ThomasAusHH/BackpackPermission
