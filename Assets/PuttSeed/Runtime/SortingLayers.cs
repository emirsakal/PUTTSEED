namespace PuttSeed.Unity
{
    /// <summary>
    /// Draw order for everything above the course. The course meshes render at
    /// the default order 0, so anything meant to be SEEN over the felt must
    /// name a layer here — trails used to sit at -1 and -2, which drew them
    /// before the felt and hid them completely.
    /// </summary>
    public static class SortingLayers
    {
        /// <summary>Ghost trails: the faintest thing above the course.</summary>
        public const int GhostTrail = 1;

        /// <summary>Ghost balls, over their own trails.</summary>
        public const int GhostBall = 2;

        /// <summary>The player's trail and ball shadow.</summary>
        public const int BallTrail = 3;

        /// <summary>The player's ball, always over its trail and every ghost.</summary>
        public const int Ball = 4;
    }
}
