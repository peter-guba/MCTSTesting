/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;

/**
 * Like UCB, but with a tighter bound on the uncertainty of observations.
 */
public class UCBTuned extends SelectionFunction {
    
    @Override
    public MCTSNode getBestChild(MCTSNode node) {
        double best_score = 0;
        MCTSNode best = null;
        for (MCTSNode child : node.children) {
            double exploitation = ((double)child.accum_evaluation) / child.visit_count;
          
            if (node.type == 0) {
                exploitation = (node.evaluation_bound + exploitation) / (2 * node.evaluation_bound);
            }
            else {
                exploitation = (node.evaluation_bound - exploitation) / (2 * node.evaluation_bound);
            }
            
            double v = child.squaredValueSum / child.visit_count - exploitation * exploitation + Math.sqrt(2 * Math.log(node.visit_count) / child.visit_count);
            double exploration = Math.sqrt((Math.log(node.visit_count) / child.visit_count) * Math.min(0.25, v));
            
            double tmp = exploitation + exploration;
            
            if (best==null || tmp>best_score) {
                best = child;
                best_score = tmp;
            }
        }
        
        return best;
    }
}
