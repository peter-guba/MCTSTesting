using System;

namespace ChessTesting
{
    class SigmoidMCTS : BasicMCTS
    {
        /// <summary>
        /// A parameter of the sigmoid function.
        /// </summary>
        private float k;

        public SigmoidMCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, float k) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.k = k;
        }

        // Instead of returning 1, 0 or -1, this function returns the the difference
        // between the values of the remaining pieces of the two players run through
        // a sigmoid function.
        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);
            var eval = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());

            // Apply the sigmoid function and scale it to between -1 and 1.
            float result = (float)(1.0 / (1.0 + Math.Exp(-1.0 * k * eval))) * 2 - 1;
            return result;
        }

        public override string ToString()
        {
            return $"Sigmoid MCTS k: {k}, playouts: {maxNumOfPlayouts}";
        }
    }
}
