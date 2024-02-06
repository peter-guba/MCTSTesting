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
import rts.units.UnitTypeTable;
import ai.selection.EpsilonGreedy;
import ai.selection.UCBSqrt;
import java.util.Arrays;
import java.util.List;
import ai.evaluation.EvaluationFunction;
import ai.selection.SelectionFunction;
import rts.GameState;

public class SimpleRegretMCTS extends BasicMCTS {
    public SimpleRegretMCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new BasicOneZero(),
             new UCBSqrt(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "simple_regret_mcts",
             "");
    }      
    
    public SimpleRegretMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new UCBSqrt(), scripts, name, bmrkID);
    }
    
    public SimpleRegretMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, double epsilon, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new EpsilonGreedy(epsilon), scripts, name, bmrkID);
    }
    
    private SimpleRegretMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, EvaluationFunction ef, SelectionFunction sf, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
    }
    
    @Override
    public AI clone() {
        return new SimpleRegretMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, ef, sf, scripts, name, bmrkID);
    }
    
    // Overridden so that the root node has the expandFirst parameter set to false if the epsilon-
    // -greedy policy is used.
    @Override
    public double monteCarloRun(int player, long cutOffTime) throws Exception {
        MCTSNode leaf = root.UCTSelectLeaf(player, 1-player, MAX_TREE_DEPTH, sf instanceof UCBSqrt);
        
        // Keep track of the maximum reached depth.
        if (leaf.depth > maxDepth) {
            maxDepth = leaf.depth;
        }        

        if (leaf!=null) {
            GameState gs2 = leaf.gs.clone();
            simulate(gs2, gs2.getTime() + MAXSIMULATIONTIME, leaf.getAssignedAbstractActions());
            double evaluation = ef.evaluate(player, 1-player, gs2);
            Backpropagate(leaf, evaluation);
            
            total_runs++;
            total_runs_this_move++;
            return evaluation;
        } else {
            // No actions to choose from.
            System.err.println(this.getClass().getSimpleName() + ": claims there are no more leafs to explore...");
            return 0;
        }
    }
    
    @Override
    public String toString()
    {
        if (sf instanceof EpsilonGreedy)
        {
            return "Simple Regret MCTS, metric: EpsilonGreedy, epsilon: " + ((EpsilonGreedy)sf).epsilon + ", playouts: " + ITERATIONS_BUDGET;
        }
        else
        {
            return "Simple Regret MCTS, metric: UCTSqrt, playouts: " + ITERATIONS_BUDGET;
        }
    }
}
