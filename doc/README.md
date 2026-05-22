# Doc Einerjahr

Documentation technique du projet. Unity 6 (6000.3.2f1, URP) + Mirror + Steam P2P, modèle Darktide-like (host = un joueur, chacun amène son perso persistent).

## Index

| # | Fichier | Quand le lire |
|---|---|---|
| 01 | [Mirror cheatsheet](01-mirror-cheatsheet.md) | Avant n'importe quel script networké |
| 02 | [Steam integration](02-steam-integration.md) | Pour ajouter du social Steam (invites, friend list, achievements) ou pour le build de prod |
| 03 | [Character & save system](03-character-save.md) | Quand tu codes le multi-class + persistence |
| 04 | [Player systems](04-player-systems.md) | Stats (HP/level/XP), input, movement |
| 05 | [Spell system](05-spell-system.md) | Design des sorts, hitscan vs projectile |
| 06 | [Enemy AI](06-enemy-ai.md) | NavMesh, IA host-only, spawn waves |
| 07 | [Combat & damage](07-combat-damage.md) | Pipeline de dégâts, résistances, attribution |
| 08 | [UI guide](08-ui-guide.md) | HUD, menu sélection perso, lobby UI |
| 09 | [Debugging multi](09-debugging-multi.md) | Tester sans 2 machines, ParrelSync, logs |
| 10 | [Troubleshooting](10-troubleshooting.md) | Bugs connus qu'on a déjà rencontrés/fixés |
| 11 | [Project structure](11-project-structure.md) | Organisation des dossiers et asmdef |

## État du projet (snapshot à la création de cette doc)

### ✅ En place
- Steamworks.NET (.unitypackage) initialisé via `SteamManager`
- FizzySteamworks transport P2P
- Lobby Steam (création/join via invite) → `Assets/Scripts/Networking/SteamLobby.cs`
- HUD test → `Assets/Scripts/Networking/SteamLobbyTestHUD.cs`
- Custom NetworkManager avec pattern character-payload → `Assets/Scripts/Networking/EinerjahrNetworkManager.cs`
- Player prefab minimal avec sync identité Steam → `Assets/Scripts/Player/NetworkPlayer.cs`
- AppID dev : 480 (Spacewar) via `steam_appid.txt`

### 🚧 À construire (par toi)
- `CharacterSave` + `PlayerProfile` (multi-class, JSON local)
- `PlayerStats` networké (HP, level, XP)
- Mouvement joueur + caméra
- Système de spells (abstraction `Spell` + hitscan/projectile)
- Enemies avec NavMesh + IA host-only
- UI : menu sélection perso, HUD in-game, écran fin de partie
- Persistence des gains de fin de session

## Stack

| Couche | Choix | Pourquoi |
|---|---|---|
| Networking | Mirror | Mature, doc abondante, gratuit |
| Transport | FizzySteamworks | P2P via Steam, zéro infra serveur |
| Steam binding | Steamworks.NET (.unitypackage) | UPM cassé en HEAD (cf. doc 10) |
| Render pipeline | URP | Déjà installé, performant |
| Lighting | Aura 2 | Volumétriques (atmosphère type Darktide) |
| Persistence | JSON local + Steam Cloud (plus tard) | Simple, suffisant pour indie |

## Conventions

- Code en anglais (noms de classes, méthodes, variables)
- Logs avec préfixe entre crochets : `[EinerjahrNM]`, `[SteamLobby]`, etc.
- Commentaires en français OK
- `Assets/Scripts/<Domaine>/<Script>.cs` (Networking, Player, Spells, Enemies, UI, Save)
