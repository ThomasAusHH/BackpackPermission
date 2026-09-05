# Testplan Multiplayer

Rollen: **A** = Träger (du, Mod installiert), **B** = zweiter Spieler mit Mod, **C** = Spieler ohne Mod
(optional, für die Grenzfälle). Wer Host ist, steht bei jedem Block dabei. Vor jedem Block
`Verbose = true` in `BepInEx/config/com.peakcode.backpackpermission.cfg` lassen, dann steht jede
Entscheidung im Log `BepInEx/LogOutput.log`.

Erwartete Log-Zeilen:

```
Access rule published: 1|0|1|u:<SteamID>,...
Denied <Name> taking an item from <Träger>'s backpack (host check).
Blocked opening <Träger>'s backpack (local check).
Blocked stashing into <Träger>'s backpack (local check).
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
| 2.2 | C legt ein Item in A's Rucksack. | Funktioniert wie Vanilla. Das ist die bekannte Grenze, siehe README. Nur notieren. |
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
| 4.1 | A schaltet „Allow everyone“ auf On. | Alle Zeilen zeigen „Allowed“ (gedämpft, wenn nicht einzeln freigegeben). B sieht „Open“. |
| 4.2 | A schaltet „Allow everyone“ wieder auf Off. | Einzelfreigaben bleiben erhalten, alle anderen wieder „Locked“. |

## 5. Ohnmacht und Tod

| # | Schritte | Erwartet |
|---|---|---|
| 5.1 | „Unlock while passed out“ = On. A wird ohnmächtig (z. B. Sturz), B ist gesperrt. | B sieht „Open“, kann Items nehmen. Sobald A wieder steht: „Locked“. |
| 5.2 | „Unlock while passed out“ = Off, A ohnmächtig. | B bleibt gesperrt. |
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

## Was zu notieren ist

Bei jedem Fehlschlag: Nummer des Tests, wer Host war, und die Zeilen aus `LogOutput.log` und
`%USERPROFILE%/AppData/LocalLow/LandCrab/PEAK/Player.log` rund um den Zeitpunkt.
