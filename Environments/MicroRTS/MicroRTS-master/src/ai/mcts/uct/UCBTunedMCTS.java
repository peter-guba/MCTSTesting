/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomScriptedAI;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.BasicOneZero;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.selection.UCBTuned;
import java.util.Arrays;
import rts.units.UnitTypeTable;
import java.util.List;

public class UCBTunedMCTS extends BasicMCTS {
    public UCBTunedMCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new BasicOneZero(),
             new UCBTuned(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "ucb_tuned_mcts",
             "");
    }  
    
    public UCBTunedMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new UCBTuned(), scripts, name, bmrkID);
    }
    
    @Override
    public AI clone() {
        return new UCBTunedMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts, name, bmrkID);
    }  
    
    
    // Re-implemented so that squaredValueSum gets updated while backpropagating.
    @Override
    protected void Backpropagate(MCTSNode leaf, double evaluation) {
        while(leaf!=null) {
            leaf.accum_evaluation += evaluation;
            // Scale the value to between 0 and 1 before squaring it.
            if (leaf.parent != null) {
                leaf.squaredValueSum += leaf.parent.type == 0 ? (leaf.evaluation_bound + evaluation) * (leaf.evaluation_bound + evaluation) / 4 : (leaf.evaluation_bound - evaluation) * (leaf.evaluation_bound - evaluation) / 4;
            }
            leaf.visit_count++;
            leaf = leaf.parent;
        }
    }
    
    @Override
    public String toString()
    {
        return "UCB1-Tuned MCTS, playouts: " + ITERATIONS_BUDGET;
    }
}
