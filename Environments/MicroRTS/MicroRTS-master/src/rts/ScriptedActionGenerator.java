/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package rts;

import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;
import java.util.Random;
import rts.units.Unit;
import util.Pair;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.portfolio.portfoliogreedysearch.UnitScript;
import java.util.Map;
import java.util.HashMap;
import ai.abstraction.UpdateableAbstractAction;
import ai.abstraction.UpdateableAttackOnce;
import ai.abstraction.HitAndRun;
import java.util.Collections;
import rts.UnitAction;
import java.util.stream.IntStream;
import java.util.stream.Collectors;

/**
 * Action generator that offers actions from a given portfolio of scripts.
 */
public class ScriptedActionGenerator {
    static Random r = new Random();
    
    /**
     * Portfolio of scripts that this generator can choose from.
     */
    List<DependentUnitScript> scripts;
    
    /**
     * The current game state.
     */
    GameState gameState;
    
    /**
     * Resources already taken up by some units.
     */
    ResourceUsage base_ru;
    
    /**
     * A map used to convert player actions returned by the generator to
     * maps of updateable abstract actions.
     */
    private Map<PlayerAction, Map<Long, UpdateableAbstractAction>> abstractActions;
    
    /**
     * The last returned player action.
     */
    PlayerAction lastAction;
    
    /**
     * The number of actions generated thus far.
     */
    long generated = 0;
    
    /**
     * Indicates whether the generator has more actions to generate.
     */
    boolean moreActions = true;
    
    /**
     * A list of units that can have actions assigned.
     */
    List<Unit> assignableUnits;
    
    /**
     * A list that holds the current choice of scripts for all the units
     * that can have actions assigned.
     */
    //List<Integer> scriptIndices;
    
    /**
     * Damage assigned to enemy units by this generator.
     */
    Map<Long, Integer> assignedDamage;
    
    /**
     * Damage already assigned to enemy units before this action generator
     * was instantiated.
     */
    Map<Long, Integer> alreadyAssignedDamage;
    
    /**
     * The player whose actions this generator is supposed to generate.
     */
    int player;
    
    /**
     * Every number in this list represents a configuration of scripts to be
     * assigned to units. Used to randomize the order of actions produced by
     * this generator.
     */
    List<Integer> choices;
    
    public long getGenerated() {
        return generated;
    }
    
    public PlayerAction getLastAction() {
        return lastAction;
    }
    
    public ScriptedActionGenerator(GameState a_gs, List<DependentUnitScript> scripts, int pID, Map<Long, Integer> alreadyAssignedDamage) throws Exception {
        this.scripts = scripts;
        this.alreadyAssignedDamage = new HashMap<Long, Integer>();
        
        for (Map.Entry<Long, Integer> kv : alreadyAssignedDamage.entrySet()) {
            this.alreadyAssignedDamage.put(kv.getKey(), kv.getValue());
        }
        
        // Generate the reserved resources:
        base_ru = new ResourceUsage();
        gameState = a_gs;
        PhysicalGameState pgs = gameState.getPhysicalGameState();
        
        for (Unit u : pgs.getUnits()) {
            UnitActionAssignment uaa = gameState.unitActions.get(u);
            if (uaa != null) {
                ResourceUsage ru = uaa.action.resourceUsage(u, pgs);
                base_ru.merge(ru);
            }
	}      
        
        player = pID;
        assignableUnits = new ArrayList<>();
        abstractActions = new HashMap<>();
        assignedDamage = new HashMap<>();
        
        // Initialize the assignableUnits, scriptIndices and assignedDamage variables.
	for (Unit u : pgs.getUnits()) {
            if (u.getPlayer() == pID) {
                if (gameState.unitActions.get(u) == null) {                                    
                    assignableUnits.add(u);
		}
            }
            else {
                assignedDamage.put(u.getID(), 0);
            }
	}
        
        // Create a list of integers with one number for every sequence of scripts
        // that this generator can generate, then shuffle it to ensure a random
        // ordering of actions.        
        choices = IntStream.rangeClosed(
                0, (int)Math.pow(scripts.size(), assignableUnits.size()) - 1
        ).boxed().collect(Collectors.toList());
	Collections.shuffle(choices);
        
        // If there are no units that can be assigned actions, something
        // went wrong.
	if (assignableUnits.size() == 0) {
            System.err.println("Problematic game state:");
            System.err.println(a_gs);
            throw new Exception(
		"Move generator for player " + pID + " created with no units that can execute actions! (status: "
		+ a_gs.canExecuteAnyAction(0) + ", " + a_gs.canExecuteAnyAction(1) + ")"
            );
	}
        
    }

    /**
     * Returns the next PlayerAction for the state stored in this object
     */
    public PlayerAction getNextAction(long cutOffTime) throws Exception {        
        // While there are more actions available, look for the next consistent
        // action assignment.
        while(moreActions) {
            GameState gsc = gameState.clone();
            clearAssignedDamage();
            boolean consistent = true;
            PlayerAction pa = new PlayerAction();
            pa.setResourceUsage(base_ru.clone());
            Map<Long, UpdateableAbstractAction> uaas = new HashMap<>();
            int i = assignableUnits.size();
            
            if (i == 0)
                throw new Exception("Move generator created with no units that can execute actions!");
            
            // Go through all the units.
            while (i > 0) {
                --i;
                Unit u = assignableUnits.get(i);
                UnitScript us = scripts.get(getScriptIndex(i)).instantiate(u, gsc, assignedDamage);
                UnitAction ua = null;
                boolean scriptActionFound = false;

                if (us != null) {
                    ua = us.getAction(u, gsc);
                    scriptActionFound = true;
                }

                // If the chosen unit script didn't generate a unit action,
                // assign the unit an action of type None.
                if (ua == null) {
                    scriptActionFound = false;
                    ua = new UnitAction(UnitAction.TYPE_NONE, 1);
                }
                // Otherwise add the damage assigned by the generated action to
                // the assignedDamage map.
                else {
                    Pair<Unit, Integer> newAssignedDamage = ((UpdateableAbstractAction)us.getAbstractAction()).getAssignedDamage();
                    if (newAssignedDamage != null) {
                        assignedDamage.put(newAssignedDamage.m_a.getID(), assignedDamage.get(newAssignedDamage.m_a.getID()) + newAssignedDamage.m_b);
                    }
                }

                ResourceUsage r2 = ua.resourceUsage(u, gsc.getPhysicalGameState());

                // Check for consistency.
                if (pa.getResourceUsage().consistentWith(r2, gameState)) {
                    pa.getResourceUsage().merge(r2);
                    pa.addUnitAction(u, ua);

                    PlayerAction auxiliaryAction = new PlayerAction();
                    auxiliaryAction.addUnitAction(u, ua);
                    gsc.issue(auxiliaryAction);

                    if (scriptActionFound) {
                        uaas.put(u.getID(), (UpdateableAbstractAction)us.getAbstractAction());
                    }
                }
                // If the assignment isn't consistent, break out of the cycle.
                else {
                    consistent = false;
                    break;
                }
            }
        
            moveCurrentChoice();

            // If the same player action based on the same updateable abstract
            // actions was already returned, skip it.
            if (checkForDuplicates(pa, uaas)) {
                continue;
            }
            
            // If a consistent assignment was found, return it.
            if (consistent) {
                lastAction = pa;
                generated++;
                abstractActions.put(pa, uaas);
                return pa;
            }
        }
        lastAction = null;
        return null;
    }
    
    /**
     * Returns a random player action for the game state in this object. The action
     * is still stored in the abstractActions variable, so using this method
     * probably shouldn't be mixed with using the getNextAction method.
     */
    public PlayerAction getRandom() {
	PlayerAction pa = new PlayerAction();
	pa.setResourceUsage(base_ru.clone());
        Map<Long, UpdateableAbstractAction> uaas = new HashMap<>();
        GameState gsc = gameState.clone();
        PhysicalGameState pgs = gsc.getPhysicalGameState();
        
        // Cycle through all the units and try to assign each of them a random action.
	for (Unit u : pgs.units) {
            if (u.getPlayer() == player && gsc.getUnitAction(u) == null) {
                List<DependentUnitScript> scriptsCopy = new ArrayList<>(scripts);
                Pair<Unit, Integer> newAssignedDamage = null;
                        
                // Keep trying assigning actions to the unit until a consistent
                // assignment has been found
                boolean consistent = false;
                do {
                    boolean scriptActionFound = false;
                    UnitScript us = null;
                    UnitAction ua = null;

                    // If all the scripts have been tried and no consistent assignment was
                    // found, assign an action of type None to the unit.
                    if (scriptsCopy.isEmpty()) {
                        ua = new UnitAction(UnitAction.TYPE_NONE, 1);
                    }
                    // Otherwise try a new script.
                    else {
                        us = scriptsCopy.remove(r.nextInt(scriptsCopy.size())).instantiate(u, gsc, assignedDamage);
                        if (us != null) {
                            ua = us.getAction(u, gsc);
                            scriptActionFound = true;
                        }
                        if (ua == null) {
                            scriptActionFound = false;
                            ua = new UnitAction(UnitAction.TYPE_NONE, 1);
                        }
                        else {
                            newAssignedDamage = ((UpdateableAbstractAction)us.getAbstractAction()).getAssignedDamage();
                        }
                    }

                    ResourceUsage r2 = ua.resourceUsage(u, pgs);

                    // Check for consistency.
                    if (pa.getResourceUsage().consistentWith(r2, gameState)) {
                        pa.getResourceUsage().merge(r2);
                        pa.addUnitAction(u, ua);
                        consistent = true;

                        PlayerAction auxiliaryAction = new PlayerAction();
                        auxiliaryAction.addUnitAction(u, ua);
                        gsc.issue(auxiliaryAction);
                    
                        if (scriptActionFound) {
                            uaas.put(u.getID(), (UpdateableAbstractAction)us.getAbstractAction());
                        }
                    }
                } while (!consistent);                
                
                if (newAssignedDamage != null) {
                    assignedDamage.put(newAssignedDamage.m_a.getID(), assignedDamage.get(newAssignedDamage.m_a.getID()) + newAssignedDamage.m_b);
                }
            }
        }
        
        // Stored in abstractActions so that the getCorrespondingUAAs method works properly.
        abstractActions.put(pa, uaas);
	return pa;
    }
    
    /**
     * Fetches the index of the script for a given unit based on the current choice
     * of scripts given by the last element in the choices variable.
     */
    private int getScriptIndex(int unitIndex) {
        int current = choices.get(choices.size() - 1);             
        return (current % (int)Math.pow(scripts.size(), unitIndex + 1)) / (int)Math.pow(scripts.size(), unitIndex);
    }
    
    /**
     * Moves the current choice of scripts to the next random element. 
     */
    private void moveCurrentChoice() {
        choices.remove(choices.size() - 1);
        if (choices.size() == 0) {
            moreActions = false;
        }
    }
    
    /**
     * Re-initializes the assignedDamage variable. Used when starting to
     * generate a new PlayerAction.
     */
    private void clearAssignedDamage() {
        for (Long id : assignedDamage.keySet()) {
            if (alreadyAssignedDamage.containsKey(id)) {
                assignedDamage.put(id, alreadyAssignedDamage.get(id));
            }
            else {
                assignedDamage.put(id, 0);
            }
        }
    }
    
    public String toString() {
        StringBuilder ret = new StringBuilder("ScriptedActionGenerator:\n");
        for(UnitScript s:scripts) {
            ret.append(s.toString()).append("\n");
        }
        ret.append("\nactions generated so far: ").append(generated);
        return ret.toString();
    }
    
    
    public List<DependentUnitScript> getScripts() {
        return scripts;
    }
    
    /**
     * Fetches references to the updateable abstract actions from which the given
     * player action is derived. They are passed by reference, so in order to
     * adjust them, they must be cloned first. 
     */
    public Map<Long, UpdateableAbstractAction> getCorrespondingUAAs(PlayerAction pa) {      
        return abstractActions.get(pa);
    }
    
    /**
     * Checks whether the given player action derived from the given sequence of
     * updateable abstract actions has already been generated.
     * true <=> duplicate found
     */
    private boolean checkForDuplicates(PlayerAction p1, Map<Long, UpdateableAbstractAction> u1) {        
        for(PlayerAction p2 : abstractActions.keySet()) {
            if (p1.equals(p2)) {
                for (Map.Entry<Long, UpdateableAbstractAction> kv : u1.entrySet()) {
                    if (!abstractActions.get(p2).containsKey(kv.getKey()) || !abstractActions.get(p2).get(kv.getKey()).equals(kv.getValue())) {
                        return false;
                    }
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    /**
     * Returns the number of possible actions this generator can generate.
     */
    public int getActionCount() {
        return (int)Math.pow(scripts.size(), assignableUnits.size());
    }
}
