
namespace SharpTimer.Data
{
    public class PlayerJumpStats
    {
        public int FramesOnGround { get; set; }
        public int LastFramesOnGround { get; set; }
        public bool OnGround { get; set; }
        public bool LastOnGround { get; set; }
        public string? LastPos { get; set; }
        public string? LastSpeed { get; set; }
        public string? LastEyeAngle { get; set; }
        public string? JumpPos { get; set; }
        public string? OldJumpPos { get; set; }
        public string? JumpSpeed { get; set; }
        public bool Jumped { get; set; }
        public int JumpedTick { get; set; }
        public string? LastJumpType { get; set; }
        public bool LastDucked { get; set; }
        public bool LandedFromSound { get; set; }
        public int WTicks { get; set; }
        public List<JumpFrames> jumpFrames { get; set; } = new List<JumpFrames>();

        public class JumpFrames
        {
            public string PositionString { get; set; }
            public string RotationString { get; set; }
            public string SpeedString { get; set; }
            public double MaxSpeed { get; set; }
            public double MaxHeight { get; set; }
            public bool LastLeft { get; set; }
            public bool LastRight { get; set; }
            public bool LastLeftRight { get; set; }
        }
    }
}
