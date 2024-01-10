<div align="center">
  <img src="https://github.com/DEAFPS/SharpTimer/assets/43534349/8097684d-8b0b-4c39-b093-0acc76ee8fd6" alt="">
</div>

# SharpTimer
SharpTimer is a simple Surf/KZ/Bhop/MG/Deathrun/etc. CS2 Timer plugin using CounterStrikeSharp<br>


## Features
<details> 
  <summary>Timer, speedometer and key input with color customization</summary>
   <img src="https://i.imgur.com/TxAwgbC.png">
</details>

<details> 
  <summary>Players PB</summary>
  <img src="https://i.imgur.com/9HGOhRR.png">
</details>

<details> 
  <summary>Surf Stages and Checkpoints</summary>
  <img src="https://i.imgur.com/xL2y6vs.png">
</details>

<details> 
    <summary>Bonus stages</summary>
  <img src="https://i.imgur.com/NURlZBK.png">
</details>

<details> 
  <summary>Rank Icons</summary>
  <img src="https://i.imgur.com/7vSKeCv.png">
</details>

<details> 
  <summary>KZ Checkpoint system (disabled by default)</summary>
   <img src="https://i.imgur.com/USX5i8C.png"><br>
   <img src="https://i.imgur.com/kWiHOlz.png"><br>
   <img src="https://i.imgur.com/lXwXNN7.png"><br>
   <img src="https://i.imgur.com/nyn76Q4.png">
</details>

## Dependencies

[**MetaMod**](https://cs2.poggu.me/metamod/installation/)

[**CounterStrikeSharp** *(at least v141)*](https://github.com/roflmuffin/CounterStrikeSharp/releases)

[**MovementUnlocker** *(optional but recommended)*](https://github.com/Source2ZE/MovementUnlocker)

[**Web panel** *(optional but recommended)*](https://github.com/Letaryat/sharptimer-web-panel)

:exclamation: **CS2Fixes** does clash with **CSS** there fore the plugin might not work correctly with it

## Install
* Download the [latest release](https://github.com/DEAFPS/SharpTimer/releases),

* Unzip into your servers `game/csgo/` directory,

* :exclamation: See `game/csgo/cfg/SharpTimer/config.cfg` for basic plugin configuration,

* :exclamation: It is recommended to have a custom server cfg with your desired settings (for example [SURF](https://github.com/DEAFPS/cs-cfg/blob/main/surf.cfg) or [KZ](https://github.com/DEAFPS/cs-cfg/blob/main/kz.cfg)),

## MySQL
* Head over to `game/csgo/cfg/SharpTimer/config.cfg` and enable `sharptimer_mysql_enabled`,

* Configure MySQL connection with database credentials in `mysqlConfig.json`, which is located in the same directory,

## Commands

| Command  | What it does |
| ------------- | ------------- |
| `!r`  | Teleports the player back to Spawn |
| `!rb`  | Teleports the player to a Bonus Spawn |
| `!top`  | Prints the top 10 times on the current map |
| `!topbonus`  | Prints the top 10 bonus times on the current map |
| `!rank` | Tells you your rank on the current map |
| `!sr` | Tells you the current server record of the map |
| `!hud` | Hides the Timer HUD |
| `!keys` | Hides the HUD Keys |
| `!azerty` | Changes Key Layout to Azerty on the HUD |
| `!cp` | Sets a checkpoint |
| `!tp` | Teleports the player to the latest checkpoint |
| `!prevcp` | Teleports the player to the previous checkpoint |
| `!nextcp` | Teleports the player to the previous checkpoint |
| `!timer` | Stops timer and blocks it from starting |´
| `!goto` | Teleports the player to another player by name |
| `!stage` | Teleports the player to a stage by index |
| `!sthelp` | Prints commdands for player |
| `!stver` | Prints the current plugin version |


## Admin Commands
These commands require the `@css/root` admin flag

| Command  | What it does |
| ------------- | ------------- |
| `!noclip`  | Enables noclip for admin |

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
* ⚠️ Only use these as "the last resort" you will run into issues otherwise if a map supports default triggers and you decide to add "fake" zone triggers regardless!
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


* These are the supported info_target targetnames

| Start Zone info_target targetname  | End Zone info_target targetname |
| ------------- | ------------- |
| left_start  | left_end  |
| right_start | right_end |

## Author: [@DEAFPS_](https://twitter.com/deafps_)
