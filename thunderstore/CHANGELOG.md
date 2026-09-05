# Changelog

## 1.1.0

- **Host mode.** The host can switch the lobby to "Host decides" and assign every player to a
  team (A to D). Team mates may access each other's packs, unassigned players are locked for
  everyone. Individual permission lists are ignored while host mode is active and come back as
  soon as the host switches to "Individual".
- Host-wide switches for "Allow everyone" and "Unlock while passed out".
- **Dropped packs.** New rules for packs lying on the ground: "Dropped pack" (put down by the
  wearer) and "After death". Individually: *My list* or *Everyone*. In host mode: *Team only* or
  *Everyone*. Defaults keep a pack you put down protected and leave death drops open, so a run
  can still be rescued.
- The host tracks who dropped which pack and why, and shares it with the lobby. Protected packs
  on the ground show "Locked", cannot be opened, stashed into or picked up by locked players.
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
