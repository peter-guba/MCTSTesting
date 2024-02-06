/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomAI;
import ai.RandomScriptedAI;
import ai.abstraction.UpdateableAbstractAction;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.HPDifference;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.selection.UCBwoScaling;
import ai.selection.SelectionFunction;
import ai.evaluation.EvaluationFunction;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.PrintStream;
import rts.GameState;
import rts.units.UnitTypeTable;
import java.util.List;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Map;
import rts.units.Unit;

/**
 *
 * @author Peter Guba
 */
public class WPMCTS extends BasicMCTS {
    /**
     * A list of scores of all the states encountered during the last playout.
     */
    private List<Float> intermediateResults = null;
    
    /**
     * The base used when computing an estimate of the value of information provided
     * by a state.
     */
    private double voiBase;
    
    /**
     * The base used when computing an estimate of the probability of encountering
     * a state.
     */
    private double poeBase;
    
    /**
     * Determines whether the score of the state from which a playout starts is
     * supposed to be subtracted from the scores of the states encountered during
     * a playout.
     */
    private boolean relative;
    
    private double upperBound;
    
    public WPMCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new HPDifference(),
             new UCBwoScaling(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "wp_mcts",
             "");
        
        voiBase = Math.E;
        poeBase = Math.E;
        relative = false;
    }  
    
    public WPMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, double vB, double pB, boolean normalize, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new HPDifference(), new UCBwoScaling(), scripts, name, bmrkID);
        voiBase = vB;
        poeBase = pB;
        this.relative = normalize;
    }
    
    private WPMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, double vB, double pB, boolean normalize, String name, String bmrkID, SelectionFunction sf, EvaluationFunction ef) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
        voiBase = vB;
        poeBase = pB;
        this.relative = normalize;
    }
    
    @Override
    public AI clone() {
        return new WPMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts, voiBase, poeBase, relative, name, bmrkID, sf, ef);
    }  
    
    // Overridden so that the upper bound on the score of a state is computed.
    @Override
    public void computeDuringOneGameFrame() throws Exception {   
        long start = System.currentTimeMillis();
        nPlayouts = 0;
        maxDepth = 0;
        long cutOffTime = start + TIME_BUDGET;
        if (TIME_BUDGET<=0) cutOffTime = 0;

        PrintStream ps = null;
        try {
            ps = new PrintStream(new FileOutputStream(path + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true));
        }       
        catch (FileNotFoundException e) {
            throw new Error("Can't log time and depth.");
        }
        
        // Compute the upper bound on the score of a state as the sum of hit points
        // of the team that has more hit points left at this point. (Because, if
        // that team were to destroy the other team without losing any hp,
        // this is the score it would end up with).
        int hp0 = 0;
        int hp1 = 0;
        for (Unit u : root.gs.getUnits()) {
            if (u.getPlayer() == 0) {
                hp0 += u.getHitPoints();
            }
            else {
                hp1 += u.getHitPoints();
            }
        }
        
        upperBound = Math.max(hp0, hp1);
        
        // Keep running iterations of the MCTS algorithm, until one of the
        // cut off conditions is met.
        while(true) {
            long roundStart = System.nanoTime();
            if (cutOffTime>0 && System.currentTimeMillis() > cutOffTime) break;
            nPlayouts++;
            if (ITERATIONS_BUDGET>0 && nPlayouts>ITERATIONS_BUDGET) break;
            monteCarloRun(playerForThisComputation, cutOffTime);
            logTimeAndDepth(System.nanoTime() - roundStart, ps);
            
            // If there is only one possible action at the root, there is no reason
            // to continue running the algorithm.
            if (root.children.size() == 1 && !root.hasMoreActions) {
                break;
            }
        }
        
        total_cycles_executed++;
    }
    
    // Overridden so that the reward from the playout is computed differently.
    @Override
    public double monteCarloRun(int player, long cutOffTime) throws Exception {
        MCTSNode leaf = root.UCTSelectLeaf(player, 1-player, MAX_TREE_DEPTH, true);
        if (leaf.depth > maxDepth) {
            maxDepth = leaf.depth;
        }
        
        if (leaf!=null) {
            GameState gs2 = leaf.gs.clone();
            simulate(gs2, gs2.getTime() + MAXSIMULATIONTIME, leaf.getAssignedAbstractActions(), player);
            double evaluation = sumUpIntermediateResults() / upperBound;
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
    
    // Re-implemented so that the algorithm stores scores of the states encountered
    // during the playout.
    public void simulate(GameState gs, int time, Map<Long, UpdateableAbstractAction> aaas, int maxPlayer) throws Exception {
        boolean gameover = false;
        float initialEval = ef.evaluate(maxPlayer, 1-maxPlayer, gs);
        intermediateResults = new ArrayList<>();
        RandomScriptedAI randomPlayer1 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer1.setAbstractActions(aaas);
        RandomScriptedAI randomPlayer2 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer2.setAbstractActions(aaas);
        
        do{
            if (gs.isComplete()) {
                gameover = gs.cycle();
            } else {
                gs.issue(defaultPolicy.getAction(0, gs));
                gs.issue(defaultPolicy.getAction(1, gs));
                
                if (relative) {
                    intermediateResults.add(ef.evaluate(maxPlayer, 1-maxPlayer, gs) - initialEval);
                }
                else {
                    intermediateResults.add(ef.evaluate(maxPlayer, 1-maxPlayer, gs));
                }
            }
        }while(!gameover && gs.getTime()<time);
    }
    
    private float sumUpIntermediateResults() {
        float result = 0;
        float sumOfWeights = 0;
        
        for (int i = 0; i < intermediateResults.size(); ++i) {
            float weight = (probabilityOfEncounter(i, intermediateResults.size()) + valueOfInformation(i, intermediateResults.size())) / 2;
            sumOfWeights += weight;
            result += intermediateResults.get(i) * weight;
        }
        
        return result / sumOfWeights;
    }
    
    /**
     * Estimates the importance of a state based on the probability of
     * it being encountered (the earlier it occurs, the higher).
     */
    private float probabilityOfEncounter(int index, int length) {
        return (float) Math.min(Math.pow(poeBase, -index), 10000);
    }
    
    /**
     * Estimates the importance of a state based on the value of the
     * information contained in it (the later it occurs, the higher).
     */
    private float valueOfInformation(int index, int length) {
        return (float) Math.min(Math.pow(voiBase, index - length), 10000);
    }
    
    @Override
    public String toString()
    {
        return "WP MCTS, voiBase: " + voiBase + ", poeBase: " + poeBase + ", normalized: " + relative + ", playouts: " + ITERATIONS_BUDGET;
    }
}
