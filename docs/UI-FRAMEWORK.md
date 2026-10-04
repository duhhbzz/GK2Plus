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
- reusable native item-requirement cells

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
- legacy/shared action buttons
- exact native red SpriteSwap buttons
- native inspiration-style progress bars
- native tooltip containers, text, and separators
- flat category/tab buttons

### `GK2UiWindowBuilder`

Creates modal GK2+ window chrome:

- fullscreen overlay/dimmer
- top sorting canvas
- native frame/background
- content safe area

The F2 mod menu now uses this builder. Its frame interior uses the exact
`comm-frame_bg_1` + 13-unit inset recipe rather than the old title-screen
background approximation.

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

### `GK2UiItemRequirementBuilder`

Creates a reusable native-style item requirement cell with the game's item
sprite material/tint and enough/not-enough count styling. Quest Journal and
future recipe/build views can share this control.

### `GK2UiPool<T>`

Small reusable view pool. Runtime HUDs and future dynamic lists should use it
rather than destroying/recreating rows every refresh.

The tracker HUD uses pooled ingredient cells.

## Native GK2 v1.008 catalog

The reusable native primitives below were captured directly from live GK2
v1.008 UI with UnityExplorer. UnityExplorer is discovery tooling only; GK2+
does **not** depend on UnityExplorer or UniverseLib at runtime.

### Window frame

Native window stone is a two-layer composition:

- border: `comm-frame_1-border`, Sliced
- interior: `comm-frame_bg_1`, Simple
- interior inset: 13 logical UI units on all four sides

Do not use `titlescreen-menu-bg` as a generic window interior. It is not the
stone panel used by normal GK2 windows.

### Red action button

`GK2UiFactory.CreateNativeRedButton` uses the native SpriteSwap set:

- normal: `comm-btn-simple_red-active`
- hover/selected: `comm-btn-simple_red-over`
- pressed: `comm-btn-simple_red-press`
- disabled: `comm-btn-simple_red-inactive`

The native control is 162x26 in the pause menu, uses a Tiled background,
10-unit left/right text padding, 2-unit top padding, and
`small_font_bold` at 16.

### Item cell

The reusable item requirement cell now follows the native `UIItemCell`
geometry:

- logical cell: 42x42
- background: `widget_items_cell-inventory_item_cell`, Simple, 44x44
- item art: 48x48 with the native `ItemCellMaterial`
- selection: `selection`, Sliced
- inactive shade: `comm-item-inactive_shade`

The builder scales these native proportions when a feature requests a larger
or smaller logical cell.

### Inspiration progress bar

`GK2UiFactory.CreateNativeProgressBar` reproduces the vanilla inspiration
bar:

- frame: `inspiration-plate-green-value_frame`, Sliced
- fill: `inspiration-plate-green-bar_fill`, Sliced
- non-interactable Unity `Slider`, left-to-right, range 0..1
- fill is driven by the Slider's fill RectTransform, not Image.fillAmount
- label: `tiny_font`, 16, centered, using the native progress material

### Tooltip

`GK2UiFactory.CreateNativeTooltip` and its text/separator helpers reproduce
the native tooltip container:

- frame: `hint-frame`, Sliced, 20-unit sprite border
- padding: 14 on all sides
- spacing: 2
- minimum width: 60
- preferred width: 200
- separator: `hint-text_separator`, 96x6
- optional tail asset: `hint-frame_tail`, 10x10
- regular text: `small_font`, 16
- additional/meta text: `tiny_font`, 16
- native tooltip sorting order: 700

Tail corner/orientation behavior remains data in the theme until a feature
actually needs a directional tooltip pointer; no guessed rotation logic is
used.

### Confirmation dialog

`GK2UiDialogBuilder` reproduces the reusable `UIDialogWindow` shell:

- overlay shadow: black at 40% alpha
- sorting order: 415
- minimum window: 200x100
- GenericWindowLayout padding: left/right 24, top 47, bottom 23
- content spacing: 6
- body: `small_font`, 16, native `regular_text_3` material
- actions use the native red-button builder

### Main-window header/tab assets

The theme also caches the exact CharacterWindow header/tab primitives for
future builders:

- `main_window-header_1`
- `main_window-header_1-dec_side_3`
- `comm-header_frame_splitter`
- `main_window-header_1-button`
- `main_window-header_1-button-over`
- `main_window-header_1-dec_separator`

These are deliberately cataloged without inventing geometry that was not
captured.

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

### RPG Quest Journal

The native Quests tab can now host a two-pane RPG journal built with the same
theme, metrics, pooling, scrolling, and item-requirement primitives. The
journal controller is presentation-only and falls back to the vanilla
`QuestTreePageWidget` if replacement initialization fails.

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
