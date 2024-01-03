# SharpTimer
SharpTimer is a simple Surf/KZ/Bhop/MG/Deathrun/etc. CS2 Timer plugin using CounterStrikeSharp<br>
Author: [@DEAFPS_](https://twitter.com/deafps_)

## Features
<details> 
  <summary>Timer, speedometer and key input</summary>
   <img src="https://i.imgur.com/cGUjH6m.png">
</details>

<details> 
  <summary>Players personal best</summary>
  <img src="https://i.imgur.com/9HGOhRR.png">
</details>

<details> 
  <summary>Checkpoint system (disabled by default)</summary>
   <img src="https://i.imgur.com/USX5i8C.png"><br>
   <img src="https://i.imgur.com/kWiHOlz.png"><br>
   <img src="https://i.imgur.com/lXwXNN7.png"><br>
   <img src="https://i.imgur.com/nyn76Q4.png">
</details>

## Dependencies

[**MetaMod**](https://cs2.poggu.me/metamod/installation/)

[**CounterStrikeSharp** *(at least v126)*](https://github.com/roflmuffin/CounterStrikeSharp/releases)

[**MovementUnlocker** *(optional but recommended)*](https://github.com/Source2ZE/MovementUnlocker)

[**Web panel** *(optional but recommended)*](https://github.com/Letaryat/sharptimer-web-panel)

:exclamation: **CS2Fixes** does clash with **CSS** there fore the plugin might not work correctly with it

## Install
* Download the [latest release](https://github.com/DEAFPS/SharpTimer/releases),

* Unzip into your servers `game/csgo/` directory,

* :exclamation: See `game/csgo/cfg/SharpTimer/config.cfg` for basic plugin configuration,

* :exclamation: It is recommended to have a custom server cfg with your desired settings (for example [SURF](https://github.com/DEAFPS/cs-cfg/blob/main/surf.cfg) or [KZ](https://github.com/DEAFPS/cs-cfg/blob/main/kz.cfg)),

* Here is a [collection of maps that are supported by default](https://steamcommunity.com/sharedfiles/filedetails/?id=3095738559),

## MySQL
* Head over to `game/csgo/cfg/SharpTimer/config.cfg` and enable `sharptimer_mysql_enabled`,

* Configure MySQL connection with database credentials in `mysqlConfig.json`, which is located in the same directory,

## Commands

| Command  | What it does |
| ------------- | ------------- |
| `!r`  | Teleports the player back to Spawn |
| `!top`  | Prints the top 10 times on the current map |
| `!rank` | Tells you your rank on the current map |
| `!pb` | Tells you your PB on the current map |
| `!sr` | Tells you the current server record of the map |
| `!hud` | Hides the Timer HUD |
| `!azerty` | Changes Key Layout to Azerty on the HUD |
| `!cp` | Sets a checkpoint |
| `!tp` | Teleports the player to the latest checkpoint |
| `!prevcp` | Teleports the player to the previous checkpoint |
| `!nextcp` | Teleports the player to the previous checkpoint |

## Admin Commands
These commands require the `@css/root` admin flag

| Command  | What it does |
| ------------- | ------------- |
| `!jsontodatabase`  | Uploads all saved Records to the MySql Database from the local Json |
| `!databasetojson`  | Downloads all saved Records from the MySql Database to Json |
| `!addrespawnpos`  | Adds a manual respawn pos |
| `!addstartzone`  | Adds a manual start zone to |
| `!addendzone`  | Adds a manual end zone |
| `!savezones`  | Saves manually set zones to mapdata.json |

## Zones

### Adding Zone Triggers
* This plugin will look for trigger_multiple entities by default depending what map is being played. By default the plugin tries to hook the following target names:

| Start targetname  | End targetname |
| ------------- | ------------- |
| timer_startzone  | timer_endzone  |
| zone_start | zone_end |
| map_start | map_end  |
| s1_start |   |
| stage1_start |   |


* If the map uses different trigger targetnames or does not have triggers at all (most bhop and deathrun maps dont) you will have to add them into the `.json` files

* To add Map Start and End zones you can simply add the `targetnames` of the triggers inside a `.json` file in `game/csgo/cfg/SharpTimer/MapData` using `MapStartTrigger` and  `MapEndTrigger`

  You can check if a map has built in triggers with the following server commands:
  ```
  sv_cheats true
  ent_find trigger_multiple
  ```

### Adding "Fake" Zone Triggers
* Many maps do not contain any `startzone` or `endzone` triggers. As a server admin with a `@css/root` flag you can use `!addrespawnpos`, `!addstartzone`, `!addendzone` & `!savezones` to manually add "fake" zone triggers! [Example Video](https://streamable.com/9ez6gq)

<details>
<summary>Here is a Example of what the `map.json` can look like with both map triggers and manual triggers:</summary>

### surf_utopia_njv.json
```
{
  "MapStartTrigger": "zone_start",
  "MapEndTrigger": "zone_end"
}
```
### bhop_zentic.json
```
{
  "MapStartC1": "-67.89055 188.01341 64.03125",
  "MapStartC2": "123.32273 -187.58983 64.03125",
  "MapEndC1": "13736.031 1540.6246 -639.96875",
  "MapEndC2": "13884.47 1915.2767 -639.96875",
  "RespawnPos": "-2 0 64.03125"
}
```

</details>

### Adding Zone outline guides
* This plugin will look for info_target entities to define the zone outlines. Mappers can place these at the opposite corners of the zone triggers.<details> 
  <summary>EXAMPLE</summary>
   <img src="https://i.imgur.com/8nJBHaH.jpeg">
</details>


* These are the supported info_target targetnames

| Start Zone info_target targetname  | End Zone info_target targetname |
| ------------- | ------------- |
| left_start  | left_end  |
| right_start | right_end |



