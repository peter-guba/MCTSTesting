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
import ai.selection.VOIAware;
import ai.selection.SelectionFunction;
import ai.evaluation.EvaluationFunction;
import java.util.Arrays;
import rts.units.UnitTypeTable;
import java.util.List;

public class VOIAwareMCTS extends BasicMCTS {
    public VOIAwareMCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new BasicOneZero(),
             new VOIAware(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "voi_mcts",
             "");
    }  
    
    public VOIAwareMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new VOIAware(), scripts, name, bmrkID);
    }
    
    private VOIAwareMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID, SelectionFunction sf, EvaluationFunction ef) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
    }
    
    @Override
    public AI clone() {
        return new VOIAwareMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts, name, bmrkID, sf, ef);
    }
    
    @Override
    public String toString()
    {
        return "VOI MCTS, playouts: " + ITERATIONS_BUDGET;
    }
}
