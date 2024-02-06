/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai;

import ai.abstraction.UpdateableAbstractAction;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.core.ParameterSpecification;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import rts.GameState;
import rts.PlayerAction;
import rts.ScriptedActionGenerator;
import rts.UnitAction;
import rts.units.Unit;
import rts.units.UnitTypeTable;
import util.Pair;

/**
 *
 * @author Peter Guba
 */
public class RandomScriptedAI extends AI {
    /**
     * List of scripts that the ai can choose from.
     */
    private List<DependentUnitScript> scripts;
    
    /**
     * A map of assigned updateable abstract actions. The keys are unit ids.
     */
    Map<Long, UpdateableAbstractAction> abstractActions;  
    
    public RandomScriptedAI(UnitTypeTable utt) {
        scripts = new ArrayList<>();
        scripts.add(new NOKAV(new AStarPathFinding()));
        scripts.add(new Kiter(new AStarPathFinding(), 2));
        abstractActions = new HashMap<>();
    }
    
    public RandomScriptedAI(List<DependentUnitScript> scripts) {
        this.scripts = scripts;
        abstractActions = new HashMap<>();
    }
    
    private RandomScriptedAI(List<DependentUnitScript> scripts, Map<Long, UpdateableAbstractAction> abstractActions) {
        this.scripts = scripts;
        this.abstractActions = new HashMap<>(UpdateableAbstractAction.copyAll(abstractActions));
    }
    
    public void setAbstractActions(Map<Long, UpdateableAbstractAction> abstractActions) {
        this.abstractActions.putAll(UpdateableAbstractAction.copyAll(abstractActions));
    }
    
    @Override
    public void reset() {
        abstractActions = new HashMap<>();
    }

    @Override
    public AI clone() {
        return new RandomScriptedAI(scripts, abstractActions);
    }
   
    @Override
    public PlayerAction getAction(int player, GameState gs) {
        try {
            GameState gsClone = gs.clone();
            
            if (!gsClone.canExecuteAnyAction(player)) return new PlayerAction();
            PlayerAction initialAssignment = new PlayerAction();
        
            // Remove all the updateable abstract actions for which the
            // corresponding units have been destroyed.
            List<Long> keys = new ArrayList<>(abstractActions.keySet());
            for (Long id : keys) {
                if (gsClone.getUnit(id) == null) {
                    abstractActions.remove(id);
                }
            }

            // Assign actions based on the map of assigned updateable abstract
            // actions.
            for (Unit u : gsClone.getUnits()) {
                if (abstractActions.containsKey(u.getID()) && gsClone.getUnitAction(u) == null) {                  
                    abstractActions.get(u.getID()).update(gsClone);
                    
                    // If the updateable abstract action hasn't been completed yet,
                    // try to use it to pick an action for the given unit.
                    if (!abstractActions.get(u.getID()).completed(gsClone)) {
                        UnitAction ua = abstractActions.get(u.getID()).execute(gsClone);
                        if (ua == null) {
                            abstractActions.remove(u.getID());
                        }
                        else {
                            initialAssignment.addUnitAction(u, ua);
                        }
                    }
                    // Otherwise remove it from the map.
                    else {
                        abstractActions.remove(u.getID());
                    }
                }
            }
            
            // Issue the actions that were picked in the previous cycle.
            gsClone.issue(initialAssignment);

            // If there aren't anymore units with no actions assigned, return.
            if (!gsClone.canExecuteAnyAction(player)) {
                return getEquivalentPA(initialAssignment, gs);
            }

            // Compute the damage that has been assigned to enemy units thus far.
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
            
            // Get random updateable abstract actions from a script action generator.
            ScriptedActionGenerator sag = new ScriptedActionGenerator(gsClone, scripts, player, assignedDamage);
            PlayerAction result = sag.getRandom();
            abstractActions.putAll(sag.getCorrespondingUAAs(result));
            return getEquivalentPA(result.merge(initialAssignment), gs);
        }catch(Exception e) {
            // The only way the player action generator returns an exception is if there are no units that
            // can execute actions, in this case, just return an empty action:
            // However, this should never happen, since we are checking for this at the beginning      
            e.printStackTrace();
            return new PlayerAction();
        }
    }
    
    /**
     * Takes a player action and a game state and finds the equivalents of all units
     * specified in the player action in the given game state. Creates a player action
     * with these units and returns it. This is necessary because the same units in
     * different states are different objects.
     */
    public PlayerAction getEquivalentPA(PlayerAction original, GameState destination) {
        PlayerAction result = new PlayerAction();
        for (Pair<Unit, UnitAction> p : original.getActions()) {
            result.addUnitAction(destination.getUnit(p.m_a.getID()), p.m_b);
        }
        return result;
    }
    
    @Override
    public List<ParameterSpecification> getParameters()
    {
        // Not implemented.
        
        return new ArrayList<>();
    }
}
