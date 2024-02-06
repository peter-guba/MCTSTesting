/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.core.AI;
import ai.core.AIWithComputationBudget;
import ai.core.ParameterSpecification;
import ai.evaluation.EvaluationFunction;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;
import rts.GameState;
import rts.PlayerAction;
import rts.units.UnitTypeTable;
import ai.core.InterruptibleAI;
import ai.selection.SelectionFunction;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.abstraction.pathfinding.AStarPathFinding;
import java.util.Arrays;
import ai.evaluation.BasicOneZero;
import ai.selection.UCB;
import ai.abstraction.UpdateableAbstractAction;
import rts.units.Unit;
import java.util.Map;
import java.util.HashMap;
import rts.UnitAction;
import util.Pair;
import java.io.PrintStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.File;
import ai.RandomScriptedAI;
import ai.RandomAI;
import gui.PhysicalGameStateJFrame;
import gui.PhysicalGameStatePanel;


public class BasicMCTS extends AIWithComputationBudget implements InterruptibleAI {
    EvaluationFunction ef;
    SelectionFunction sf;
    RandomScriptedAI defaultPolicy = null;
       
    Random r = new Random();
    
    /**
     * The initial game state from which the algorithm starts its search.
     */
    GameState gs_to_start_from;
    
    public MCTSNode root;
    
    // statistics:
    public long total_runs = 0;
    public long total_cycles_executed = 0;
    public long total_actions_issued = 0;
    
    long total_runs_this_move = 0;
        
    int MAXSIMULATIONTIME = 1024;
    int MAX_TREE_DEPTH = 10;
    
    /**
     * The player whose actions the algorithm is supposed to compute.
     */
    int playerForThisComputation;
    
    /**
     * Tracks the number of performed playouts. Gets reset every time the
     * algorithm starts.
     */
    int nPlayouts = 0;
    
    /**
     * A portfolio of scripts that the algorithm uses to restrict the action space.
     */
    List<DependentUnitScript> scripts;
    
    /**
     * A map of already assigned updateable abstract actions. Carries over
     * from one run of the algorithm to the next.
     */
    Map<Long, UpdateableAbstractAction> abstractActions;
    
    /**
     * The initial assignment of actions as dictated by the already assigned
     * updateable abstract actions. Gets computed before the search starts.
     * Only units that don't get an action assigned this way are then taken
     * into account.
     */
    PlayerAction initialActionAssignment;
    
    /**
     * The name of the algorithm.
     */
    String name;
    
    /**
     * The id of the currently running benchmark.
     */
    String bmrkID;
    
    /**
     * The path to the folder where secondary metrics are supposed to be outputted.
     */
    final String path = "./time_depth_data/";
    
    /**
     * The maximum depth that a node in the tree has reached thus far.
     * Gets reset every time the algorithm starts.
     */
    int maxDepth = 0;
    
    /**
     * The name of the battle that is currently being performed.
     */
    String battleName;
    
    /**
     * A string that gets added to the ends of files that contain time and depth logs
     * of battles in order to create separate files for different battles. This is necessary
     * in order to compute the confidence bounds of round counts.
     */
    String randomBattleString;
    
    public BasicMCTS(UnitTypeTable utt) {
        this(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new BasicOneZero(),
             new UCB(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "basic_mcts",
             "");
    }      
    
    public BasicMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, String name, String bmrkID) {
        this(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new UCB(), scripts, name, bmrkID);
    }
    
    public BasicMCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, EvaluationFunction a_ef, SelectionFunction sf, List<DependentUnitScript> scripts, String name, String bmrkID) {
        super(available_time, max_playouts);
        MAXSIMULATIONTIME = lookahead;
        defaultPolicy = policy;
        MAX_TREE_DEPTH = max_depth;
        ef = a_ef;
        this.sf = sf;
        this.scripts = scripts;
        abstractActions = new HashMap<>();
        this.name = name;
        this.bmrkID = bmrkID;
        
        File f = new File(path + name);
        if (!f.exists()) {
            f.mkdirs();
        }
    }
    
    
    @Override
    public String statisticsString() {
        // Not implemented.        
        return "";
    }
    
    @Override
    public void printStats() {
        // Not implemented.
        System.out.println("Not implemented.");
    }
    
    
    @Override
    public void reset() {
        gs_to_start_from = null;
        root = null;
        total_runs_this_move = 0;
        maxDepth = 0;
        initialActionAssignment = null;
        abstractActions = new HashMap<>();
        nPlayouts = 0;
    }
    
    
    @Override
    public AI clone() {
        return new BasicMCTS(TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, ef, sf, scripts, name, bmrkID);
    }  
    
    
    @Override
    public PlayerAction getAction(int player, GameState gs) throws Exception
    {        
        if (gs.canExecuteAnyAction(player)) {
            // Initialize the algorithm.
            startNewComputation(player,gs.clone());
        
            // If assigning actions from abstractActions didn't cause the player to be
            // unable to perform more actions, run the MCTS algorithm.
            PlayerAction chosen = new PlayerAction();
            if (gs_to_start_from.canExecuteAnyAction(player)) {
                computeDuringOneGameFrame();
                chosen = getBestActionSoFar();
                getAbstractActions(chosen);      
            }
            
            // Combine the initial assignment with the actions selected by the algorithm.
            for (Pair<Unit, UnitAction> p : initialActionAssignment.getActions()) {
                chosen.addUnitAction(p.m_a, p.m_b);
            }
            
            return chosen;
        } else {
            return new PlayerAction();        
        }  
    }
    
    
    @Override
    public void startNewComputation(int a_player, GameState gs) throws Exception { 
        initialActionAssignment = new PlayerAction();
        
        // Remove the updateable abstract actions assigned to units that no
        // longer exist.
        List<Long> keys = new ArrayList<>(abstractActions.keySet());
        for (Long id : keys) {
            if (gs.getUnit(id) == null) {
                abstractActions.remove(id);
            }
        }
        
        // Try assigning an atomic action to every unit based on the already
        // assigned updateable abstract actions.
        for (Unit u : gs.getUnits()) {            
            if (abstractActions.containsKey(u.getID()) && gs.getUnitAction(u) == null) {                  
                abstractActions.get(u.getID()).update(gs);              
                
                // If the updateable abstract action hasn't been completed yet,
                // try to use it to assign an atomic action to a unit.
                if (!abstractActions.get(u.getID()).completed(gs)) {
                    UnitAction ua = abstractActions.get(u.getID()).execute(gs);
                    if (ua == null) {
                        abstractActions.remove(u.getID());                        
                    }
                    else {
                        initialActionAssignment.addUnitAction(u, ua);
                    }
                }
                // Otherwise remove it.
                else {
                    abstractActions.remove(u.getID());
                }
            }
        }
        
        // The game state passed to this method was cloned, so assigning
        // the actions like this is ok.
        gs.issue(initialActionAssignment);
        
        // If the player can't execute any more actions after the initial
        // assignment, return.
        if (!gs.canExecuteAnyAction(a_player)) {
            gs_to_start_from = gs;
            return;
        }
        
        float evaluation_bound = ef.upperBound(gs);
        playerForThisComputation = a_player;
        
        // Compute the damage that was assigned to each of the enemy units
        // during the initial assignment.
        Map<Long, Integer> assignedDamage = new HashMap<>();
        for (Long id : abstractActions.keySet()) {
            Pair<Unit, Integer> ad = ((UpdateableAbstractAction)abstractActions.get(id)).getAssignedDamage();
            if (assignedDamage.containsKey(ad.m_a.getID())) {
                assignedDamage.put(ad.m_a.getID(), assignedDamage.get(ad.m_a.getID()) + ad.m_b);
            }
            else {
                assignedDamage.put(ad.m_a.getID(), ad.m_b);
            }
        }
        
        root = new MCTSNode(playerForThisComputation, 1-playerForThisComputation, gs, null, evaluation_bound, sf, scripts, assignedDamage, abstractActions);
        gs_to_start_from = gs;
        total_runs_this_move = 0;
    }    
    
    
    public void resetSearch() {
        root = null;
        gs_to_start_from = null;
        total_runs_this_move = 0;
        maxDepth = 0;
        initialActionAssignment = null;
        abstractActions = new HashMap<>();
        nPlayouts = 0;
    }
    
    
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
    

    public double monteCarloRun(int player, long cutOffTime) throws Exception {
        MCTSNode leaf = root.UCTSelectLeaf(player, 1-player, MAX_TREE_DEPTH, true);
        
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
    
    /**
     * Backpropagates the result of a playout up the search tree.
     */
    protected void Backpropagate(MCTSNode node, double evaluation) {
        while(node!=null) {
            node.accum_evaluation += evaluation;
            node.visit_count++;
            node = node.parent;
        }
    }    
    
    @Override
    public PlayerAction getBestActionSoFar() {
        total_actions_issued++;
        
        if (root.children==null) {
            return new PlayerAction();
        }
                
        // Go through the children and find the one with the highest mean value.
        int bestIdx = -1;
        float bestScore = 0;
        MCTSNode best = null;
        for(int i = 0;i<root.children.size();i++) {
            MCTSNode child = root.children.get(i);
            float score = child.accum_evaluation / child.visit_count;
            if (best == null || bestScore < score) {
                best = child;
                bestIdx = i;
                bestScore = score;
            }
        }
        
        if (bestIdx==-1) return new PlayerAction();
        
        return root.actions.get(bestIdx);
    }
    
    
    /**
     * Stores the abstract actions that are the source of unit actions issued in this iteration
     * in the abstractActions variable. The actions aren't cloned as this method is used
     * after the main body of the algorithm has finished running.
     */
    public void getAbstractActions(PlayerAction chosen) {
        if (!chosen.isEmpty())
        {
            abstractActions.putAll(root.getAbstractActions(chosen));
        }
    }    
    
    /**
     * Performs a playout from the given node.
     */
    public void simulate(GameState gs, int time, Map<Long, UpdateableAbstractAction> aaas) throws Exception {
        boolean gameover = false;
        RandomScriptedAI randomPlayer1 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer1.setAbstractActions(aaas);
        RandomScriptedAI randomPlayer2 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer2.setAbstractActions(aaas);
        
        do{
            if (gs.isComplete()) {
                gameover = gs.cycle();
            } else {
                PlayerAction a1 = randomPlayer1.getAction(0, gs);
                PlayerAction a2 = randomPlayer2.getAction(1, gs);
                gs.issue(a1);
                gs.issue(a2);
            }
        }while(!gameover && gs.getTime()<time);
    }
    
    
    @Override 
    public String toString()
    {
        return "Basic MCTS, playouts: " + ITERATIONS_BUDGET;
    }
    
    
    @Override
    public List<ParameterSpecification> getParameters() {
        List<ParameterSpecification> parameters = new ArrayList<>();
        
        // Not implemented.
        
        return parameters;
    }      
    
    
    public int getPlayoutLookahead() {
        return MAXSIMULATIONTIME;
    }
    
    
    public void setPlayoutLookahead(int a_pola) {
        MAXSIMULATIONTIME = a_pola;
    }


    public int getMaxTreeDepth() {
        return MAX_TREE_DEPTH;
    }
    
    
    public void setMaxTreeDepth(int a_mtd) {
        MAX_TREE_DEPTH = a_mtd;
    }    
    
    public void setDefaultPolicy(RandomScriptedAI a_dp) {
        defaultPolicy = a_dp;
    }
    
    public void setEvaluationFunction(EvaluationFunction a_ef) {
        ef = a_ef;
    }

    protected void logTimeAndDepth(long time, PrintStream ps)
    {
        ps.println(nPlayouts + ", " + maxDepth + ", " + time);
    }
    
    public void setBattleName(String name) {
        battleName = name;
    }
    
    public void setRndBattleString(String str) {
        randomBattleString = str;
    }
}
