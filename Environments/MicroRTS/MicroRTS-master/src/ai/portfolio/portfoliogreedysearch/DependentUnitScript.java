/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.portfolio.portfoliogreedysearch;

import ai.abstraction.UpdateableAbstractAction;
import rts.GameState;
import rts.units.Unit;
import java.util.List;
import java.util.Map;
import rts.UnitAction;
import util.Pair;

/**
 * An extension of UnitScript which is supposed to redefine the parameters
 * of the {@code instantiate} function.
 */
public abstract class DependentUnitScript extends UnitScript {
    /**
     * Creates an instance of the unit script for the given unit in the
     * given game state while taking the given assigned damage into account.
     * The assigned damage is necessary to avoid overkill (assigning more damage
     * to an enemy unit than they have hp, thereby wasting attacks).
     */
    public abstract UnitScript instantiate(Unit u, GameState gs, Map<Long, Integer> assignedDamage);
}
