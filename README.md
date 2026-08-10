# Cowboy Idle Frontier (PorQueSeriaUnVaquero)

Juego clicker idle / runner en pixel art para Android. El vaquero avanza solo por
un camino de 3 carriles, ataca enemigos automáticamente y el jugador compra mejoras,
prestigia con Maestría y completa misiones diarias.

## Cómo abrir el proyecto

1. Instalar **Unity 6000.3.10f1** (Unity 6.3) vía Unity Hub — usar exactamente esa
   versión para no ensuciar el repo con reimports.
2. Clonar el repo y abrir la carpeta raíz desde Unity Hub.
3. Escenas principales en `Assets/Scenes/`:
   - `MainMenu.unity` — menú inicial.
   - `GameScene.unity` — el juego (abrir esta para probar gameplay).

> **Firma de builds:** el archivo `.keystore` NO va al repo (está en `.gitignore`).
> Pedir el keystore por privado o usar Play App Signing.

## Estructura de carpetas

```
Assets/
  Scenes/            MainMenu y GameScene
  Scripts/
    GameSystem/      GameManager, GameSaveManager, Ads, audio, tutorial
    IdleSystem/      Oro (GoldManager), upgrades (UpgradeBase + UIs)
    Progression/     Maestría/Prestigio, misiones diarias, tienda
    EnemyScripts/    Enemigos runner, spawner, minijuego de jefe
    PlayerScripts/   Movimiento, disparo, habilidad del caballo
    Bonus/           Pickups de bonus y rewarded ads
  Resources/
    Upgrades/        ScriptableObjects de mejoras (01_CowboyHat, ...)
    Missions/        ScriptableObjects de misiones diarias
  GoogleMobileAds/   SDK de AdMob
  GooglePlayGames/   SDK de Play Games
```

## Sistema de guardado

- El save es un JSON en `Application.persistentDataPath/savegame.json`
  (en Windows: `%userprofile%\AppData\LocalLow\<company>\<product>\`).
- Los saves viejos en PlayerPrefs se **migran automáticamente** la primera vez.
- Qué se guarda: oro, niveles/bonus de upgrades, zona actual, cooldowns,
  puntos de maestría, misiones del día y boosts activos de la tienda.

## Sistemas de retención (nuevos)

| Sistema | Manager | UI | Estado |
|---|---|---|---|
| Maestría/Prestigio | `MasteryManager` | `MasteryPanel` + `MasteryProgressHUD` | Lógica lista; falta wiring de UI |
| Misiones diarias | `DailyMissionManager` | `DailyMissionsPanel` + `MissionEntryUI` | Lógica lista; falta wiring de UI |
| Tienda | `ShopManager` | `ShopPanel` | Lógica lista; falta wiring de UI |

Los tres managers se auto-instancian en runtime (`ProgressionBootstrap`), no hay que
agregarlos a las escenas. Los paneles de UI sí necesitan armarse en el editor:
**ver [Docs/INTEGRACION_UI.md](Docs/INTEGRACION_UI.md) con el paso a paso.**

## Convenciones

- Los assets de upgrades usan nombres semánticos con prefijo de orden:
  `01_CowboyHat.asset`, `02_LeatherBoots.asset`, etc. El campo `upgradeName`
  interno es lo que usa el save — no renombrarlo a la ligera.
- Ids de misiones (`missionId`) son estables: nunca cambiarlos una vez publicados,
  el save los referencia.
- No commitear: `Library/`, `Temp/`, builds, `*.keystore` (ya cubierto por `.gitignore`).
