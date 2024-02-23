using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpTimer.src.Data
{
    public class PlayerReplays
    {
        public int CurrentPlaybackFrame { get; set; }
        public int BonusX { get; set; }
        public List<ReplayFrames> replayFrames { get; set; } = new List<ReplayFrames>();

        public class ReplayFrames
        {
            public string PositionString { get; set; }
            public string RotationString { get; set; }
            public string SpeedString { get; set; }
            public PlayerButtons? Buttons { get; set; }
            public uint Flags { get; set; }
            public MoveType_t MoveType { get; set; }
        }
    }
}
