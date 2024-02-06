/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;

/**
 * Computes the UCB scores of all child nodes and selects the one with the
 * best score.
 */
public class UCB extends SelectionFunction {
    private float C = 1.0f;
    
    @Override
    public MCTSNode getBestChild(MCTSNode node) {
        double best_score = 0;
        MCTSNode best = null;
        for (MCTSNode child : node.children) {
            double exploitation = ((double)child.accum_evaluation) / child.visit_count;
            double exploration = Math.sqrt(Math.log((double)node.visit_count)/child.visit_count);
            
            if (node.type == 0) {
                exploitation = (node.evaluation_bound + exploitation) / (2 * node.evaluation_bound);
            }
            else {
                exploitation = (node.evaluation_bound - exploitation) / (2 * node.evaluation_bound);
            }

            double tmp = exploitation + C*exploration;
            
            if (best==null || tmp>best_score) {
                best = child;
                best_score = tmp;
            }
        }
        
        return best;
    }
}
