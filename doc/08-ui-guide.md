# UI Guide

Stack UI : **UGUI** (Unity legacy UI, déjà installé via `com.unity.ugui`). Pour de l'indie Darktide-like c'est largement suffisant. UI Toolkit serait plus moderne mais plus de friction.

---

## Canvas principal (HUD in-game)

Hiérarchie type :
```
Canvas (Screen Space - Overlay)
├─ HealthBar
│   ├─ Background (Image)
│   ├─ Fill (Image, Type=Filled, Method=Horizontal)
│   └─ Text "HP: 80/100"
├─ ManaBar
├─ XPBar
├─ SpellSlots
│   ├─ Slot1 (Image icon + cooldown overlay + key text)
│   ├─ Slot2
│   ├─ Slot3
│   └─ Slot4
├─ MinimapPanel
└─ Crosshair (image centered)
```

### Pattern HUD singleton

```csharp
public class HUD : MonoBehaviour
{
    public static HUD Instance;

    [SerializeField] Image hpFill;
    [SerializeField] TMP_Text hpText;
    [SerializeField] Image[] spellIcons;
    [SerializeField] Image[] spellCooldownOverlays;

    void Awake() => Instance = this;

    public void SetHp(int current, int max)
    {
        hpFill.fillAmount = (float)current / max;
        hpText.text = $"{current} / {max}";
    }

    public void SetSpellCooldown(int slot, float fraction)
    {
        spellCooldownOverlays[slot].fillAmount = fraction; // 1 = en CD, 0 = prêt
    }
}
```

`PlayerStats.OnHpChanged` (côté client local) appelle `HUD.Instance.SetHp(newHp, maxHp);`

---

## Menu principal + sélection de personnage

Scène séparée `MainMenu.unity`. Affiche :
- Liste des persos du `PlayerProfile`
- Bouton "+" pour créer un nouveau perso (popup choix de classe)
- Bouton "Host" → load la scène game + create lobby
- Bouton "Join" → ouvre l'overlay Steam pour rejoindre un ami

```csharp
public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] Transform characterListParent;
    [SerializeField] GameObject characterCardPrefab;
    [SerializeField] ClassRegistry classRegistry;

    PlayerProfile profile;

    void Start()
    {
        profile = PlayerProfile.Load();
        Refresh();
    }

    void Refresh()
    {
        foreach (Transform t in characterListParent) Destroy(t.gameObject);

        foreach (var ch in profile.characters)
        {
            var card = Instantiate(characterCardPrefab, characterListParent);
            var classDef = classRegistry.Get(ch.classId);
            card.GetComponent<CharacterCard>().Setup(ch, classDef, OnSelect);
        }
    }

    void OnSelect(CharacterSave ch)
    {
        profile.selectedCharacterId = ch.id;
        profile.Save();
        // → load scène de jeu
    }
}
```

---

## Lobby UI (qui est connecté, prêt à start)

Une fois le lobby créé, affiche :
- Liste des joueurs (pseudo Steam + classe + level)
- Bouton "Start Mission" (host only)
- Bouton "Leave"

```csharp
public class LobbyUI : NetworkBehaviour
{
    [SerializeField] Transform playerListParent;
    [SerializeField] GameObject playerRowPrefab;

    // Réagit aux SyncVar changes des Player objects
    void Update()
    {
        // simple : rebuild la liste chaque frame (pas perf-critical en lobby)
        foreach (Transform t in playerListParent) Destroy(t.gameObject);
        foreach (var ps in PlayerRegistry.Players.Values)
        {
            var row = Instantiate(playerRowPrefab, playerListParent);
            row.GetComponent<PlayerRow>().Setup(ps);
        }
    }
}
```

Mieux : utilise des events `OnPlayerJoined` / `OnPlayerLeft` pour ne rebuild que quand nécessaire.

---

## Damage numbers (world-space)

Texte flottant qui apparaît au point de hit et s'efface en montant.

```csharp
public class DamageNumber : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] float lifetime = 1f;
    [SerializeField] float floatSpeed = 1.5f;

    public void Setup(int amount, bool crit)
    {
        text.text = amount.ToString();
        text.color = crit ? Color.yellow : Color.white;
        text.fontSize *= crit ? 1.4f : 1f;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        // billboard vers la caméra
        transform.rotation = Camera.main.transform.rotation;

        // fade
        var c = text.color;
        c.a = 1f - (lifetime - GetComponent<TimeLeft>().Remaining) / lifetime;
        text.color = c;
    }
}
```

---

## Écran fin de mission (rewards)

Reçu via TargetRpc → affichage :
- XP gagné par perso
- Loot reçu (liste d'items avec icones)
- Bouton "Retourner au menu"

```csharp
public class EndOfMissionUI : MonoBehaviour
{
    [SerializeField] TMP_Text xpGainedText;
    [SerializeField] Transform lootListParent;
    [SerializeField] GameObject lootItemPrefab;

    public void Show(int xp, string[] lootIds)
    {
        gameObject.SetActive(true);
        xpGainedText.text = $"+{xp} XP";

        foreach (string id in lootIds)
        {
            var item = Instantiate(lootItemPrefab, lootListParent);
            item.GetComponent<LootItemUI>().Setup(id);
        }
    }
}
```

---

## Steam friend avatars (pour player rows)

```csharp
using Steamworks;
using UnityEngine;

public static class SteamAvatarLoader
{
    public static Texture2D GetAvatar(ulong steamId)
    {
        var id = new CSteamID(steamId);
        int handle = SteamFriends.GetMediumFriendAvatar(id);
        if (handle == -1) return null;

        SteamUtils.GetImageSize(handle, out uint w, out uint h);
        byte[] data = new byte[w * h * 4];
        if (!SteamUtils.GetImageRGBA(handle, data, data.Length)) return null;

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(data);
        tex.Apply();
        return FlipVertical(tex); // Steam renvoie l'image à l'envers
    }

    static Texture2D FlipVertical(Texture2D src) { /* ... */ return src; }
}
```

---

## Best practices

- **TextMeshPro** plutôt que UI Text → meilleur rendu, déjà inclus dans Unity 6
- **Anchors corrects** sur Canvas Scaler → "Scale With Screen Size" mode, reference 1920x1080
- **Évite les RaycastTarget** inutiles → coche-off sur tout ce qui ne réagit pas au clic (textes décoratifs, icones, etc.) — gros gain perf
- **Sépare les Canvas** pour ce qui se met à jour fréquemment (HP bar) vs statique → un Canvas se rebuild en entier quand un seul element change
- **Object pool** pour les damage numbers si tu en spawn > 10/sec

---

## Input UI (navigation au gamepad)

Si tu vises un release Steam Deck-friendly, supporte le pad. Avec le new Input System :
- L'`EventSystem` doit avoir un composant `InputSystemUIInputModule`
- Tes boutons ont automatiquement la navigation (Submit/Cancel/Move) mappée
- Set `EventSystem.firstSelectedGameObject` sur ton bouton principal pour que le pad ait quelque chose à highlight au démarrage

---

## Gotchas

- **NetworkBehaviour sur Canvas** : si tu mets le HUD comme NetworkBehaviour, ça va te causer des soucis. **Le HUD est purement client-side**, jamais networké.
- **TargetRpc avant que le client ait son HUD** : si tu envoies les rewards juste après spawn, le HUD peut ne pas être prêt. Mets un check `if (HUD.Instance == null) yield return null;`.
- **DontDestroyOnLoad sur le Canvas** : tentant pour éviter de recréer le HUD à chaque scène, mais ça duplique si tu charges 2 fois la même scène. Préfère un Canvas par scène.
