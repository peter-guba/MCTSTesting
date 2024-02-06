/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomAI;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.HPDifference;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.selection.UCB;
import ai.selection.SelectionFunction;
import ai.evaluation.EvaluationFunction;
import java.util.Arrays;
import rts.GameState;
import rts.units.UnitTypeTable;
import java.util.List;
import ai.RandomScriptedAI;
import rts.PhysicalGameState;
import rts.units.Unit;


public class FAP_HP_MCTS extends BasicMCTS {
    /**
    * The number of segments into which the playouts are supposed to be separated.
    */
    private int numOfSegments;

    /**
    * Determines whether the segmentation is supposed to be exponential or linear.
    */
    private boolean exponentialSegmentation;

    /**
    * Determines whether the multiplicative factor should be computed in an exponential
    * or linear fashion.
    */
    private boolean exponentialMultiplication;
    
    public FAP_HP_MCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new HPDifference(),
             new UCB(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "fap_hp_mcts",
             "");
        
        numOfSegments = 10;
        exponentialSegmentation = true;
        exponentialMultiplication = true;
    }  
    
    public FAP_HP_MCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, boolean eS, boolean eM, int nS, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new HPDifference(), new UCB(), scripts, name, bmrkID);
        
        numOfSegments = nS;
        exponentialSegmentation = eS;
        exponentialMultiplication = eM;
    }
    
    private FAP_HP_MCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, boolean eS, boolean eM, int nS, String name, String bmrkID, SelectionFunction sf, EvaluationFunction ef) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
        
        numOfSegments = nS;
        exponentialSegmentation = eS;
        exponentialMultiplication = eM;
    }
    
    @Override
    public AI clone() {
        return new FAP_HP_MCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts, exponentialSegmentation, exponentialMultiplication, numOfSegments, name, bmrkID, sf, ef);
    }
    
    @Override
    public double monteCarloRun(int player, long cutOffTime) throws Exception {
        MCTSNode leaf = root.UCTSelectLeaf(player, 1-player, MAX_TREE_DEPTH, true);
        if (leaf.depth > maxDepth) {
            maxDepth = leaf.depth;
        }    

        if (leaf!=null) {
            GameState gs2 = leaf.gs.clone();
            simulate(gs2, gs2.getTime() + MAXSIMULATIONTIME, leaf.getAssignedAbstractActions());

            // Compute the multiplicative factor which will multiply the result
            // of the evaluation function.
            int segmentNumber = GetSegmentNumber(nPlayouts);
            double multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = Math.pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }
            
            double evaluation = (float) multiplicativeFactor * ef.evaluate(player, 1-player, gs2);

            Backpropagate(leaf, evaluation, (int)multiplicativeFactor);
            
            total_runs++;
            total_runs_this_move++;
            return evaluation;
        } else {
            // no actions to choose from :)
            System.err.println(this.getClass().getSimpleName() + ": claims there are no more leafs to explore...");
            return 0;
        }
    }
    
    /**
    * Computes the number of the segment into which the n-th playout falls.
    */
    private int GetSegmentNumber(int n)
    {
        if (exponentialSegmentation)
        {
            double aux = Math.pow(2, numOfSegments) - 1;
            int result = 1;

            for (int i = 1; i <= numOfSegments; ++i)
            {
                double bound = ITERATIONS_BUDGET * (Math.pow(2, i) / aux);
                if (n < bound)
                {
                    break;
                }
                else
                {
                    ++result;
                }
            }

            return result;
        }
        else
        {
            return ((n - 1) / (ITERATIONS_BUDGET / numOfSegments)) + 1;
        }
    }

    protected void Backpropagate(MCTSNode node, double evaluation, int visitCount)
    {
        while(node!=null) {
            float hpSum = 0;
            int player = evaluation > 0 ? playerForThisComputation : 1 - playerForThisComputation;
            
            PhysicalGameState pgs = node.gs.getPhysicalGameState();
            for(Unit u:pgs.getUnits()) {
                if (u.getPlayer()==player) {
                    hpSum += u.getHitPoints();
                }
            }            
            
            node.accum_evaluation += evaluation / hpSum;
            
            node.visit_count += visitCount;
            node = node.parent;
        }
    }
    
    @Override
    public String toString()
    {
        String segmentation = exponentialSegmentation ? "exp" : "lin";
        String multiplication = exponentialMultiplication ? "exp" : "lin";

        return "FAP HP MCTS, segments: " + numOfSegments + ", segmentation: " + segmentation + ", multiplication: " + multiplication + ", playouts: " + ITERATIONS_BUDGET;
    }
}