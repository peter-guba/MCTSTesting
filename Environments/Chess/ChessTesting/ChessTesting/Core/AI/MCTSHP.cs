namespace ChessTesting
{
    class MCTSHP : BasicMCTS
    {
        public MCTSHP(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        { }

        // The main change with respect to basic MCTS is this function.
        // Instead of just backpropagating the value obtained from evaluating
        // the final state of the playout, it is normalised at every node.
        protected override void Backpropagate(MCTSNode node, float val)
        {
            int colorIndex = node.State.WhiteToMove ? 0 : 1;
            evaluation.board = node.State;

            if (val > 0)
            {
                node.Value += val / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue);
            }
            else
            {
                node.Value += val / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue);
            }

            ++node.Visits;

            if (node.parent != null)
            {
                Backpropagate(node.parent, -val);
            }
        }

        // Instead of returning 1, 0 or -1, this function returns the difference between
        // the values of the remaining pieces of the two players.
        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);

            return evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());
        }

        public override string ToString()
        {
            return $"MCTS HP, playouts: {maxNumOfPlayouts}";
        }
    }
}
