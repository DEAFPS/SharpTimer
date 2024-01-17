using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.RegularExpressions;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        private bool IsValidStartTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validStartTriggers = { "map_start", "s1_start", "stage1_start", "timer_startzone", "zone_start", currentMapStartTrigger };
                return validStartTriggers.Contains(triggerName);
            }
            catch (NullReferenceException ex)
            {
                SharpTimerError($"Null ref in IsValidStartTriggerName: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidStartTriggerName: {ex.Message}");
                return false;
            }
        }

        private (bool valid, int X) IsValidStartBonusTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = {
                    @"^b([1-9][0-9]?)_start$",
                    @"^bonus([1-9][0-9]?)_start$",
                    @"^timer_bonus([1-9][0-9]?)_startzone$"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        return (true, X);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidStartBonusTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private (bool valid, int X) IsValidStageTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = {
                    @"^s([1-9][0-9]?)_start$",
                    @"^stage([1-9][0-9]?)_start$",
                    @"^map_start$",
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        if (pattern == @"^map_start$")
                        {
                            // If pattern is "^map_start$", set X to 1
                            return (true, 1);
                        }
                        else
                        {
                            int X = int.Parse(match.Groups[1].Value);
                            return (true, X);
                        }
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidStageTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private (bool valid, int X) IsValidCheckpointTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);

                string[] patterns = {
                    @"^map_cp([1-9][0-9]?)$",
                    @"^map_checkpoint([1-9][0-9]?)$"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        return (true, X);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidCheckpointTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private bool IsValidEndTriggerName(string triggerName)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return false;
                string[] validEndTriggers = { "map_end", "timer_endzone", "zone_end", currentMapEndTrigger };
                return validEndTriggers.Contains(triggerName);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidEndTriggerName: {ex.Message}");
                return false;
            }
        }

        private (bool valid, int X) IsValidEndBonusTriggerName(string triggerName, int playerSlot)
        {
            try
            {
                if (string.IsNullOrEmpty(triggerName)) return (false, 0);
                string[] patterns = {
                    @"^b([1-9][0-9]?)_end$",
                    @"^bonus([1-9][0-9]?)_end$",
                    @"^timer_bonus([1-9][0-9]?)_endzone$"
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(triggerName, pattern);
                    if (match.Success)
                    {
                        int X = int.Parse(match.Groups[1].Value);
                        if (X != playerTimers[playerSlot].BonusStage) return (false, 0);
                        return (true, X);
                    }
                }

                return (false, 0);
            }
            catch (Exception ex)
            {
                SharpTimerError($"Exception in IsValidStartTriggerName: {ex.Message}");
                return (false, 0);
            }
        }

        private void UpdateEntityCache()
        {
            entityCache.UpdateCache();
        }

        private (Vector?, QAngle?) FindStartTriggerPos()
        {
            currentRespawnPos = null;
            currentRespawnAng = null;

            foreach (var trigger in entityCache.Triggers)
            {
                if (trigger == null || trigger.Entity.Name == null || !IsValidStartTriggerName(trigger.Entity.Name.ToString()))
                    continue;

                foreach (var info_tp in entityCache.InfoTeleportDestinations)
                {
                    if (info_tp.Entity?.Name != null && IsVectorInsideBox(info_tp.AbsOrigin, trigger.Collision.Mins + trigger.CBodyComponent.SceneNode.AbsOrigin, trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin))
                    {
                        if (info_tp.CBodyComponent.SceneNode.AbsOrigin != null && info_tp.AbsRotation != null)
                        {
                            return (info_tp.CBodyComponent.SceneNode.AbsOrigin, info_tp.AbsRotation);
                        }
                    }
                }

                if (trigger.CBodyComponent.SceneNode.AbsOrigin != null)
                {
                    return (trigger.CBodyComponent.SceneNode.AbsOrigin, null);
                }
            }

            return (null, null);
        }

        private void FindStageTriggers()
        {
            stageTriggers.Clear();
            stageTriggerPoses.Clear();

            foreach (var trigger in entityCache.Triggers)
            {
                if (trigger == null || trigger.Entity.Name == null) continue;

                var (validStage, X) = IsValidStageTriggerName(trigger.Entity.Name.ToString());
                if (validStage)
                {
                    foreach (var info_tp in entityCache.InfoTeleportDestinations)
                    {
                        if (info_tp.Entity?.Name != null && IsVectorInsideBox(info_tp.AbsOrigin, trigger.Collision.Mins + trigger.CBodyComponent.SceneNode.AbsOrigin, trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin))
                        {
                            if (info_tp.CBodyComponent?.SceneNode?.AbsOrigin != null && info_tp.AbsRotation != null)
                            {
                                stageTriggerPoses[X] = info_tp.CBodyComponent.SceneNode.AbsOrigin;
                                stageTriggerAngs[X] = info_tp.AbsRotation;
                                SharpTimerDebug($"Added !stage {X} pos {stageTriggerPoses[X]} ang {stageTriggerAngs[X]}");
                            }
                        }
                    }

                    stageTriggers[trigger.Handle] = X;
                    SharpTimerDebug($"Added Stage {X} Trigger {trigger.Handle}");
                }
            }

            stageTriggerCount = stageTriggers.Any() ? stageTriggers.OrderByDescending(x => x.Value).First().Value : 0;

            if (stageTriggerCount == 1)
            {
                // If there's only one stage trigger, the map is linear
                stageTriggerCount = 0;
                useStageTriggers = false;
                stageTriggers.Clear();
                stageTriggerPoses.Clear();
                stageTriggerAngs.Clear();
                SharpTimerDebug($"Only one Stage Trigger found. Not enough. Cancelling...");
            }
            else if (stageTriggerCount > 1)
            {
                useStageTriggers = true;
            }

            SharpTimerDebug($"Found a max of {stageTriggerCount} Stage triggers");
            SharpTimerDebug($"Use stageTriggers is set to {useStageTriggers}");
        }

        private void FindCheckpointTriggers()
        {
            cpTriggers.Clear();

            foreach (var trigger in entityCache.Triggers)
            {
                if (trigger == null || trigger.Entity.Name == null) continue;

                var (validCp, X) = IsValidCheckpointTriggerName(trigger.Entity.Name.ToString());
                if (validCp)
                {
                    cpTriggers[trigger.Handle] = X;
                    SharpTimerDebug($"Added Checkpoint {X} Trigger {trigger.Handle}");
                }
            }

            cpTriggerCount = cpTriggers.Any() ? cpTriggers.OrderByDescending(x => x.Value).First().Value : 0;

            useCheckpointTriggers = cpTriggerCount != 0;

            SharpTimerDebug($"Found a max of {cpTriggerCount} Checkpoint triggers");
            SharpTimerDebug($"Use useCheckpointTriggers is set to {useCheckpointTriggers}");
        }

        private void FindBonusStartTriggerPos()
        {
            bonusRespawnPoses.Clear();
            bonusRespawnAngs.Clear();

            foreach (var trigger in entityCache.Triggers)
            {
                if (trigger == null || trigger.Entity.Name == null) continue;

                var (validStartBonus, bonusX) = IsValidStartBonusTriggerName(trigger.Entity.Name.ToString());
                if (validStartBonus)
                {
                    bool bonusPosAndAngSet = false;

                    foreach (var info_tp in entityCache.InfoTeleportDestinations)
                    {
                        if (info_tp.Entity?.Name != null && IsVectorInsideBox(info_tp.AbsOrigin, trigger.Collision.Mins + trigger.CBodyComponent.SceneNode.AbsOrigin, trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin))
                        {
                            if (info_tp.CBodyComponent?.SceneNode?.AbsOrigin != null && info_tp.AbsRotation != null)
                            {
                                bonusRespawnPoses[bonusX] = info_tp.CBodyComponent.SceneNode.AbsOrigin;
                                bonusRespawnAngs[bonusX] = info_tp.AbsRotation;
                                SharpTimerDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]} ang {bonusRespawnAngs[bonusX]}");
                                bonusPosAndAngSet = true;
                            }
                        }
                    }

                    if (!bonusPosAndAngSet && trigger.CBodyComponent?.SceneNode?.AbsOrigin != null)
                    {
                        bonusRespawnPoses[bonusX] = trigger.CBodyComponent.SceneNode.AbsOrigin;
                        SharpTimerDebug($"Added Bonus !rb {bonusX} pos {bonusRespawnPoses[bonusX]}");
                    }
                }
            }
        }

        private void FindTriggerPushData()
        {
            if (triggerPushFixEnabled)
            {
                triggerPushData.Clear();
                var trigger_pushers = entityCache.TriggerPushEntities;

                foreach (var trigger_push in trigger_pushers)
                {
                    if (trigger_push == null)
                    {
                        SharpTimerDebug("Trigger_push was null");
                        continue;
                    }

                    nint handle = trigger_push.Handle;

                    triggerPushData[handle] = new TriggerPushData(
                        trigger_push.PushSpeed,
                        trigger_push.PushEntitySpace,
                        trigger_push.PushDirEntitySpace
                    );

                    trigger_push.PushSpeed = 0.0f;

                    SharpTimerDebug($"Trigger_push {trigger_push.Entity.Name} Speed set to {trigger_push.PushSpeed}");
                }
            }
        }

        private (Vector?, Vector?, Vector?, Vector?) FindTriggerBounds()
        {
            Vector? startMins = null;
            Vector? startMaxs = null;

            Vector? endMins = null;
            Vector? endMaxs = null;

            foreach (var trigger in entityCache.Triggers)
            {
                if (trigger == null || trigger.Entity.Name == null)
                    continue;

                if (IsValidStartTriggerName(trigger.Entity.Name.ToString()))
                {
                    startMins = trigger.Collision.Mins + trigger.CBodyComponent.SceneNode.AbsOrigin;
                    startMaxs = trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin;
                    currentMapStartTriggerMaxs = startMaxs;
                    currentMapStartTriggerMins = startMins;
                    continue;
                }

                if (IsValidEndTriggerName(trigger.Entity.Name.ToString()))
                {
                    endMins = trigger.Collision.Mins + trigger.CBodyComponent.SceneNode.AbsOrigin;
                    endMaxs = trigger.Collision.Maxs + trigger.CBodyComponent.SceneNode.AbsOrigin;
                    continue;
                }
            }

            return (startMins, startMaxs, endMins, endMaxs);
        }

        public bool IsInsideTrigger(Vector triggerPos, float triggerCollisionRadius, Vector info_tpPos)
        {
            return info_tpPos.X >= triggerPos.X - triggerCollisionRadius && info_tpPos.X <= triggerPos.X + triggerCollisionRadius &&
                   info_tpPos.Y >= triggerPos.Y - triggerCollisionRadius && info_tpPos.Y <= triggerPos.Y + triggerCollisionRadius &&
                   info_tpPos.Z >= triggerPos.Z - triggerCollisionRadius && info_tpPos.Z <= triggerPos.Z + triggerCollisionRadius;
        }
    }
}