/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;

/**
 * Selects the best child by estimating the value of information that can
 * be obtained by selecting the child.
 */
public class VOIAware extends SelectionFunction {
    private UCB ucb = new UCB();
    
    @Override
    public MCTSNode getBestChild(MCTSNode node) {
        if (!node.hasMoreActions && node.children.size() == 1) {
            return node.children.get(0);
        }
        
        if (node.parent == null) {
            MCTSNode bestChild = null;
            MCTSNode secondBestChild = null;
            double secondBestScore = 0.0;
            double bestScore = 0.0;
            for (MCTSNode child : node.children)
            {
                double childScore = (1.0 + child.accum_evaluation / child.visit_count) / 2;
                if ((childScore > bestScore && node.type == 0) || (childScore < bestScore && node.type == 1) || bestChild == null)
                {
                    secondBestScore = bestScore;
                    bestScore = childScore;
                    secondBestChild = bestChild;
                    bestChild = child;
                }
                else if ((childScore > secondBestScore && node.type == 0) || (childScore < secondBestScore && node.type == 1) || secondBestChild == null)
                {
                    secondBestChild = child;
                    secondBestScore = childScore;
                }
            }

            MCTSNode childWithBestVOI = null;
            bestScore = 0.0;
            for (MCTSNode child : node.children)
            {
                double childScore;
                if (child == bestChild) {
                    childScore = GetVOIBest(bestChild, secondBestChild);
                }
                else
                {
                    childScore = GetVOIOther(bestChild, child);
                }

                if ((childScore > bestScore && node.type == 0) || (childScore < bestScore && node.type == 1) || childWithBestVOI == null)
                {
                    bestScore = childScore;
                    childWithBestVOI = child;
                }
            }

            return childWithBestVOI;
        }
        else {
            return ucb.getBestChild(node);
        }
    }
    
    /**
    * Computes an estimate of the VOI obtained by sampling the current best child node.
    */
    private double GetVOIBest(MCTSNode bestChild, MCTSNode secondBestChild)
    {
        double bestChildAvgVal = (1.0 + bestChild.accum_evaluation / bestChild.visit_count) / 2;
        double secondBestChildAvgVal = (1.0 + secondBestChild.accum_evaluation / secondBestChild.visit_count) / 2;
        return secondBestChildAvgVal / (bestChild.visit_count + 1) *
            Math.exp(-2.0 * Math.pow(bestChildAvgVal - secondBestChildAvgVal, 2.0) * bestChild.visit_count);
    }

    /**
    * Computes an estimate of the VOI obtained by a child node that isn't currently the best.
    */
    private double GetVOIOther(MCTSNode bestChild, MCTSNode otherChild)
    {
        double bestChildAvgVal = (1.0 + bestChild.accum_evaluation / bestChild.visit_count) / 2;
        double otherChildAvgVal = (1.0 + otherChild.accum_evaluation / otherChild.visit_count) / 2;
        return (1 - bestChildAvgVal) / (otherChild.visit_count + 1) *
            Math.exp(-2.0 * Math.pow(bestChildAvgVal - otherChildAvgVal, 2.0) * otherChild.visit_count);
    }
}