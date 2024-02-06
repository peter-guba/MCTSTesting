/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;
import rts.GameState;
import rts.PlayerAction;
import rts.ScriptedActionGenerator;
import ai.selection.SelectionFunction;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import java.util.Map;
import java.util.HashMap;
import ai.abstraction.UpdateableAbstractAction;
import rts.units.Unit;
import rts.UnitAction;
import util.Pair;

public class MCTSNode {
    static Random r = new Random();

    /**
     * 0 : max, 1 : min, -1: Game-over
     */
    public int type;
    
    public MCTSNode parent;
    
    /**
     * The game state that corresponds to this node.
     */
    public GameState gs;
    
    /**
     * The depth of this node in the search tree.
     */
    int depth = 0;
    
    /**
     * Determines whether the node has actions that haven't been tried yet.
     */
    public boolean hasMoreActions = true;
    
    public ScriptedActionGenerator moveGenerator;
    
    /**
     * A list of actions that have been tried from this node. Each action
     * therefore has a corresponding child.
     */
    public List<PlayerAction> actions;
    
    public List<MCTSNode> children;
    
    /**
     * The maximum value that this node can have.
     */
    public float evaluation_bound = 0;
    
    /**
     * A sum of rewards from playouts that were started after passing through
     * this node in the search tree.
     */
    public float accum_evaluation = 0;
    
    /**
     * The number of times this node has been visited during the traversal
     * of the search tree.
     */
    public int visit_count = 0;
    
    /**
     * Sum of the squares of rewards from playouts that were started after
     * passing through this node in the search tree. Used only by UCBTunedMCTS.
     */
    public float squaredValueSum = 0;
    
    SelectionFunction sf;
    
    protected int lastMaxPlayer;
    
    protected int lastMinPlayer;
    
    /**
     * Updateable abstract actions that have already been assigned to units.
     */
    protected Map<Long, UpdateableAbstractAction> assignedAbstractActions;
    
    /**
     * Damage that has already been assigned to enemy units.
     */
    protected Map<Long, Integer> assignedDamage;
    
    /**
     * The number of times that all units can be assigned actions based on
     * the updateable abstract actions that have been assigned to them
     * before they are considered to be in deadlock.
     */
    protected final int assignmentCounterBound = 50;
    
    public MCTSNode(int maxplayer, int minplayer, GameState a_gs, MCTSNode a_parent, float bound, SelectionFunction sf, List<DependentUnitScript> scripts, Map<Long, Integer> assignedDamage, Map<Long, UpdateableAbstractAction> assignedAA) throws Exception {
        parent = a_parent;
        gs = a_gs;
        
        this.sf = sf;
        this.assignedDamage = assignedDamage;
        assignedAbstractActions = UpdateableAbstractAction.copyAll(assignedAA);        
        
        if (parent==null) depth = 0;
                     else depth = parent.depth+1;        
        evaluation_bound = bound;
        
        // Continue assigning actions to units based on the updateable abstract 
        // actions that have been assigned to them already. If all units get
        // actions assigned this way, roll the game state forward. If this
        // happens {@code }
        int assignmentCounter = 0;
        boolean abstractActionsAssigned = false;
        while(gs.winner()==-1 && !gs.gameover()) {
            // If neither player can execute an action, roll the game state forward.
            if (!gs.canExecuteAnyAction(maxplayer) && !gs.canExecuteAnyAction(minplayer)) {
                gs.cycle();
                abstractActionsAssigned = false;
            }
            // If abstract action were assigned a given number of times, it means that the units
            // are probably in a deadlock, so this deletes all the units, making the game over.
            else if (assignmentCounter == assignmentCounterBound) {
                gs.getUnitActions().clear();
                gs.getUnits().clear();
            }
            // If units haven't yet been assigned atomic actions based on updateable abstract
            // actions, try assigning them.
            else if (!abstractActionsAssigned) {
                PlayerAction initialActionAssignment = new PlayerAction();
        
                // Remove all the updateable abstract actions that were assigned
                // to units that no longer exist.
                List<Long> keys = new ArrayList<>(assignedAbstractActions.keySet());
                for (Long id : keys) {
                    if (gs.getUnit(id) == null) {
                        assignedAbstractActions.remove(id);
                    }
                }

                // Go through all units and try assigning actions to them.
                for (Unit u : gs.getUnits()) {
                    if (assignedAbstractActions.containsKey(u.getID()) && gs.getUnitAction(u) == null) {                  
                        assignedAbstractActions.get(u.getID()).update(gs);
                        
                        // If the given updateable abstract action hasn't been completed yet,
                        // try using it to assign an atomic action to a unit.
                        if (!assignedAbstractActions.get(u.getID()).completed(gs)) {
                            UnitAction ua = assignedAbstractActions.get(u.getID()).execute(gs);
                            if (ua == null) {
                                assignedAbstractActions.remove(u.getID());
                            }
                            else {
                                initialActionAssignment.addUnitAction(u, ua);
                            }
                        }
                        // Otherwise, remove it.
                        else {
                            assignedAbstractActions.remove(u.getID());
                        }
                    }
                }

                // Issue the assigned actions.
                gs.issue(initialActionAssignment);
                abstractActionsAssigned = true;
                ++assignmentCounter;
            }
            // If the units have been assigned actions based on updateable abstract
            // actions and there are still some units that have no actions assigned,
            // finish.
            else {
                break;
            }
        }
        
        // Determine the node type.
        if (gs.winner()!=-1 || gs.gameover()) {
            type = -1;
            return;
        }else if (gs.canExecuteAnyAction(maxplayer)) {
            type = 0;
            moveGenerator = new ScriptedActionGenerator(a_gs, scripts, maxplayer, this.assignedDamage);
            actions = new ArrayList<>();
            children = new ArrayList<>();
        } else if (gs.canExecuteAnyAction(minplayer)) {
            type = 1;
            moveGenerator = new ScriptedActionGenerator(a_gs, scripts, minplayer, this.assignedDamage);
            actions = new ArrayList<>();
            children = new ArrayList<>();
        } else {
            type = -1;
            System.err.println("RTMCTSNode: This should not have happened...");
        }
    }
    
    /**
     * Performs the selection and expansion steps of the MCTS algorithm.
     * @param expandFirst Determines whether expansion should be performed before
     * the selection function is called. It makes sense to set it to false
     * only when the selection function takes care of expansion as well,
     * as is the case with the epsilon-greedy selection function.
     */
    public MCTSNode UCTSelectLeaf(int maxplayer, int minplayer, int max_depth, boolean expandFirst) throws Exception {
        lastMaxPlayer = maxplayer;
        lastMinPlayer = minplayer;
        
        // Cut the tree policy at a predefined depth
        if (depth>=max_depth) return this;     
        
        if (expandFirst) {
            // If there are actions available in this node that haven't been
            // tried yet.
            if (hasMoreActions) {
                if (moveGenerator==null) {
                    return this;
                }                
                PlayerAction a = moveGenerator.getNextAction(0);
                
                // If a new action was returned by the move generator,
                // create a new child node and return it.
                if (a!=null) {
                    actions.add(a);
                    GameState gs2 = gs.cloneIssue(a);        
                    MCTSNode node = makeNewChild(gs2, a);
                    children.add(node);
                    return node;                
                } else {
                    hasMoreActions = false;
                }
            }
        }
        
        // Bandit policy:
        MCTSNode best = sf.getBestChild(this);
        
        if (best==null) {
            return this;
        }
        
        // If the best node is a newly created one (i.e. it has 0 visits), that means 
        // that it was created by the selection function, so we should return it.
        if (best.visit_count == 0) {
            return best;
        }
        
        return best.UCTSelectLeaf(maxplayer, minplayer, max_depth, true);
    }   
    
    /**
     * Creates a new child which corresponds to the given game state.
     * @param a The action that led to the game state.
     */
    public MCTSNode makeNewChild(GameState gs, PlayerAction a) throws Exception {
        // Get the updateable abstract actions that correspond to player action a,
        // compute the damage assigned to enemy units based on these actions and
        // pass that to the new child.
        Map<Long, Integer> newAssignedDamage = new HashMap<>();
        Map<Long, UpdateableAbstractAction> uaas = new HashMap<>();
        uaas.putAll(getAbstractActions(a));
        for (Long id : uaas.keySet()) {
            Pair<Unit, Integer> ad = ((UpdateableAbstractAction)uaas.get(id)).getAssignedDamage();
            newAssignedDamage.put(ad.m_a.getID(), ad.m_b);
        }
        newAssignedDamage.putAll(assignedDamage);        
        uaas.putAll(assignedAbstractActions);
        
        return new MCTSNode(lastMaxPlayer, lastMinPlayer, gs.clone(), this, evaluation_bound, sf, moveGenerator.getScripts(), newAssignedDamage, uaas);
    }
    
    /**
     * Fetches references to the updateable abstract actions from which the given
     * player action is derived. In order to adjust them, they must be cloned
     * first. 
     */
    public Map<Long, UpdateableAbstractAction> getAbstractActions(PlayerAction pa) {
        return moveGenerator.getCorrespondingUAAs(pa);
    }
    
    public Map<Long, UpdateableAbstractAction> getAssignedAbstractActions() {
        return assignedAbstractActions;
    }
    
    /**
     * Returns the number of possible actions this node can have.
     */
    public int getActionCount() {
        return moveGenerator.getActionCount();
    }
}
