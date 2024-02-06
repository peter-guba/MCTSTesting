/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomScriptedAI;
import ai.abstraction.UpdateableAbstractAction;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.HPDifference;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.selection.UCB;
import ai.selection.SelectionFunction;
import ai.evaluation.EvaluationFunction;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.PrintStream;
import java.util.Arrays;
import java.util.HashMap;
import rts.PhysicalGameState;
import rts.units.Unit;
import rts.units.UnitTypeTable;
import java.util.List;
import java.util.Map;
import rts.GameState;
import rts.PlayerAction;
import rts.UnitAction;
import util.Pair;


public class MCTSHP extends BasicMCTS {
    public MCTSHP(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new HPDifference(),
             new UCB(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "mcts_hp",
             "");
    }  
    
    public MCTSHP(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new HPDifference(), new UCB(), scripts, name, bmrkID);
    }
    
    private MCTSHP(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID, SelectionFunction sf, EvaluationFunction ef) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
    }
    
    @Override
    public AI clone() {
        return new MCTSHP(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts, name, bmrkID, sf, ef);
    }
    
    // The main change with respect to basic MCTS is this function.
    // Instead of just backpropagating the value obtained from evaluating
    // the final state of the playout, it is normalised at every node.
    @Override
    protected void Backpropagate(MCTSNode leaf, double evaluation) {
        while(leaf!=null) {
            float hpSum = 0;
            int player = evaluation > 0 ? playerForThisComputation : 1 - playerForThisComputation;
            
            PhysicalGameState pgs = leaf.gs.getPhysicalGameState();
            for(Unit u:pgs.getUnits()) {
                if (u.getPlayer()==player) {
                    hpSum += u.getHitPoints();
                }
            }            
            
            leaf.accum_evaluation += evaluation / hpSum;
            
            leaf.visit_count++;
            leaf = leaf.parent;
        }
    }
    
    @Override
    public String toString()
    {
        return "MCTS HP, playouts: " + ITERATIONS_BUDGET;
    }
}
