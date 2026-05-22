# Character & Save System

Modèle Darktide : chaque joueur a **plusieurs personnages**, un par classe. Chaque perso a sa progression indépendante. La sauvegarde vit côté client.

---

## Modèle de données

```
PlayerProfile (1 par compte Steam, fichier JSON local)
├─ steamId
├─ List<CharacterSave> characters
│   ├─ CharacterSave "Mage"   → level=15, xp=2400, classId="mage"
│   ├─ CharacterSave "Warrior" → level=8,  xp=600,  classId="warrior"
│   └─ CharacterSave "Rogue"   → level=20, xp=4800, classId="rogue"
└─ selectedCharacterId (lequel est utilisé pour la prochaine partie)
```

---

## Classes à créer

### `Assets/Scripts/Save/CharacterSave.cs`
```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class CharacterSave
{
    public string id;            // GUID unique du perso
    public string displayName;   // "Mon Mage Cool"
    public string classId;       // référence à un ClassDefinition (SO)
    public int level = 1;
    public int xp = 0;
    public List<string> unlockedSpellIds = new();
    public List<InventoryItem> inventory = new();
    public DateTime createdAt;
    public DateTime lastPlayedAt;
}

[Serializable]
public class InventoryItem
{
    public string itemId;
    public int quantity = 1;
}
```

### `Assets/Scripts/Save/PlayerProfile.cs`
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    public ulong steamId;
    public List<CharacterSave> characters = new();
    public string selectedCharacterId;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "profile.json");

    public static PlayerProfile Load()
    {
        if (!File.Exists(FilePath))
            return new PlayerProfile();

        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(this, prettyPrint: true);
        File.WriteAllText(FilePath, json);
    }

    public CharacterSave GetSelected()
    {
        return characters.Find(c => c.id == selectedCharacterId);
    }

    public CharacterSave CreateCharacter(string classId, string displayName)
    {
        var ch = new CharacterSave
        {
            id = Guid.NewGuid().ToString(),
            displayName = displayName,
            classId = classId,
            createdAt = DateTime.UtcNow,
            lastPlayedAt = DateTime.UtcNow
        };
        characters.Add(ch);
        return ch;
    }
}
```

---

## Définition de classe (ScriptableObject)

Une classe = un archétype de gameplay (mage, warrior, rogue). Ses **stats de base** et **spells** sont des données, pas du code par perso → **ScriptableObject**.

### `Assets/Scripts/Save/ClassDefinition.cs`
```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Class Definition")]
public class ClassDefinition : ScriptableObject
{
    public string classId;       // doit matcher CharacterSave.classId
    public string displayName;
    public Sprite icon;

    [Header("Base stats")]
    public int baseHp = 100;
    public int baseDamage = 10;
    public float moveSpeed = 5f;

    [Header("Per-level growth")]
    public int hpPerLevel = 10;
    public int damagePerLevel = 2;

    [Header("Spells")]
    public SpellDefinition[] startingSpells;
}
```

Tu crées tes assets dans `Assets/Data/Classes/` (Create → Einerjahr → Class Definition).

### Registry pour résoudre `classId` → `ClassDefinition`
```csharp
[CreateAssetMenu(menuName = "Einerjahr/Class Registry")]
public class ClassRegistry : ScriptableObject
{
    public ClassDefinition[] classes;

    public ClassDefinition Get(string classId)
        => System.Array.Find(classes, c => c.classId == classId);
}
```

Tu drag tes classes dans le registry, et tu le ref dans le NetworkManager ou un singleton.

---

## Brancher la save au NetworkManager

Dans `EinerjahrNetworkManager.cs`, remplace les TODO :

```csharp
public override void OnClientConnect()
{
    base.OnClientConnect();

    var profile = PlayerProfile.Load();
    var selected = profile.GetSelected();
    string json = JsonUtility.ToJson(selected);

    NetworkClient.Send(new CharacterPayloadMessage { characterJson = json });
}

void OnReceiveCharacterPayload(NetworkConnectionToClient conn, CharacterPayloadMessage msg)
{
    var save = JsonUtility.FromJson<CharacterSave>(msg.characterJson);
    var classDef = classRegistry.Get(save.classId);

    GameObject player = Instantiate(playerPrefab);
    player.GetComponent<PlayerStats>().ApplyFromSave(save, classDef);
    NetworkServer.AddPlayerForConnection(conn, player);
}
```

---

## Sauvegarder les gains en fin de partie

Le host envoie les rewards à chaque client via TargetRpc (cf. doc 01 section "Gains fin de partie"). Côté client :

```csharp
[TargetRpc]
void TargetSendRewards(NetworkConnectionToClient target, int xpGained, string[] loot)
{
    var profile = PlayerProfile.Load();
    var current = profile.GetSelected();

    current.xp += xpGained;
    while (current.xp >= XpForNextLevel(current.level))
    {
        current.xp -= XpForNextLevel(current.level);
        current.level++;
    }

    foreach (string itemId in loot)
        current.inventory.Add(new InventoryItem { itemId = itemId });

    current.lastPlayedAt = System.DateTime.UtcNow;
    profile.Save();
}

int XpForNextLevel(int level) => 100 * level * level; // courbe quadratique
```

---

## Steam Cloud (plus tard)

Quand tu auras ton AppID réel, double-écris : local + Steam Cloud (cf. [02-steam-integration.md](02-steam-integration.md) section Steam Cloud).

```csharp
public void Save()
{
    string json = JsonUtility.ToJson(this);

    File.WriteAllText(FilePath, json);

    if (SteamManager.Initialized)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
        SteamRemoteStorage.FileWrite("profile.json", data, data.Length);
    }
}
```

Au chargement : essaie Steam Cloud d'abord (cross-machine), fallback local.

---

## Migration de save

Quand tu changes le schéma de `CharacterSave` (ajout de champs, etc.), JsonUtility est tolérant : nouveaux champs prennent leur valeur par défaut, champs supprimés sont ignorés. Mais si tu **renommes** ou **changes le type** d'un champ → casser.

Ajoute un `version` dans `PlayerProfile` et un migrateur :
```csharp
public int saveVersion = 1;

public static PlayerProfile Load()
{
    // ... lecture JSON
    Migrate(profile);
    return profile;
}

static void Migrate(PlayerProfile p)
{
    if (p.saveVersion < 2) { /* migration v1 → v2 */ p.saveVersion = 2; }
    if (p.saveVersion < 3) { /* migration v2 → v3 */ p.saveVersion = 3; }
}
```

---

## Gotchas

- `Application.persistentDataPath` est **par utilisateur Windows**, pas par compte Steam. Si 2 joueurs partagent un PC, leurs saves se mélangent → utilise `steamId` dans le nom du fichier : `profile_{steamId}.json`
- `JsonUtility` ne sérialise **pas** les Dictionary natifs. Utilise List ou écris des wrappers.
- `JsonUtility` ne sérialise pas les propriétés (`{ get; set; }`), uniquement les fields publics ou `[SerializeField]`.
- N'envoie pas la save complète via NetworkMessage → le host pourrait recevoir des données triché. Filtre côté server (validation max level, items légaux, etc.).
