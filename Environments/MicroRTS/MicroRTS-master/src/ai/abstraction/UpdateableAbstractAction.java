/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.abstraction;

/**
 * An extension of the AbstractAction class that allows for updating of the
 * abstract action based on a given game state. This is necessary for the proper
 * functioning of scripts which use actions that last more than one tick.
 */

import rts.units.Unit;
import rts.GameState;
import util.Pair;
import java.util.Map;
import java.util.HashMap;
import rts.PlayerAction;
import rts.UnitAction;

public abstract class UpdateableAbstractAction extends AbstractAction {
    public UpdateableAbstractAction(Unit u) {
        super(u);
    }
    
    /**
     * Updates the action based on the given game state. This is necessary
     * as the 'same' units in two different game states are different objects.
     */
    public abstract void update(GameState gs);
    
    /**
     * A function that is only necessary for abstract actions that deal damage,
     * but had to be included here so that other code could access it without
     * trying to recast all updateable abstract actions first.
     */
    public abstract Pair<Unit, Integer> getAssignedDamage();
    
    public abstract UpdateableAbstractAction clone();
    
    /**
     * Clones all the updateable abstract actions in the given map.
     */
    public static Map<Long, UpdateableAbstractAction> copyAll(Map<Long, UpdateableAbstractAction> original) {
        Map<Long, UpdateableAbstractAction> copy = new HashMap<>();
        for (Long id : original.keySet()) {
            copy.put(id, original.get(id).clone());
        }
        return copy;
    }
    
    @Override
    public abstract boolean equals(Object o);
}
