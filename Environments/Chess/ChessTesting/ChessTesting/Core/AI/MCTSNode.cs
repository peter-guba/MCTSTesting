using System.Collections.Generic;
using System;

namespace ChessTesting
{
    public class MCTSNode
    {
        /// <summary>
        /// The board configuration that corresponds to this node.
        /// </summary>
        public Board State { get; protected set; }

        /// <summary>
        /// The value accumulated in this node during the playouts that passed through it.
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Sum of squares of observed rewards, used by UCBTunedMCTS.
        /// </summary>
        public float SquaredValueSum { get; set; }

        /// <summary>
        /// The number of times this node has been visited.
        /// </summary>
        public float Visits { get; set; }

        /// <summary>
        /// Determines whether all the actions that are possible in this node have been tried.
        /// </summary>
        public bool FullyExpanded { get; protected set; }

        /// <summary>
        /// The action that led to the state corresponding to this node from the state corresponding
        /// to its parent.
        /// </summary>
        public Move Action { get; private set; }

        public readonly MCTSNode parent;

        /// <summary>
        /// The team whose turn it is in the state corresponding to this node.
        /// true <=> white
        /// </summary>
        public readonly bool team;

        public readonly int depth;

        public List<MCTSNode> Children { get; protected set; }

        /// <summary>
        /// A random number generator used when generating moves during playouts and picking
        /// moves during expansion.
        /// </summary>
        protected static System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// The actions that can be performed in the current state by the player whose turn it is.
        /// </summary>
        public List<Move> PossibleActions { get; private set; }

        public readonly bool isTerminal;

        /// <summary>
        /// An object that provides functions used to evaluate a game.
        /// </summary>
        private Evaluation eval;

        private MoveGenerator mg;

        public List<int> SortedChildren { get; protected set; }

        public MCTSNode(Board state, bool team, Move action, MoveGenerator mg, Evaluation eval, MCTSNode parent, int depth)
        {
            this.State = state;
            this.team = team;
            Action = action;
            this.parent = parent;
            this.eval = eval;
            this.mg = mg;
            this.depth = depth;

            PossibleActions = mg.GenerateMoves(state);

            if (PossibleActions.Count == 0)
            {
                isTerminal = true;
                FullyExpanded = true;
            }
            Children = new List<MCTSNode>();

            SortedChildren = new List<int>();
        }

        /// <summary>
        /// Picks an action from the possible actions that haven't yet been tried and creates a child.
        /// </summary>
        public MCTSNode Expand()
        {
            int index = rand.Next(0, PossibleActions.Count);

            Board newState = State.Clone();
            newState.MakeMove(PossibleActions[index]);

            MCTSNode newNode = new MCTSNode(newState, !team, PossibleActions[index], mg, eval, this, depth + 1);
            Children.Add(newNode);

            PossibleActions.RemoveAt(index);
            if (PossibleActions.Count == 0)
            {
                FullyExpanded = true;
            }

            SortedChildren.Add(Children.Count - 1);

            return newNode;
        }

        /// <summary>
        /// Picks an action with the specified index from the possible actions that haven't yet been tried and creates a child.
        /// </summary>
        public MCTSNode Expand(int index)
        {
            Board newState = State.Clone();
            newState.MakeMove(PossibleActions[index]);

            MCTSNode newNode = new MCTSNode(newState, !team, PossibleActions[index], mg, eval, this, depth + 1);
            Children.Add(newNode);

            PossibleActions.RemoveAt(index);
            if (PossibleActions.Count == 0)
            {
                FullyExpanded = true;
            }

            SortedChildren.Add(Children.Count - 1);

            return newNode;
        }

        /// <summary>
        /// Picks the move that leads to the child with the best mean value.
        /// </summary>
        public Move GetBestMove()
        {
            MCTSNode bestChild = null;
            float bestScore = 0.0f;

            foreach (MCTSNode child in Children)
            {
                float score = (child.Value / child.Visits);
                if (bestChild == null || score > bestScore)
                {
                    bestChild = child;
                    bestScore = score;
                }
            }

            return bestChild.Action;
        }

        /// <summary>
        /// Updates the sortedChildren variable in the parent node. 
        /// </summary>
        public void SortInParent()
        {
            // Since this node is the only one being updated, only it needs to be moved around.
            int thisIndexInChildren = parent.Children.IndexOf(this);
            int thisIndexInSorted = parent.SortedChildren.IndexOf(thisIndexInChildren);

            bool shiftedLeft = false;

            // Check if the node should be shifted to the left.
            MCTSNode leftNeighbour = thisIndexInSorted == 0 ? null : parent.Children[parent.SortedChildren[thisIndexInSorted - 1]];
            while (leftNeighbour != null && leftNeighbour.Value / leftNeighbour.Visits < Value / Visits)
            {
                int aux = parent.SortedChildren[thisIndexInSorted - 1];
                parent.SortedChildren[thisIndexInSorted - 1] = parent.SortedChildren[thisIndexInSorted];
                parent.SortedChildren[thisIndexInSorted] = aux;
                --thisIndexInSorted;

                leftNeighbour = thisIndexInSorted == 0 ? null : parent.Children[parent.SortedChildren[thisIndexInSorted - 1]];

                shiftedLeft = true;
            }

            if (shiftedLeft)
            {
                return;
            }

            // Check if the node should be shifted to the right.
            MCTSNode rightNeighbour = thisIndexInSorted == parent.SortedChildren.Count - 1 ? null : parent.Children[parent.SortedChildren[thisIndexInSorted + 1]];
            while (rightNeighbour != null && rightNeighbour.Value / rightNeighbour.Visits > Value / Visits)
            {
                int aux = parent.SortedChildren[thisIndexInSorted + 1];
                parent.SortedChildren[thisIndexInSorted + 1] = parent.SortedChildren[thisIndexInSorted];
                parent.SortedChildren[thisIndexInSorted] = aux;
                ++thisIndexInSorted;

                rightNeighbour = thisIndexInSorted == parent.SortedChildren.Count - 1 ? null : parent.Children[parent.SortedChildren[thisIndexInSorted + 1]];
            }
        }

        /// <summary>
        /// Determines whether the node is in the top n children of its parent.
        /// </summary>
        /// <param name="n"> The number of nodes to take into account. </param>
        public bool InTopN(int n)
        {
            int thisIndexInChildren = parent.Children.IndexOf(this);
            int thisIndexInSorted = parent.SortedChildren.IndexOf(thisIndexInChildren);

            if (thisIndexInSorted < n)
            {
                return true;
            }
            // If the nodes index when sorted wasn't within the first n nodes, it's still possible that it is
            // as good as the last of the top n nodes, so this needs to be taken into account.
            else
            {
                int shift = 0;
                MCTSNode leftNeighbour = this;
                while ((leftNeighbour.Value / leftNeighbour.Visits) == (Value / Visits) && thisIndexInSorted - shift >= n)
                {
                    ++shift;
                    leftNeighbour = parent.Children[parent.SortedChildren[(thisIndexInSorted - shift)]];
                }

                return (thisIndexInSorted - shift) < n;
            }
        }
    }
}
