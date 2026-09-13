# Pepper Mod

A small Vintage Story code/content mod for perennial pepper plants.

## Status

Version 0.4.0 completes serranos alongside jalapenos and habaneros, including baking, bundles, ceiling hanging, air drying, and optional Biomes regional spawning.

Work in progress. All three finished varieties have 11 plant states, picked fruit models, seeds, perennial growth, and spice effects. Serrano assets and gameplay integration pass automated checks but still need a native in-game playtest. The latest Habanero fruit rework also needs an in-game visual check. The other five varieties remain unfinished.

## Current Content

- Pepper plants with 8 initial growth stages, 2 mature regrowth stages, and 1 dormant winter stage
- Seeds with distinct inventory artwork for all eight pepper types
- Fresh vegetable items for each pepper type
- Scoville tooltips and two oven preparations: baked peppers (Charred Habaneros), then long-lasting dried peppers
- Eight-pepper jalapeno, habanero, and serrano bundles for baking 32 peppers in one full clay oven
- Ceiling-hung bundles that air-dry over seven in-game days
- Hold right-click for 1.5 seconds to harvest ripe plants without breaking them; peppers fall to the ground nearby
- Seasonal dormancy when temperatures are outside the growing range
- Rare wild jalapeno and serrano patches and rarer habanero patches in any biome with suitable soil
- Complete jalapeno, habanero, and serrano growth, dormant, harvested, and regrowing models
- Matching 3D fruit for inventory, held, and dropped items
- Short, full-bodied Habanero fruit with a blunt lobed end progresses from green to bright orange on the existing broad, leafy bush, with matching charred and dried skins for loose peppers and bundles
- Serrano's taller, narrower bush bears slender green harvest-ready fruit
- Placeholder models for the five remaining pepper types

Current pepper types:

```text
jalapeno
habanero
serrano
cayenne
poblano
bell-pepper
banana-pepper
ghost-pepper
```

## Install For Testing

Only jalapenos, habaneros, and serranos currently spawn wild, in any biome on suitable soil in newly generated terrain.
The remaining five varieties' wild spawning is disabled for now; their items and existing plants remain intact.
Existing terrain is not repopulated. Cold-weather dormancy and growing-season
temperature requirements still apply.

Build and install the compiled test package with:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\Build-PepperMod.ps1 -VintageStory "E:\Vintagestory" -Install
```

This is a code mod, so the release zip needs the compiled `peppermod.dll` at the root of the package. The build script creates `peppermod-0.4.0.zip` and copies it into your Vintage Story `Mods` folder when `-Install` is used. Keep only one version of Pepper Mod in that folder; move the old version outside it before testing this update.

## Biomes Compatibility

Optional support is included for [Biomes](https://mods.vintagestory.at/biomes),
tested against its 2.2.0 release on Vintage Story 1.22.3. No separate compatibility
download or required dependency is needed.

When Biomes is enabled, wild peppers follow regional spawning rules:

- Jalapenos: Pacific Nearctic, Pacific Neotropic, and Atlantic Neotropic.
- Habaneros: Atlantic Neotropic.
- Serranos: Pacific Nearctic, Pacific Neotropic, and Atlantic Neotropic, matching jalapenos.

All three can spawn near or away from rivers. Normal spawn rarity, suitable-soil checks,
and seasonal growing requirements are unchanged. Player-planted peppers are not
restricted by these world-generation rules. Without Biomes, wild peppers retain
their existing any-climate spawning rules.

In an existing Biomes world, explore newly generated terrain to find new patches;
existing terrain is not repopulated. When adding Biomes itself, follow its author's
recommendation to start a new world. This integration only adds our plant IDs to
Biomes' regional configuration; it does not alter terrain generation or other crops.

The optional asset is `assets/peppermod/config/biomes/blockconfig/peppermod.json`.
The released Biomes loader and plant filter are exercised in automated tests;
a combined in-game world-generation playtest is still recommended before release.

## Spice Effects

Raw jalapenos, habaneros, and serranos provide 20 satiety instead of 80. Each completed bite adds spice:
banana pepper 10, poblano 15, jalapeno 25, serrano 40, cayenne 50, habanero 50,
and ghost pepper 100. Bell peppers add no spice. The remaining varieties retain 80 satiety.
Two habaneros eaten back-to-back fill the spice meter from empty.
Serranos add 40 spice each: two reach Extreme at 80 and three fill the meter.

Spice is capped at 100 and appears in a three-segment HUD near the lower-right:

- Mild: above 0 and below 34, with no gameplay effect.
- Hot: 34 to below 67, gently warming the player's body.
- Extreme: 67 to 100, retaining warmth, adding a slowly pulsing red edge tint,
  and draining 0.5 satiety per real-time second (1 point every 2 seconds).

The additional hunger drain stops below Extreme, does not change nutrition
levels, and does not apply in Creative or Spectator mode. Hunger cannot fall
below zero; ordinary starvation rules still apply when it is empty.

Spice never applies health damage directly or fires damage events. Warming adds up to 0.12
degrees per real-time second, capped at 2 degrees above normal body temperature.
It stops below Hot; ordinary temperature simulation then controls cooling.

After five seconds without another spicy bite, spice decays by one point per
real-time second. The HUD disappears at zero. Spice pauses while offline, is
saved with the player, and clears on death. Raw, baked, and dried whole peppers
apply spice; mixed cooked dishes do not yet inherit spice from their ingredients.

## Baking And Scoville Heat

Hover over a raw, baked, or dried pepper to see its Scoville heat units (SHU).
Each oven stage halves both SHU and the spice-meter dose. These fixed values
and cooking reductions are gameplay balance, not a simulation of real-world
capsaicin concentration or food preservation.

Habanero's first cooked stage is now named **Charred Habanero**, with blackened,
blistered skin. This also applies to its portable and hanging bundles. Other
varieties still use **Baked**. Existing cooked Habaneros remain compatible with
saved worlds: internal `baked` codes, recipes, heat values, and timers are unchanged.

| Pepper | Raw SHU | Baked SHU | Dried SHU |
| --- | ---: | ---: | ---: |
| Jalapeno | 5,000 | 2,500 | 1,250 |
| Habanero | 250,000 | 125,000 | 62,500 |
| Serrano | 15,000 | 7,500 | 3,750 |
| Cayenne | 40,000 | 20,000 | 10,000 |
| Poblano | 1,500 | 750 | 375 |
| Bell pepper | 0 | 0 | 0 |
| Banana pepper | 500 | 250 | 125 |
| Ghost pepper | 1,000,000 | 500,000 | 250,000 |

Use a heated clay oven, as for bread: raw pepper -> baked pepper -> dried pepper.
Remove peppers at the baked stage to keep them baked. Return them for a second
batch, or leave them in the oven longer, to dry them. Drying takes longer than
the first bake at the same temperature. Dried is the final stage; further baking
does not reduce heat again. There is no firepit or cooking-pot recipe.

Loose raw, baked, and dried peppers use dedicated oven transforms to lie flat and
centered in their slots. Inventory, held, and dropped-item transforms are unchanged.

Base freshness is 7 days for raw jalapenos, habaneros, and serranos, 14 days (+/- 2) for other raw peppers,
7 days (+/- 1) for baked peppers, and 120 days for dried peppers, before temperature
and storage modifiers. After
freshness runs out, raw/baked peppers rot over 1 day and dried peppers over 7 days.
All three use normal food spoilage. The oven applies vanilla freshness carryover,
so an already-aged input does not produce a brand-new shelf-life timer.

Jalapeno spice doses are 25 / 12.5 / 6.25; habanero doses are 50 / 25 / 12.5.
Serrano doses are 40 / 20 / 10.
Two raw habaneros still fill the meter from empty. Satiety is unchanged by baking.
Prepared forms also exist for the five unfinished varieties, without enabling
their wild spawning or replacing their placeholder geometry.

The implementation uses the game's [BakingProperties](https://apidocs.vintagestory.at/api/Vintagestory.API.Common.BakingProperties.html)
and the installed Vintage Story 1.22.3 oven behavior. See
`art/previews/prepared-peppers.html` for the raw/baked/dried item preview.

## Pepper Bundles

Tie eight matching jalapenos, habaneros, or serranos with one **flax fiber** in the crafting grid:

```text
Pepper  Pepper  Pepper
Pepper  Fiber   Pepper
Pepper  Pepper  Pepper
```

All eight peppers must be the same variety and preparation: raw, baked, or dried.
One fiber is consumed per bundle. Bundles stack to eight and cannot be eaten directly.
Put one bundle anywhere in an otherwise empty crafting grid to unpack **eight peppers**
of that same variety and preparation. Unpacking does not return the fiber.

Each bundle takes one normal clay-oven slot, so four bundles process **32 peppers**.
The whole bundle progresses from raw to baked to dried at the existing baking times.
This does not change oven capacity or the behavior of other foods. The dried stage
is terminal, just like individual dried peppers.

Bundle tooltips show the pepper count and SHU **per pepper**, not eight times the SHU.
Shelf life matches the loose peppers. Tying uses the oldest ingredient's exact perish
timer; untying preserves it, preventing a freshness reset from repeated crafting.
Baking continues to use vanilla freshness carryover. A fully rotten bundle yields
two rot, equivalent to the normal rot ratio for eight peppers.

The nine ristra-style item models have eight peppers arranged around a central cord
at varied heights and angles, with each stem directly touching the cord and no
outward string arms. They lie flat in the oven and when dropped. The models reference
the game's linen texture for their cord and reuse the existing pepper skins.
An interactive preview is available at `art/previews/pepper-bundles.html`.

Right-click the underside of a ceiling while holding a bundle to hang one from its
loop. A small iron hook appears between the ceiling and the string, with the loop
resting in its curved end. This is part of the placed model only, not an additional
crafting ingredient or item. No special ceiling material is required; the block must have a
supporting underside and an empty, dry space below it. Right-click a hung bundle
to take it down. If your inventory is full, it drops below the hanging point.
Removing its support or flooding its space also drops the bundle.

Raw and baked bundles air-dry directly into dried bundles after **168 in-game hours
(7 days)** hanging. The variety and eight-pepper count stay the same. Progress is
saved and catches up when a chunk reloads. Taking a bundle down pauses drying;
rehanging resumes its saved progress. Only elapsed game time counts, not time
while the world is stopped.

Raw and baked bundles spoil at half the normal rate while hanging, allowing fresh
bundles to finish drying. Carried bundles and hung dried bundles use normal rates.
Drying carries over freshness using the
game's food conversion rules, so it does not reset old food to brand-new condition.
A bundle that starts spoiling before drying finishes will not become dried food;
when fully spoiled it drops two rot. Dried bundles can stay hanging as decoration
but still have the long, finite dried-pepper shelf life.


