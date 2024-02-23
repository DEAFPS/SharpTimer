using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpTimer.src.Data
{
    public class TriggerPushData
    {
        public float PushSpeed { get; set; }
        public QAngle PushEntitySpace { get; set; }
        public Vector PushDirEntitySpace { get; set; }
        public Vector PushMins { get; set; }
        public Vector PushMaxs { get; set; }
        public TriggerPushData(float pushSpeed, QAngle pushEntitySpace, Vector pushDirEntitySpace, Vector pushMins, Vector pushMaxs)
        {
            PushSpeed = pushSpeed;
            PushEntitySpace = pushEntitySpace;
            PushDirEntitySpace = pushDirEntitySpace;
            PushMins = pushMins;
            PushMaxs = pushMaxs;
        }
    }
}
