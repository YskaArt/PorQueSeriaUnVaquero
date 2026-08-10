# Integración de UI — Maestría, Misiones y Tienda

La lógica de los tres sistemas ya funciona sola (los managers se crean en runtime
vía `ProgressionBootstrap`). Lo único que falta es armar los paneles en
`GameScene.unity` y conectar los botones del HUD. Todo el wiring es arrastrar
referencias en el inspector; no hay que escribir código.

Los tres paneles siguen el mismo patrón que el menú de upgrades existente:
un GameObject panel desactivado + un botón del HUD que llama a `OpenPanel()`.

---

## 1. Maestría / Prestigio

**Cómo funciona:** el oro ganado en la run acumula progreso. Al prestigiar se
reinician upgrades/oro/zona y se ganan puntos de maestría (+2% de oro permanente
por punto, configurable en el `MasteryManager` si algún día se agrega a escena;
por defecto: primer punto a los 100K de oro ganado, el n-ésimo cuesta n² × 100K).

> **Importante para los 3 paneles:** el componente (`MasteryPanel`,
> `DailyMissionsPanel`, `ShopPanel`) va en un GameObject **siempre activo**
> (ej: el Canvas o un hijo "UI Controllers"), y `panelRoot` apunta al panel
> visual que se prende/apaga. Si el componente vive en el panel desactivado,
> los refrescos en segundo plano (badge de misiones, timer del boost) no corren.

**Panel (`MasteryPanel`):**
1. Crear un panel bajo el Canvas (copiar el estilo del menú de upgrades).
2. Agregar el componente `MasteryPanel` a un GameObject siempre activo del Canvas
   y asignar:
   - `panelRoot` → el GameObject del panel.
   - `pointsText` (TMP) → muestra "Mastery: 12 (+24% Gold)".
   - `earnableText` (TMP) → puntos que daría prestigiar ahora.
   - `progressFill` → Image con Image Type = **Filled** (barra al próximo punto).
   - `prestigeButton` → botón "PRESTIGE".
   - `confirmPanel` + `confirmButton` + `cancelButton` → sub-panel "¿Estás seguro?".
   - `closeButton` → cerrar.
3. Botón del HUD → OnClick → `MasteryPanel.OpenPanel()`.

**Barra del HUD superior (GDD):** agregar `MasteryProgressHUD` a un GameObject
del HUD con una Image Filled (`progressFill`) y un TMP opcional (`label`).

## 2. Misiones diarias

**Cómo funciona:** cada día (fecha local) se eligen 3 misiones del pool de
`Assets/Resources/Missions/` de forma determinística (misma fecha = mismas
misiones para todos). El progreso se trackea solo con hooks ya integrados:
matar enemigos, ganar oro, comprar niveles, ver rewarded ads y derrotar jefes.

**Panel (`DailyMissionsPanel` + 3 filas `MissionEntryUI`):**
1. Crear el panel con 3 filas (una por misión). Cada fila lleva el componente
   `MissionEntryUI` con:
   - `descriptionText` (TMP) → descripción.
   - `progressText` (TMP) → "35 / 150".
   - `progressFill` → Image Filled (opcional).
   - `rewardText` (TMP) → "1.2K Gold + 1 Mastery" (opcional).
   - `claimButton` → botón CLAIM (se habilita al completar).
   - `claimedIndicator` → GameObject con un tilde (opcional).
2. Agregar `DailyMissionsPanel` y asignar `panelRoot`, `entries` (las 3 filas),
   `closeButton` y opcionalmente `pendingBadge` (puntito rojo sobre el botón del
   HUD cuando hay recompensas sin reclamar).
3. Botón del HUD → OnClick → `DailyMissionsPanel.OpenPanel()`.

**Agregar misiones nuevas:** click derecho → Create → Idle → Missions → Mission,
guardar en `Resources/Missions/`. El `missionId` debe ser único y NO cambiarse
después (el save lo referencia).

## 3. Tienda

**Cómo funciona:** 3 items fijos —
- **Gold Rush** (rewarded ad): oro instantáneo = 10 min de GPS (mínimo 500).
- **Frenzy** (rewarded ad): ×2 oro por 10 minutos.
- **Lucky Horseshoe** (cuesta oro): ×1.5 oro por 30 minutos.

Solo un boost activo a la vez (repetir el mismo lo extiende). El tiempo restante
persiste y descuenta el tiempo con el juego cerrado.

**Panel (`ShopPanel`):**
1. Crear el panel con 3 botones de compra.
2. Agregar `ShopPanel` y asignar:
   - `panelRoot`, `closeButton`.
   - `goldRushButton` + `goldRushValueText` (TMP, muestra cuánto oro da).
   - `frenzyButton` + `frenzyValueText`.
   - `horseshoeButton` + `horseshoeCostText` (se deshabilita solo si no alcanza el oro).
   - `boostStatusText` (TMP) → "x2 Gold active - 08:15 left".
3. Botón del HUD → OnClick → `ShopPanel.OpenPanel()`.

---

## Checklist de prueba rápida (en el editor)

1. Abrir `GameScene`, dar Play: en la consola debería aparecer
   `[ProgressionBootstrap] ProgressionSystems creado` y
   `[DailyMissionManager] Misiones del día ...`.
2. Matar enemigos / comprar upgrades → abrir misiones y ver el progreso.
3. Ganar >100K de oro acumulado → el botón PRESTIGE se habilita; prestigiar
   resetea upgrades/oro y suma puntos (el GPS mostrado incluye el bonus).
4. Comprar Lucky Horseshoe → el GPS mostrado sube ×1.5 y expira a los 30 min.
5. Cerrar y reabrir: misiones, maestría y boost restantes deben persistir
   (el save ahora está en `persistentDataPath/savegame.json`).

## Notas de balance

Valores por defecto pensados como punto de partida; se tunean en:
- Maestría: constantes serializadas de `MasteryManager` (100K / +2% por punto).
- Tienda: constantes serializadas de `ShopManager` (duraciones, multiplicadores, costos).
- Misiones: cada asset en `Resources/Missions` (targets y recompensas).
