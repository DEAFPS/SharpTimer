using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace SharpTimer.src.Data
{
    public class EntityCache
    {
        public List<CBaseTrigger> Triggers { get; private set; }
        public List<CInfoTeleportDestination> InfoTeleportDestinations { get; private set; }
        public List<CTriggerPush> TriggerPushEntities { get; private set; }
        public List<CPointEntity> InfoTargetEntities { get; private set; }

        public EntityCache()
        {
            Triggers = new List<CBaseTrigger>();
            InfoTeleportDestinations = new List<CInfoTeleportDestination>();
            TriggerPushEntities = new List<CTriggerPush>();
            InfoTargetEntities = new List<CPointEntity>();
            UpdateCache();
        }

        public void UpdateCache()
        {
            Triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple").ToList();
            InfoTeleportDestinations = Utilities.FindAllEntitiesByDesignerName<CInfoTeleportDestination>("info_teleport_destination").ToList();
            TriggerPushEntities = Utilities.FindAllEntitiesByDesignerName<CTriggerPush>("trigger_push").ToList();
        }
    }
}
