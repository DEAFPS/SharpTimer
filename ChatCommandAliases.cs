using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_saveloc", "alias for !cp")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SaveLocAlias(CCSPlayerController player, CommandInfo commandInfo)
        {
            SetPlayerCP(player, commandInfo, true);
        }

        [ConsoleCommand("css_loadloc", "alias for !tp")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void LoadLocAlias(CCSPlayerController player, CommandInfo commandInfo)
        {
            TpPlayerCP(player, commandInfo, true);
        }

        [ConsoleCommand("css_prevloc", "alias for !prevcp")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void PrevLocAlias(CCSPlayerController player, CommandInfo commandInfo)
        {
            TpPreviousCP(player, commandInfo, true);
        }

        [ConsoleCommand("css_nextloc", "alias for !nextcp")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void NextLocAlias(CCSPlayerController player, CommandInfo commandInfo)
        {
            TpNextCP(player, commandInfo, true);
        }

        [ConsoleCommand("css_b", "alias for !rb")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void BAlias(CCSPlayerController player, CommandInfo commandInfo)
        {
            RespawnBonusPlayer(player, commandInfo);
        }
    }
}