/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;
import java.util.Random;
import rts.GameState;
import rts.PlayerAction;

/**
 * Picks the best child with probability epsilon and a random different
 * child with probability 1 - epsilon.
 */
public class EpsilonGreedy extends SelectionFunction {
    public final double epsilon;
    private UCB ucb = new UCB();
    public static Random r = new Random();
    
    public EpsilonGreedy(double epsilon) {
        this.epsilon = epsilon;
    }
    
    @Override
    public MCTSNode getBestChild(MCTSNode node) {        
        MCTSNode bestChild = null;

        try {
            if (node.parent == null)
            {
                if (node.children.size() == 0) 
                {
                    if (!node.hasMoreActions) 
                    {
                        return node;
                    }
                }
                
                double bestScore = 0.0;
                int bestChildIndex = -1;
                for (int i = 0; i < node.children.size(); ++i)
                {
                    MCTSNode child = node.children.get(i);
                    
                    if (child.visit_count != 0) {
                        double score = child.accum_evaluation / child.visit_count;

                        if (bestChild == null ||
                            (score > bestScore && node.type == 0) ||
                            (score < bestScore && node.type == 1))
                        {
                            bestScore = score;
                            bestChildIndex = i;
                            bestChild = child;
                        }
                    }
                }

                if (bestChild != null && r.nextDouble() < epsilon)
                {
                    return bestChild;
                }
                else
                {
                    int index = r.nextInt(node.getActionCount() - 1);

                    if (index < bestChildIndex)
                    {
                        return node.children.get(index);
                    }
                    else if (index >= bestChildIndex && index < node.children.size() - 1) {
                        return node.children.get(index + 1);
                    }
                    else
                    {
                        return expandNode(node);
                    }
                }
            }
            else
            {
                return ucb.getBestChild(node);
            }
        }
        catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }
    
    private MCTSNode expandNode(MCTSNode node) throws Exception {
        if (node.moveGenerator==null) 
        {
            return node;
        }
        PlayerAction a = node.moveGenerator.getNextAction(0);
        if (a!=null) 
        {
            node.actions.add(a);
            GameState gs2 = node.gs.cloneIssue(a);                
            MCTSNode newNode = node.makeNewChild(gs2, a);
            node.children.add(newNode);
            return newNode;                
        } else {
            node.hasMoreActions = false;
            return node;
        }
    }
}
