# Testplan Multiplayer

Rollen: **A** = Träger (du, Mod installiert), **B** = zweiter Spieler mit Mod, **C** = Spieler ohne Mod
(optional, für die Grenzfälle). Wer Host ist, steht bei jedem Block dabei. Vor jedem Block
`Verbose = true` in `BepInEx/config/com.peakcode.backpackpermission.cfg` lassen, dann steht jede
Entscheidung im Log `BepInEx/LogOutput.log`.

Erwartete Log-Zeilen:

```
Access rule published: 1|0|1|u:<SteamID>,...
Lobby rule published: 1|1|<HostActor>|1|0|u:<SteamID>=1,...|1|0
Dropped pack <ViewId> registered: owner actor <n>, cause Manual|Death
Dropped packs published: <ViewId>:<Actor>:<0|1>,...
Denied <Name> taking an item from <Träger>'s backpack (host check).
Blocked opening <Träger>'s backpack (local check).
Blocked stashing into <Träger>'s backpack (local check).
Denied <Name> stashing into <Träger>'s backpack (host check), returning the item.
```

## 1. Grundschutz (A und B haben die Mod, beliebiger Host)

| # | Schritte | Erwartet |
|---|---|---|
| 1.1 | A trägt Rucksack mit mindestens einem Item. B schaut auf A's Rücken. | Prompt zeigt **„Locked“** statt „Open“, kein Haltebalken beim Halten von E. |
| 1.2 | B hält E mehrere Sekunden auf A's Rücken. | Rad öffnet sich nicht. Log bei B: `Blocked opening`. |
| 1.3 | A legt Rucksack ab, öffnet ihn, klickt bei gehaltener E-Taste die Zeile B, lässt E los, setzt ihn wieder auf. | Zeile B springt sofort auf „Allowed“, das Rad bleibt offen, bis E losgelassen wird. Log bei A: `Backpack access for B: allowed` und `Access rule published`. |
| 1.4 | B schaut erneut auf A's Rücken. | Prompt zeigt **„Open“**, Rad öffnet sich, Items sind sichtbar. |
| 1.5 | B nimmt ein Item aus A's Rucksack. | Item landet bei B im Inventar, verschwindet bei A aus dem Rucksack. Kein Fehler im Log. |
| 1.6 | B legt ein Item in A's Rucksack. | Item verschwindet bei B aus der Hand, erscheint in A's Rucksack (A prüft durch Ablegen und Öffnen). |
| 1.7 | A entzieht B die Freigabe wieder. | B sieht sofort „Locked“, ohne dass jemand neu joinen muss. |
| 1.8 | B hat A's Rad geöffnet, während A die Freigabe entzieht (A ist nah, B hält E gedrückt). | Rad bei B schließt sich von selbst. |

## 2. Host-Schutz gegen Spieler ohne Mod (A ist Host, C hat keine Mod)

| # | Schritte | Erwartet |
|---|---|---|
| 2.1 | C versucht ein Item aus A's Rucksack zu nehmen. | Bei C flackert das Item kurz, bleibt aber im Rucksack. Log bei A: `Denied C taking an item ... (host check)`. |
| 2.2 | C legt ein Item in A's Rucksack. | Bei C verschwindet das Item aus der Hand und liegt kurz darauf vor C's Füßen. A's Rucksack bleibt unverändert (A prüft durch Ablegen und Öffnen). Log bei A: `Denied C stashing into A's backpack (host check), returning the item.` |
| 2.4 | C legt ein Item in A's **abgelegten**, geschützten Rucksack (Host-Modus, anderes Team, oder individuell „Dropped pack: My list“). | Gleiches Verhalten wie 2.2, Log: `Denied C stashing into a dropped pack`. |
| 2.3 | Gleicher Test, aber **C ist Host** und A ist Client. | C kann Items nehmen. Bekannte Grenze: der Host hat keine Mod. |

## 3. Eigener Rucksack und Panel

| # | Schritte | Erwartet |
|---|---|---|
| 3.1 | A legt Rucksack ab und öffnet ihn. | Panel rechts zeigt alle Mitspieler mit Namen, Zeile pro Spieler, Zusammenfassung „x of y players allowed“. |
| 3.2 | Zeile anvisieren, dann mehrfach klicken. | Zeile hellt auf, unten im Rad steht „Allow B“ bzw. „Lock B“. Jeder Klick schaltet um, Rad bleibt offen, kein Item in der Hand wird benutzt. |
| 3.3 | A setzt den Rucksack wieder auf, nimmt ihn ab, legt Items hinein und heraus. | Alles wie Vanilla, Rucksack verschwindet nicht, kein Fehler in `Player.log`. |
| 3.4 | B öffnet A's **abgelegten** Rucksack am Boden. | Erlaubt, wie Vanilla. B sieht dabei ebenfalls das Panel, das aber B's eigene Liste zeigt. |
| 3.5 | Spieler joint nach, während A's Rad geschlossen ist. A öffnet erneut. | Neuer Spieler erscheint in der Liste, standardmäßig „Locked“. |
| 3.6 | Spieler verlässt die Runde. | Zeile verschwindet beim nächsten Öffnen. |

## 4. Alle erlauben

| # | Schritte | Erwartet |
|---|---|---|
| 4.1 | A stellt „Worn pack“ auf „Everyone“. | Alle Zeilen zeigen „Allowed“ (gedämpft, wenn nicht einzeln freigegeben). B sieht „Open“. |
| 4.2 | A stellt „Worn pack“ zurück auf „My list“. | Einzelfreigaben bleiben erhalten, alle anderen wieder „Locked“. |

## 5. Ohnmacht und Tod

| # | Schritte | Erwartet |
|---|---|---|
| 5.1 | „While passed out“ = Everyone. A wird ohnmächtig (z. B. Sturz), B ist gesperrt. | B sieht „Open“, kann Items nehmen. Sobald A wieder steht: „Locked“. |
| 5.2 | „While passed out“ = My list, A ohnmächtig. | B bleibt gesperrt. |
| 5.3 | A stirbt. | Rucksack fällt zu Boden, B kann ihn wie Vanilla öffnen. |

## 6. Spätjoiner und Persistenz

| # | Schritte | Erwartet |
|---|---|---|
| 6.1 | A hat B freigegeben. Beide verlassen die Runde, neue Runde, A öffnet Panel. | B steht weiterhin auf „Allowed“ (Steam-ID gemerkt). |
| 6.2 | B joint in eine laufende Runde, in der A schon gesperrt hat. | B sieht sofort „Locked“, ohne dass A etwas tun muss. |
| 6.3 | `RememberAllowedPlayers = false`, neue Runde. | Alle wieder „Locked“. |

## 7. Andere Pack-Typen

| # | Schritte | Erwartet |
|---|---|---|
| 7.1 | A trägt Fannypack, B gesperrt. | Wie 1.1 bis 1.2. |
| 7.2 | A trägt Jetpack, B gesperrt, B versucht Treibstoff nachzufüllen. | Rad öffnet sich nicht, kein Nachfüllen. |
| 7.3 | A trägt Rocketpack, B gesperrt, B hält E auf dem Rücken. | Rakete wird **nicht** angezündet. Nach Freigabe: anzünden funktioniert. |

## 8. Vanilla-Verhalten ohne Regel

| # | Schritte | Erwartet |
|---|---|---|
| 8.1 | C (ohne Mod) trägt einen Rucksack, A und B schauen darauf. | „Open“, alles wie Vanilla. Die Mod darf hier nichts blockieren. |
| 8.2 | A spielt eine Runde komplett ohne Rucksack-Interaktion. | Keine Fehler in `Player.log`, keine `FieldAccessException`. |

## 9. Host-Modus (A ist Host, B hat die Mod, C optional ohne Mod)

| # | Schritte | Erwartet |
|---|---|---|
| 9.1 | A öffnet den eigenen Rucksack. | Oberste Zeile „Lobby mode: Individual“, darunter die gewohnte eigene Liste. |
| 9.2 | A klickt „Lobby mode“. | Zeile springt auf „Host decides“, Hinweis „Click a player to change their team“, Schalter und Teamliste mit allen Spielern inkl. „A (you)“. Log: `Lobby mode: HostControlled` und `Lobby rule published`. |
| 9.3 | A klickt B mehrfach. | Status wechselt No team → Team A → B → C → D → No team. Rad bleibt offen. |
| 9.4 | A und B in Team A, B schaut auf A's Rücken. | „Open“, B kann nehmen und hineinlegen. Ohne dass B etwas freigegeben hat. |
| 9.5 | B öffnet den eigenen Rucksack. | Panel ist nur Anzeige: „The host manages access in this lobby“, „Your team: Team A“, alle Spieler mit Team, keine Zeile reagiert auf Klick. |
| 9.6 | A setzt B auf „No team“. | B sieht sofort „Locked“ an A's Rücken. Falls B's Rad offen war, schließt es sich. |
| 9.7 | B hatte A vorher in seiner eigenen Liste erlaubt; Host-Modus aktiv, verschiedene Teams. | A sieht trotzdem „Locked“ an B's Rücken: eigene Listen sind im Host-Modus inaktiv. |
| 9.8 | A schaltet zurück auf „Individual“. | B's eigene Liste gilt wieder, A sieht „Open“ an B's Rücken (aus 9.7). |
| 9.9 | C (ohne Mod) und A in Team A, B in Team B; C versucht bei B zu nehmen. | Item flackert, bleibt drin. Log bei A: `Denied C ... (host check)`. C bei A: erlaubt. |
| 9.10 | Host stellt „Worn pack“ auf „Everyone“. | Alle Rucksäcke offen, unabhängig von Teams. |
| 9.11 | Neue Runde, gleiche Spieler, A hostet. | Teams stehen wieder wie zuvor (Steam-ID gemerkt). |
| 9.12 | A verlässt die Runde, B wird Host (ohne die Regel zu ändern). | Panel bei allen zeigt wieder den individuellen Modus; die alte Host-Regel gilt nicht mehr. |

## 10. Abgelegte Rucksäcke und Tod (A und B haben die Mod, A ist Host)

| # | Schritte | Erwartet |
|---|---|---|
| 10.1 | A hat B gesperrt, „Dropped pack: My list“. A legt den Rucksack ab, B schaut ihn an. | Prompt „Locked“ statt „Open“, Rad öffnet sich nicht. Log bei A: `Dropped pack <id> registered: ... cause Manual` und `Dropped packs published`. |
| 10.2 | B versucht den abgelegten Rucksack aufzusetzen (Wear-Segment ist nicht erreichbar, weil das Rad nicht aufgeht). C ohne Mod versucht es. | Bei C flackert der Rucksack kurz und bleibt liegen. Log bei A: `Denied C taking from a dropped pack (wearing it)`. |
| 10.3 | A setzt den Rucksack selbst wieder auf. | Funktioniert. Log bei A nach kurzer Zeit: `Dropped packs published: (none)`. |
| 10.4 | A schaltet „Dropped pack: Everyone“, legt ab. | B sieht „Open“, kann öffnen, nehmen, hineinlegen und aufsetzen. |
| 10.5 | „After death: Everyone“ (Standard). A stirbt, Rucksack fällt. | B kann ihn wie Vanilla öffnen. Log: `cause Death`. |
| 10.6 | „After death: My list“, B gesperrt. A stirbt. | B sieht „Locked“ am gefallenen Rucksack. Nach Freigabe von B (A muss dafür wiederbelebt sein oder vorher freigeben): „Open“. |
| 10.7 | A hostet, Host-Modus, A und B in verschiedenen Teams, „Dropped pack: Team only“. B legt ab. | A sieht „Locked“. Team-Kamerad von B sieht „Open“. |
| 10.8 | Host-Modus, „After death: Team only“, B stirbt. | Nur B's Team darf an den gefallenen Rucksack. |
| 10.9 | B (kein Host) legt ab, verlässt die Runde. | Der Rucksack ist für alle offen (Besitzer weg). |
| 10.10 | Rucksack aus einem Koffer, den noch niemand getragen hat. | Für alle offen, Panel zeigt beim Öffnen die eigene Liste. |
| 10.11 | B öffnet A's abgelegten, aber freigegebenen Rucksack. | Kein Panel bei B (fremder Rucksack), Rad funktioniert normal. |

## Was zu notieren ist

Bei jedem Fehlschlag: Nummer des Tests, wer Host war, und die Zeilen aus `LogOutput.log` und
`%USERPROFILE%/AppData/LocalLow/LandCrab/PEAK/Player.log` rund um den Zeitpunkt.
