using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("sharptimer_hostname", "Default Server Hostname.")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerServerHostname(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                defaultServerHostname = $"A SharpTimer Server";
                return;
            }

            defaultServerHostname = $"{args}";
        }

        [ConsoleCommand("sharptimer_autoset_mapinfo_hostname_enabled", "Whether Map Name and Map Tier (if available) should be put into the hostname or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerHostnameConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            autosetHostname = bool.TryParse(args, out bool autosetHostnameValue) ? autosetHostnameValue : args != "0" && autosetHostname;
        }

        [ConsoleCommand("sharptimer_global_ranks_enabled", "Whether the plugin should reward players with global points for compleating maps. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGlobalRanksConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            globalRankeEnabled = bool.TryParse(args, out bool globalRankeEnabledValue) ? globalRankeEnabledValue : args != "0" && globalRankeEnabled;
        }

        [ConsoleCommand("sharptimer_replays_enabled", "Whether replays should be enabled or not. This option might be performance taxing and use more ram & cpu. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerReplayConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableReplays = bool.TryParse(args, out bool enableReplaysValue) ? enableReplaysValue : args != "0" && enableReplays;
        }

        [ConsoleCommand("sharptimer_debug_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConPrintConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            enableDebug = bool.TryParse(args, out bool enableDebugValue) ? enableDebugValue : args != "0" && enableDebug;
        }

        [ConsoleCommand("sharptimer_use2Dspeed_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimer2dSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            use2DSpeed = bool.TryParse(args, out bool use2DSpeedValue) ? use2DSpeedValue : args != "0" && use2DSpeed;
        }

        [ConsoleCommand("sharptimer_override_beam_colors_enabled", "Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerBeamColorsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            beamColorOverride = bool.TryParse(args, out bool beamColorOverrideValue) ? beamColorOverrideValue : args != "0" && beamColorOverride;
        }

        [ConsoleCommand("sharptimer_start_beam_color", "Start beam color, Requires sharptimer_override_beam_colors_enabled true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerStartBeamColor(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                startBeamColor = $"";
                return;
            }

            startBeamColor = $"{args}";
        }

        [ConsoleCommand("sharptimer_end_beam_color", "Start beam color, Requires sharptimer_override_beam_colors_enabled true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerEndBeamColor(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                endBeamColor = $"";
                return;
            }

            endBeamColor = $"{args}";
        }
        
        [ConsoleCommand("sharptimer_mysql_enabled", "Whether player times should be put into a mysql database by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMySQLConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            useMySQL = bool.TryParse(args, out bool useMySQLValue) ? useMySQLValue : args != "0" && useMySQL;
        }

        [ConsoleCommand("sharptimer_command_spam_cooldown", "Defines the time between commands can be called. Default value: 1")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCmdCooldownConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, out float cooldown) && cooldown > 0)
            {
                cmdCooldown = (int)(cooldown * 64);
                SharpTimerConPrint($"SharpTimer command cooldown set to {cooldown} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid command cooldown value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_respawn_enabled", "Whether !r is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRespawnConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            respawnEnabled = bool.TryParse(args, out bool respawnEnabledValue) ? respawnEnabledValue : args != "0" && respawnEnabled;
        }

        [ConsoleCommand("sharptimer_top_enabled", "Whether !top is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTopConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            topEnabled = bool.TryParse(args, out bool topEnabledValue) ? topEnabledValue : args != "0" && topEnabled;
        }

        [ConsoleCommand("sharptimer_rank_enabled", "Whether !rank is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRankConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            rankEnabled = bool.TryParse(args, out bool rankEnabledValue) ? rankEnabledValue : args != "0" && rankEnabled;
        }

        [ConsoleCommand("sharptimer_goto_enabled", "Whether !goto is enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerGoToConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            goToEnabled = bool.TryParse(args, out bool goToEnabledValue) ? goToEnabledValue : args != "0" && goToEnabled;
        }

        [ConsoleCommand("sharptimer_remove_legs", "Whether Legs should be removed or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveLegsConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeLegsEnabled = bool.TryParse(args, out bool removeLegsEnabledValue) ? removeLegsEnabledValue : args != "0" && removeLegsEnabled;
        }

        [ConsoleCommand("sharptimer_remove_damage", "Whether dealing damage should be disabled or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveDamageConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            disableDamage = bool.TryParse(args, out bool disableDamageValue) ? disableDamageValue : args != "0" && disableDamage;
        }

        [ConsoleCommand("sharptimer_remove_collision", "Whether Player collision should be removed or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveCollisionConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCollisionEnabled = bool.TryParse(args, out bool removeCollisionEnabledValue) ? removeCollisionEnabledValue : args != "0" && removeCollisionEnabled;
        }

        [ConsoleCommand("sharptimer_trigger_push_fix", "When enabled all trigger_push ents will only push once OnStartTouch. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTriggerPushFixConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            triggerPushFixEnabled = bool.TryParse(args, out bool triggerPushFixEnabledValue) ? triggerPushFixEnabledValue : args != "0" && triggerPushFixEnabled;
        }

        [ConsoleCommand("sharptimer_checkpoints_enabled", "Whether !cp, !tp and !prevcp are enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCPConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cpEnabled = bool.TryParse(args, out bool cpEnabledValue) ? cpEnabledValue : args != "0" && cpEnabled;
        }

        [ConsoleCommand("sharptimer_remove_checkpoints_restrictions", "Whether checkpoints should save in the air with the current player speed. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCPRestrictConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCpRestrictEnabled = bool.TryParse(args, out bool removeCpRestrictEnabledValue) ? removeCpRestrictEnabledValue : args != "0" && removeCpRestrictEnabled;
        }

        [ConsoleCommand("sharptimer_disable_telehop", "Whether the players speed should loose all speed when entring a teleport map trigger or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerResetTeleportSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            resetTriggerTeleportSpeedEnabled = bool.TryParse(args, out bool resetTriggerTeleportSpeedEnabledValue) ? resetTriggerTeleportSpeedEnabledValue : args != "0" && resetTriggerTeleportSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_max_start_speed_enabled", "Whether the players speed should be limited on exiting the starting trigger or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxStartSpeedBoolConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            maxStartingSpeedEnabled = bool.TryParse(args, out bool maxStartingSpeedEnabledValue) ? maxStartingSpeedEnabledValue : args != "0" && maxStartingSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_max_start_speed", "Defines max speed the player is allowed to have while exiting the start trigger. Default value: 120")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxStartSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int speed) && speed > 0)
            {
                maxStartingSpeed = speed;
                SharpTimerConPrint($"SharpTimer max trigger speed set to {speed}.");
            }
            else
            {
                SharpTimerConPrint("Invalid max trigger speed value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_force_knife_speed", "Whether the players speed should be always knife speed regardless of weapon held. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerForceKnifeSpeedBoolConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            forcePlayerSpeedEnabled = bool.TryParse(args, out bool forcePlayerSpeedEnabledValue) ? forcePlayerSpeedEnabledValue : args != "0" && forcePlayerSpeedEnabled;
        }

        [ConsoleCommand("sharptimer_forced_player_speed", "Speed override for sharptimer_force_knife_speed. Default value: 250")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerForcedSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int speed) && speed > 0)
            {
                forcedPlayerSpeed = speed;
                SharpTimerConPrint($"SharpTimer forced player speed set to {speed}.");
            }
            else
            {
                SharpTimerConPrint("Invalid forced player speed value. Please provide a positive integer.");
            }
        }


        [ConsoleCommand("sharptimer_connect_commands_msg_enabled", "Whether commands on join messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConnectCmdMSGConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cmdJoinMsgEnabled = bool.TryParse(args, out bool cmdJoinMsgEnabledValue) ? cmdJoinMsgEnabledValue : args != "0" && cmdJoinMsgEnabled;
        }

        [ConsoleCommand("sharptimer_connectmsg_enabled", "Whether connect/disconnect messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConnectMSGConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            connectMsgEnabled = bool.TryParse(args, out bool connectMsgEnabledValue) ? connectMsgEnabledValue : args != "0" && connectMsgEnabled;
        }

        [ConsoleCommand("sharptimer_remove_crouch_fatigue", "Whether the player should get no crouch fatigue or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoveCrouchFatigueConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCrouchFatigueEnabled = bool.TryParse(args, out bool removeCrouchFatigueEnabledValue) ? removeCrouchFatigueEnabledValue : args != "0" && removeCrouchFatigueEnabled;
        }

        [ConsoleCommand("sharptimer_sr_ad_enabled", "Whether timed Server Record messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSRConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            srEnabled = bool.TryParse(args, out bool srEnabledValue) ? srEnabledValue : args != "0" && srEnabled;
        }

        [ConsoleCommand("sharptimer_checkpoints_only_when_timer_stopped", "Will only allow checkpoints if timer is stopped using !timer")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCheckpointsOnlyWithStoppedTimer(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cpOnlyWhenTimerStopped = bool.TryParse(args, out bool cpOnlyWhenTimerStoppedValue) ? cpOnlyWhenTimerStoppedValue : args != "0" && cpOnlyWhenTimerStopped;
        }

        [ConsoleCommand("sharptimer_velo_bar_enabled", "Whether the alternative speedometer is enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAltVeloConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            alternativeSpeedometer = bool.TryParse(args, out bool alternativeSpeedometerValue) ? alternativeSpeedometerValue : args != "0" && alternativeSpeedometer;
        }

        [ConsoleCommand("sharptimer_velo_bar_max_speed", "The alternative speedometer max speed. Default value: 3000")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerAltVeloMaxSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int interval) && interval > 0)
            {
                altVeloMaxSpeed = interval;
                SharpTimerConPrint($"SharpTimer Alternative Velo Max Speed set to {interval} units/s.");
            }
            else
            {
                SharpTimerConPrint("Invalid Alternative Velo Max Speed. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_sr_ad_timer", "Interval how often SR shall be printed to chat. Default value: 120")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerMaxSRSpeedConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int interval) && interval > 0)
            {
                srTimer = interval;
                SharpTimerConPrint($"SharpTimer sr ad interval set to {interval} seconds.");
            }
            else
            {
                SharpTimerConPrint("Invalid sr ad interval value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_chat_prefix", "Default value of chat prefix for SharpTimer messages. Default value: [SharpTimer]")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerChatPrefix(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                msgPrefix = $" {ParseColorToSymbol(primaryHUDcolor)} [SharpTimer] {ChatColors.White}";
                return;
            }

            msgPrefix = $" {ParseColorToSymbol(primaryHUDcolor)} {args} {ChatColors.White}";
        }

        [ConsoleCommand("sharptimer_hud_primary_color", "Primary Color for Timer HUD. Default value: green")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerPrimaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                primaryHUDcolor = $"green";
                return;
            }

            primaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_hud_secondary_color", "Secondary Color for Timer HUD. Default value: orange")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSecondaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                secondaryHUDcolor = $"orange";
                return;
            }

            secondaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_hud_tertiary_color", "Tertiary Color for Timer HUD. Default value: white")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTertiaryHUDcolor(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                tertiaryHUDcolor = $"white";
                return;
            }

            tertiaryHUDcolor = $"{args}";
        }

        [ConsoleCommand("sharptimer_fake_trigger_height", " ")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerFakeTriggerHeightConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (float.TryParse(args, out float height) && height > 0)
            {
                fakeTriggerHeight = height;
                SharpTimerConPrint($"SharpTimer fake trigger height set to {height} units.");
            }
            else
            {
                SharpTimerConPrint("Invalid fake trigger height value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_remote_data_bhop", "Override for bhop remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideBhop(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteBhopDataSource = $"https://raw.githubusercontent.com/DEAFPS/SharpTimer/remote_data/bhop_.json";
                return;
            }

            remoteBhopDataSource = $"{args}";
        }

        [ConsoleCommand("sharptimer_remote_data_kz", "Override for kz remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideKZ(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteBhopDataSource = $"https://raw.githubusercontent.com/DEAFPS/SharpTimer/remote_data/kz_.json";
                return;
            }

            remoteKZDataSource = $"{args}";
        }

        [ConsoleCommand("sharptimer_remote_data_surf", "Override for surf remote_data")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRemoteDataOverrideSurf(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                remoteBhopDataSource = $"https://raw.githubusercontent.com/DEAFPS/SharpTimer/remote_data/surf_.json";
                return;
            }

            remoteSurfDataSource = $"{args}";
        }
    }
}