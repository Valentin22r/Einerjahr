# Project Structure

Organisation des dossiers, conventions de nommage, asmdef.

---

## Arborescence cible

```
Einerjahr/
├─ Assets/
│  ├─ Aura 2/                       (asset store, volumetric lighting)
│  ├─ Mirror/                       (asset store, networking)
│  │  └─ Transports/FizzySteamworks/
│  ├─ Plugins/
│  │  └─ Steamworks.NET/            (via .unitypackage)
│  ├─ Data/                         (ScriptableObjects)
│  │  ├─ Classes/                   (ClassDefinition.asset)
│  │  ├─ Spells/                    (SpellDefinition.asset)
│  │  ├─ Enemies/                   (EnemyDefinition.asset si tu en fais)
│  │  └─ Items/                     (ItemDefinition.asset)
│  ├─ Prefabs/
│  │  ├─ Player.prefab
│  │  ├─ Enemies/
│  │  ├─ Spells/                    (projectiles, VFX hit)
│  │  └─ UI/                        (cards, rows, popups)
│  ├─ Scenes/
│  │  ├─ Bootstrap.unity            (init Steam + load MainMenu)
│  │  ├─ MainMenu.unity             (sélection perso, host/join)
│  │  └─ Game.unity                 (la partie)
│  ├─ Scripts/
│  │  ├─ Networking/
│  │  │  ├─ EinerjahrNetworkManager.cs
│  │  │  ├─ SteamLobby.cs
│  │  │  └─ SteamLobbyTestHUD.cs
│  │  ├─ Player/
│  │  │  ├─ NetworkPlayer.cs
│  │  │  ├─ PlayerStats.cs
│  │  │  ├─ PlayerInput.cs
│  │  │  └─ PlayerMovement.cs
│  │  ├─ Save/
│  │  │  ├─ CharacterSave.cs
│  │  │  ├─ PlayerProfile.cs
│  │  │  ├─ ClassDefinition.cs
│  │  │  └─ ClassRegistry.cs
│  │  ├─ Spells/
│  │  │  ├─ SpellDefinition.cs
│  │  │  ├─ SpellRegistry.cs
│  │  │  ├─ SpellCaster.cs
│  │  │  └─ Projectile.cs
│  │  ├─ Enemies/
│  │  │  ├─ EnemyStats.cs
│  │  │  ├─ EnemyAI.cs
│  │  │  └─ EnemySpawner.cs
│  │  ├─ Combat/
│  │  │  ├─ IDamageable.cs
│  │  │  ├─ DamageInfo.cs
│  │  │  └─ DamageType.cs
│  │  ├─ UI/
│  │  │  ├─ HUD.cs
│  │  │  ├─ CharacterSelectUI.cs
│  │  │  ├─ LobbyUI.cs
│  │  │  ├─ DamageNumber.cs
│  │  │  └─ EndOfMissionUI.cs
│  │  ├─ Editor/                    (scripts éditeur uniquement)
│  │  │  └─ CopySteamAppId.cs
│  │  └─ Steamworks.NET/
│  │     └─ SteamManager.cs         (déplacé dans Plugins après install via .unitypackage)
├─ Packages/
│  └─ manifest.json
├─ ProjectSettings/
├─ doc/                             (cette doc)
├─ steam_appid.txt                  (= 480 en dev)
└─ Einerjahr.slnx
```

---

## Conventions de nommage

### Classes / fichiers
- **PascalCase** : `PlayerStats`, `SteamLobby`, `EnemyAI`
- Un fichier = une classe publique principale (Unity exige le match nom de fichier = nom de classe pour MonoBehaviour)

### Variables
- **camelCase** : `currentHp`, `attackCooldown`
- Constantes : `UPPER_SNAKE_CASE` : `const float MAX_RANGE = 30f;`
- Private fields exposés à l'inspector : `[SerializeField] int hp;` (pas de prefix `_` ou `m_`)

### Méthodes
- **PascalCase** : `TakeDamage`, `ApplyFromSave`
- Mirror : préfixe `Cmd...` pour Command, `Rpc...` pour ClientRpc, `Target...` pour TargetRpc

### Préfixes de log
Toujours un crochet avec un domaine court :
- `[EinerjahrNM]` — NetworkManager custom
- `[SteamLobby]` — lobby
- `[NetworkPlayer]` — player
- `[Spell]` — caster/projectile
- `[Enemy]` — IA
- `[Save]` — persistence

Permet le filtre rapide dans la Console Unity.

### Assets
- ScriptableObjects : `Mage.asset`, `Fireball.asset` (le nom du contenu, pas du type)
- Prefabs : `Player.prefab`, `Skeleton.prefab`
- Scenes : `MainMenu.unity`, `Game_Swamp.unity` (préfixe pour les missions)

---

## asmdef (Assembly Definition)

Crée un asmdef par dossier pour réduire les temps de compilation.

### `Einerjahr.Networking.asmdef`
```json
{
    "name": "Einerjahr.Networking",
    "rootNamespace": "",
    "references": [
        "Mirror",
        "Mirror.Components",
        "Mirror.Transports",
        "com.rlabrecque.steamworks.net",
        "Einerjahr.Save"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

À placer dans `Assets/Scripts/Networking/` (Unity reconnaît automatiquement).

### Ordre de création
Crée les asmdef du bas vers le haut (du moins dépendant au plus dépendant) :
1. `Einerjahr.Combat` (IDamageable, DamageInfo) — pas de deps
2. `Einerjahr.Save` (CharacterSave, PlayerProfile) — pas de deps
3. `Einerjahr.Spells` → ref Combat, Save
4. `Einerjahr.Enemies` → ref Combat, Spells
5. `Einerjahr.Player` → ref Combat, Spells, Save
6. `Einerjahr.Networking` → ref Player, Save, Mirror, Steamworks.NET
7. `Einerjahr.UI` → ref Player, Save (uniquement domain layer, pas Mirror direct)

### Trade-off
- ✅ Compilation incrémentale rapide (modif dans Player ne recompile pas UI)
- ❌ Plus de friction quand tu fais une nouvelle classe (faut configurer les refs)

Si t'es au début et le projet est petit (< 50 scripts), reste en `Assembly-CSharp` (= pas d'asmdef). Ajoute-les quand la compil > 3s.

---

## Namespaces

Optionnel mais recommandé quand tu commences à avoir > 30 scripts.

```csharp
namespace Einerjahr.Spells
{
    public class SpellCaster : NetworkBehaviour { /* ... */ }
}
```

Cohérence : namespace = chemin du dossier sous `Assets/Scripts/`.

---

## Git

### `.gitignore` recommandé
```
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
MemoryCaptures/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.apk
*.aab
*.unitypackage
*.app
sysinfo.txt
crashlytics-build.properties
```

### Asset Serialization
Project Settings → Editor → **Asset Serialization Mode = Force Text** (pour que les `.unity` et `.prefab` soient diffables en git).

### Git LFS pour les gros assets
Active git-lfs pour : `*.fbx`, `*.png` > 1Mo, `*.wav`, `*.mp3`, `*.psd`. Sinon ton repo grossit vite.

---

## Scènes — boot flow

Pattern recommandé :
1. **Bootstrap** : scène super légère, ne contient que ce qui doit persister (`SteamManager`, gestionnaire de scène). Charge ensuite MainMenu en additif ou single.
2. **MainMenu** : pas de NetworkManager actif. Choix du perso, host/join via Steam Lobby.
3. **Game** : NetworkManager actif. Si on est host, on a déjà lancé `StartHost()` depuis le MainMenu juste avant le LoadScene.

`SceneManager.LoadScene("Game")` côté server → Mirror gère automatiquement le changement de scène côté clients (via `NetworkManager.ServerChangeScene`).

---

## Données vs Code

Règle d'or : **tout ce qui peut varier d'un perso à l'autre, d'un sort à l'autre, d'un enemy à l'autre = ScriptableObject**.

Évite les `if (classId == "mage") damage = 25;` dans le code. Préfère `classDef.baseDamage`.

Bénéfice : tu changes les valeurs depuis l'éditeur sans recompiler. Et tu peux exposer des "data packs" pour les modders.
