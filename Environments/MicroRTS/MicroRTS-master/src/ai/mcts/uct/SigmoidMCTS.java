/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomScriptedAI;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.Sigmoid;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import rts.units.UnitTypeTable;
import ai.selection.UCB;
import java.util.Arrays;
import java.util.List;
import ai.evaluation.EvaluationFunction;
import ai.selection.SelectionFunction;

public class SigmoidMCTS extends BasicMCTS {
    /**
     * The parameter passed to the sigmoid function.
     */
    float constK;
    
    public SigmoidMCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new Sigmoid(1.0f),
             new UCB(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "sigmoid_mcts",
             "");
        constK = 1.0f;
    }  
    
    public SigmoidMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, float k, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new Sigmoid(k), new UCB(), scripts, name, bmrkID);
        constK = k;
    }
    
    private SigmoidMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, EvaluationFunction ef, SelectionFunction sf, List<DependentUnitScript> scripts, String name, String bmrkID, float k) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
        constK = k;
    }
    
    @Override
    public AI clone() {
        return new SigmoidMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, ef, sf, scripts, name, bmrkID, constK);
    }
    
    @Override
    public String toString()
    {
        return "Sigmoid MCTS k: " + constK + ", playouts: " + ITERATIONS_BUDGET;
    }
}
