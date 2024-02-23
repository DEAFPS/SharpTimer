using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_help", "alias for !sthelp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void StHelpAlias(CCSPlayerController? player, CommandInfo command)
        {
            PrintAllEnabledCommands(player);
        }
        
        [ConsoleCommand("css_saveloc", "alias for !cp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SaveLocAlias(CCSPlayerController? player, CommandInfo command)
        {
            SetPlayerCP(player, command);
        }

        [ConsoleCommand("css_loadloc", "alias for !tp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void LoadLocAlias(CCSPlayerController? player, CommandInfo command)
        {
            TpPlayerCP(player, command);
        }

        [ConsoleCommand("css_prevloc", "alias for !prevcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrevLocAlias(CCSPlayerController? player, CommandInfo command)
        {
            TpPreviousCP(player, command);
        }

        [ConsoleCommand("css_nextloc", "alias for !nextcp")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void NextLocAlias(CCSPlayerController? player, CommandInfo command)
        {
            TpNextCP(player, command);
        }

        [ConsoleCommand("css_b", "alias for !rb")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void BAlias(CCSPlayerController? player, CommandInfo command)
        {
            RespawnBonusPlayer(player, command);
        }

        [ConsoleCommand("css_btop", "alias for !topbonus")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TopBonusAlias(CCSPlayerController? player, CommandInfo command)
        {
            PrintTopBonusRecords(player, command);
        }

        [ConsoleCommand("css_mtop", "alias for !top")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TopAlias(CCSPlayerController? player, CommandInfo command)
        {
            PrintTopRecords(player, command);
        }
    }
}