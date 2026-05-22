# Troubleshooting — Known Issues

Bugs déjà rencontrés et fixés pendant le setup. Si tu retombes dessus après un reinstall / clean Library/, consulte cette page.

---

## 1. Steamworks.NET UPM cassé (version 2025.163.0)

### Symptôme
```
Library\PackageCache\com.rlabrecque.steamworks.net@...\Runtime\autogen\isteamapplist.cs(21,25):
error CS0117: 'NativeMethods' does not contain a definition for 'ISteamAppList_GetNumInstalledApps'
```

### Cause
La version `2025.163.0` distribuée via UPM (`git+https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net`) a une incohérence interne : `isteamapplist.cs` référence des méthodes natives absentes de `NativeMethods.cs`.

### Solution
**Ne pas utiliser UPM**. Installer via `.unitypackage` officiel :
1. Retirer la ligne `com.rlabrecque.steamworks.net` de `Packages/manifest.json`
2. Télécharger le dernier `.unitypackage` stable depuis https://github.com/rlabrecque/Steamworks.NET/releases
3. **Assets → Import Package → Custom Package** → tout cocher → Import
4. Le `.unitypackage` installe dans `Assets/Plugins/Steamworks.NET/` (incluant le `SteamManager.cs` qui manque dans UPM)

---

## 2. `SteamManager` introuvable

### Symptôme
```
error CS0103: The name 'SteamManager' does not exist in the current context
```

### Cause
`SteamManager.cs` est **volontairement** absent du package UPM (l'auteur veut que tu le customises). Il n'est inclus que dans le `.unitypackage`.

### Solution
Voir bug #1 ci-dessus — install via `.unitypackage`. Sinon, copier manuellement le fichier `SteamManager.cs` depuis https://github.com/rlabrecque/Steamworks.NET/blob/master/Plugins/Steamworks.NET/SteamManager.cs vers `Assets/Scripts/Steamworks.NET/`.

---

## 3. Mirror `NetworkInformationPreview` NullReferenceException sur Unity 6

### Symptôme
```
NullReferenceException: Object reference not set to an instance of an object
UnityEditor.EditorStyles.get_label ()
Mirror.NetworkInformationPreview+Styles..ctor ()
Mirror.NetworkInformationPreview..ctor ()
```

### Cause
Bug Mirror + Unity 6 : le field `Styles styles = new Styles();` (line 63 de `NetworkInformationPreview.cs`) construit l'objet **avant** que `EditorStyles` soit initialisé. Le ctor de `Styles` accède à `EditorStyles.label` → NullRef.

### Solution
Patch déjà appliqué sur ce projet — `Assets/Mirror/Editor/NetworkInformationPreview.cs` ligne 63 :
```csharp
// AVANT (cassé)
Styles styles = new Styles();

// APRÈS (corrigé, lazy-init)
Styles styles;
```
Le lazy-init `if (styles == null) styles = new Styles();` existe déjà ligne 98, donc supprimer l'eager init suffit.

⚠ Si tu fais un update de Mirror plus tard, ce fix sera écrasé → refait-le.

---

## 4. `Mirror.NetworkInformationPreview was not disposed properly`

### Symptôme
```
Mirror.NetworkInformationPreview was not disposed properly.
Make sure that base.Cleanup is called if overriding the Cleanup method.
```

### Cause
Warning inoffensif lié au même fichier `NetworkInformationPreview.cs` — Mirror ne respecte pas le pattern `ObjectPreview.Cleanup` proprement.

### Solution
Aucune — ignore-le. C'est juste un warning éditeur, n'affecte pas le runtime.

---

## 5. `The PlayerPrefab is empty on the NetworkManager`

### Symptôme
```
The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.
Mirror.NetworkManager:OnServerAddPlayerInternal
```

### Cause
Tu host avec succès mais le champ `Player Prefab` du NetworkManager est vide → Mirror ne sait pas quel objet spawn.

### Solution
1. Crée un prefab Player (cf. doc 04)
2. Sélectionne le GameObject `NetworkManager` dans la scène
3. Inspector → champ **Player Prefab** → drag-drop ton prefab

Si tu utilises `EinerjahrNetworkManager` avec pattern character-payload : **décoche aussi Auto Create Player** sinon double spawn.

---

## 6. SyncVar du local player remontent jamais le pseudo Steam

### Symptôme
Le log affiche `Player_0` au lieu du vrai pseudo, même si Steam est initialisé.

### Cause
Soit `SteamFriends.GetPersonaName()` retourne `""`, soit ma logique de fallback masque le retour.

### Diagnostic
Le code actuel de `NetworkPlayer.cs` log explicitement la valeur reçue de Steam :
```
[NetworkPlayer] Local Steam identity → id=... name='...' (len=?)
```
Si `len=0` → Steam te renvoie vide. Vérifie que ton compte Steam a bien un pseudo configuré.

### Solution
Si vraiment vide, fallback automatique sur `Unknown_<steamId>` (déjà implémenté).

---

## 7. Lobby Steam créé mais ami ne peut pas join

### Symptôme
Ton ami clique "Join Game" depuis Steam, rien ne se passe.

### Causes possibles
- `SteamLobby` n'est pas dans la scène au démarrage chez l'ami → pas de callback `GameLobbyJoinRequested`
- Lobby créé avec `ELobbyType.k_ELobbyTypePrivate` au lieu de `k_ELobbyTypeFriendsOnly`
- Pas le même AppID (un est sur 480, l'autre sur ton AppID réel)

### Solution
- Le `SteamLobby` doit être instancié dès le menu principal (avant même qu'on crée/rejoint un lobby), pour écouter les callbacks
- Vérifie `ELobbyType` dans `SteamLobby.HostLobby()`
- Vérifie que `steam_appid.txt` contient le même AppID des 2 côtés

---

## 8. `SteamAPI_Init() failed`

### Symptôme
```
[Steamworks.NET] SteamAPI_Init() failed.
```

### Causes
- Steam client pas lancé / pas loggué
- `steam_appid.txt` absent à la racine
- AppID dans le `.txt` invalide (pas de licence sur le compte)

### Solution
- Steam doit tourner + être loggué
- `steam_appid.txt` à la racine du projet (à côté de `Einerjahr.slnx`)
- Pour AppID 480 (Spacewar) : tout compte Steam a la licence gratuite, ça marche

---

## 9. Build standalone : Steam ne reconnaît pas le jeu

### Symptôme
Tu run le `.exe` du build, Steam ne montre pas "Playing Einerjahr".

### Cause
`steam_appid.txt` doit être à côté du `.exe` du build, pas seulement dans le projet Unity.

### Solution
Build Settings → coche **Copy PDB files** non, mais surtout copie manuellement `steam_appid.txt` dans le dossier du build (à côté de `Einerjahr.exe`).

Pour automatiser : utilise un post-build script :
```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class CopySteamAppId : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPostprocessBuild(BuildReport report)
    {
        string src = Path.Combine(Application.dataPath, "..", "steam_appid.txt");
        string dst = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "steam_appid.txt");
        if (File.Exists(src)) File.Copy(src, dst, true);
    }
}
#endif
```
Mets ce fichier dans `Assets/Editor/`.

---

## 10. Compilation lente après chaque petit changement

### Cause
Tout est dans `Assembly-CSharp` (assembly par défaut Unity) → un changement = tout recompile.

### Solution
Crée des asmdef par domaine :
- `Assets/Scripts/Networking/Einerjahr.Networking.asmdef`
- `Assets/Scripts/Player/Einerjahr.Player.asmdef`
- `Assets/Scripts/Spells/Einerjahr.Spells.asmdef`
- etc.

Cf. [11-project-structure.md](11-project-structure.md) pour les détails.

---

## 11. `Steam_appid.txt` ignoré au build

### Cause
Unity 6 a un build pipeline plus strict, et certains fichiers à la racine du projet ne sont pas auto-copiés dans le build.

### Solution
Voir bug #9 — utilise le post-build script.
