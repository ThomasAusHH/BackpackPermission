# BackpackPermission

**Your backpack, your rules.** Choose which players may open the backpack, fannypack,
jetpack or rocketpack on your back. Everyone else sees *Locked* and cannot take anything out,
put anything in, or light your rocketpack. **Default: nobody is allowed.**

![Backpack Access panel next to the backpack wheel](https://raw.githubusercontent.com/ThomasAusHH/BackpackPermission/main/docs/images/panel.png)

## Features

- **Per-player permissions** right inside the backpack wheel. Drop your pack, open it and click
  the player rows. Each click toggles between *Allowed* and *Locked* while the wheel stays open.
- **Native look.** The panel is built from the game's own hotbar style and font.
- **Four situations, one answer each.** *Worn pack*, *While passed out*, *Dropped pack* and
  *After death* are each either *My list* or *Everyone*. By default your pack is yours, but
  friends can still grab that heart or energy drink when you are down or dead.
- **Remembers your friends.** Permissions are stored by Steam ID and apply again next session.
- **Host-side enforcement.** If the host runs the mod, unauthorized pickups are denied and
  unauthorized stashes are bounced back to the ground, even for players without the mod.
- **Host mode.** The host can switch the lobby to *Host decides*, put players into teams A to D
  and let team mates share packs. Individual lists pause while host mode is on and return when
  the host switches back. Protects every player in the lobby, even those without the mod.
- **Host mode rules.** In host mode the same four rows switch between *Team only* and
  *Everyone* for the whole lobby.
- **Vanilla behaviour for everyone else.** Packs of players without the mod stay open, dropped
  packs on the ground stay open, and a dead player's pack drops as usual.

## Multiplayer

The rule is shared as a Photon player property. Late joiners see it immediately. No custom
RPCs are sent.

| Who has the mod | Result |
|---|---|
| Only you, and you are the host | Taking and stashing are blocked: as host you deny pickups and bounce stashed items to the ground. Lighting a rocketpack by players without the mod cannot be blocked. |
| Only you, someone else hosts | No protection. Nobody evaluates your rule. |
| You and the host | Taking and stashing are blocked by the host. Lighting a rocketpack by players without the mod cannot be blocked. |
| Everyone | Full protection. |

For the full experience every player in the lobby should install the mod.

![Locked prompt on a teammate's backpack](https://raw.githubusercontent.com/ThomasAusHH/BackpackPermission/main/docs/images/locked-prompt.png)

## Configuration

`BepInEx/config/com.peakcode.backpackpermission.cfg`

- `UnlockWhilePassedOut` (true), `ProtectDroppedPack` (true), `ProtectPackAfterDeath` (false), `RememberAllowedPlayers` (true), `Language` (English / Deutsch / Auto)
- `[Host] LobbyMode` (Individual / HostControlled), host-wide `AllowEveryone`, `UnlockWhilePassedOut`, `DroppedPacksTeamOnly`, `DeathDropsTeamOnly`
- `PanelOffsetX`, `PanelWidth`, `PanelScale` to move or resize the panel
- `Verbose` for detailed logging

## Known limitations

- The panel is mouse only. Gamepad wheel navigation does not reach the rows.

## Links

Source, issues and screenshots: https://github.com/ThomasAusHH/BackpackPermission
