# GK2+ UI Framework

GK2+ UI is built through a small shared framework under
`src/GK2Plus/Framework/UI`. Feature code should register data/actions with
`GK2UIService` and avoid creating arbitrary Unity UI hierarchies itself.

## Goals

- Reuse Graveyard Keeper 2 fonts, sprites, text styles, item-cell visuals, and
  button language instead of shipping a visually separate UI skin.
- Keep sizes and placement tunable from one location.
- Pool/reuse runtime controls rather than destroying/recreating them during
  gameplay.
- Make new screens predictable: theme -> metrics -> factory -> view/controller.

## Core pieces

### `GK2UiTheme`

Resolves native GK2 UI assets once and caches them:

- Inventory/header text template
- building/list text template
- button text template
- window frame/background/divider/button sprites
- native item-cell slot sprite and icon material
- native item-count normal/red text styles

The theme cache can be enriched later when additional native widgets have
loaded. It is reset when the UI service shuts down.

### `GK2UiMetrics`

Central source of logical UI sizes and spacing.

Use this file first when changing:

- mod-menu dimensions
- tab positions and spacing
- content viewport size
- feature-row spacing
- tracker panel width
- tracker title/body padding
- ingredient-cell size, icon size, and count font size

Avoid adding new magic dimensions directly to controllers when a reusable
metric makes sense.

### `GK2UiFactory`

Creates shared Unity UI primitives:

- RectTransform roots
- images/panels
- TMP labels copied from native templates
- native-style action buttons
- flat category/tab buttons

### `GK2UiWindowBuilder`

Creates modal GK2+ window chrome:

- fullscreen overlay/dimmer
- top sorting canvas
- native frame/background
- content safe area

The F2 mod menu now uses this builder.

### `GK2UiListRowBuilder`

Creates a consistent settings/list row with:

- native list typography
- shared row/child-row colors
- standard label indentation
- right-aligned action control
- centralized row/button sizing

Feature toggles and options in the F2 menu use this builder.

### `GK2UiSectionPanelBuilder`

Creates compact titled panels for HUD widgets and other small in-game surfaces.
The Tracker's Quests / Crafts / Items sections use this builder.

### `GK2UiPool<T>`

Small reusable view pool. Runtime HUDs and future dynamic lists should use it
rather than destroying/recreating rows every refresh.

The tracker HUD uses pooled ingredient cells.

## Controller rules

1. Controllers own state and lifecycle, not visual constants.
2. Pull visual assets from `GK2UiTheme`.
3. Pull dimensions from `GK2UiMetrics`.
4. Create controls through `GK2UiFactory` / builders.
5. Pool frequently updated rows.
6. Only rebuild hierarchy when structure changes. Update labels/counts/images
   in place for normal gameplay refreshes.
7. Feature modules should register through `GK2UIService` instead of reaching
   into menu controllers directly.

## Current migrations

### F2 mod menu

The shell now uses:

- shared native theme cache
- centralized menu metrics
- shared modal window builder
- shared flat tab controls
- reusable native settings rows
- shared action-button creation

This gives the menu a darker GK2-style slate body, parchment/brown header,
native header/list typography, and adjustable dimensions from one metrics file.

### Unified Tracker HUD

The HUD now uses:

- shared theme and metrics
- pooled material views
- cached native item slot/count/icon materials
- native blue-channel replacement for item icons
- content-driven panel sizing

The renderer skips hierarchy/layout work when the tracker snapshot has not
changed.

## Adding future UI

For a new feature screen:

1. Register the feature/action with `GK2UIService`.
2. Add any reusable dimensions to `GK2UiMetrics`.
3. Use `GK2UiTheme.Current` / `Resolve` for native styling.
4. Build static controls with `GK2UiFactory`.
5. Use `GK2UiPool<T>` for repeated/dynamic rows.
6. Prefer a dedicated controller/view class instead of extending
   `ModMenuController` with feature-specific rendering logic.

The long-term direction is to keep `ModMenuController` as navigation/window
orchestration while feature pages become independent reusable views.
