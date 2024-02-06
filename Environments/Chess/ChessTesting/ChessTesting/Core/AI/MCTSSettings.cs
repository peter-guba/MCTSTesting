namespace ChessTesting
{
    public class MCTSSettings : AISettings
    {
        /// <summary>
        /// The maximum number of playouts that the algorithm can perform.
        /// </summary>
        public int maxNumOfPlayouts;

        /// <summary>
        /// The maximum depth of a playout.
        /// </summary>
        public int playoutDepthLimit;
    }
}
