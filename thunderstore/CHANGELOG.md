# Changelog

## 1.1.0

- **Host mode.** The host can switch the lobby to "Host decides" and assign every player to a
  team (A to D). Team mates may access each other's packs, unassigned players are locked for
  everyone. Individual permission lists are ignored while host mode is active and come back as
  soon as the host switches to "Individual".
- **Four situations, one answer each.** The panel now shows *Worn pack*, *While passed out*,
  *Dropped pack* and *After death*, each either *My list* or *Everyone* (individual) or
  *Team only* or *Everyone* (host mode). The former On/Off switches are part of these rows.
  Defaults keep a worn or dropped pack protected and leave passed-out and death drops open, so a
  run can still be rescued.
- **Stash rollback.** Stashing into a locked pack is now undone by the host: the pack is
  re-synced and the item lands on the ground next to the stasher. Works against players without
  the mod. Previously only taking could be denied host-side.
- The host tracks who dropped which pack and why, and shares it with the lobby. Protected packs
  on the ground show "Locked", cannot be opened, stashed into or picked up by locked players.
- **Hotkey.** F7 (configurable) opens the panel without a backpack, so hosts without a pack can
  manage teams and everyone can edit their list at any time.
- Team assignments are remembered by Steam ID.
- Non-hosts see a read-only overview of the teams in their panel.
- The lobby rule expires automatically when the host who set it leaves, so a new host without
  the mod does not keep stale teams alive.

## 1.0.0

- Initial release.
- Per-player access control for backpack, fannypack, jetpack and rocketpack, default nobody.
- Backpack Access panel inside the backpack wheel, styled like the hotbar.
- Allow everyone and Unlock while passed out switches.
- Permissions remembered by Steam ID.
- Host-side denial of unauthorized pickups, client-side blocking of opening and stashing.
- English and German texts.
