/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/Classes/Class.java to edit this template
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;

/**
 * The same as UCB, except it doesn't scale the rewards to between 0 and 1.
 */
public class UCBwoScaling extends SelectionFunction {
    private float C = 1.0f;
    
    @Override
    public MCTSNode getBestChild(MCTSNode node) {
        double best_score = 0;
        MCTSNode best = null;
        for (MCTSNode child : node.children) {
            double exploitation = ((double)child.accum_evaluation) / child.visit_count;
            double exploration = Math.sqrt(Math.log((double)node.visit_count)/child.visit_count);
            
            double tmp = node.type == 0 ? (exploitation + exploration) : (exploitation - exploration);
            
            if (best==null ||
                (node.type == 0 && tmp>best_score) ||
                (node.type == 1 && tmp<best_score)) {
                best = child;
                best_score = tmp;
            }
        }
        
        return best;
    }
}
