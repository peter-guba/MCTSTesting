/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.portfolio.portfoliogreedysearch;

import ai.abstraction.AbstractAction;
import ai.abstraction.UpdateableAttackOnce;
import ai.abstraction.HitAndRun;
import ai.abstraction.pathfinding.PathFinding;
import java.util.Map;
import rts.GameState;
import rts.UnitAction;
import rts.units.Unit;

/**
 * An implementation of the Kiter script.
 */
public class Kiter extends DependentUnitScript {
    /**
     * The abstract action that corresponds to the script.
     */
    AbstractAction action;
    
    PathFinding pf;
    
    /**
     * The distance by which a unit is supposed to retreat after
     * attacking.
     */
    int retreatDistance;
    
    public Kiter(PathFinding a_pf, int rD) {
        pf = a_pf;
        retreatDistance = rD;
    }
    
    @Override
    public UnitAction getAction(Unit u, GameState gs) {
        if (action.completed(gs)) {
            return null;
        } else {
            return action.execute(gs);
        }
    }
    
    @Override
    public UnitScript instantiate(Unit u, GameState gs, Map<Long, Integer> assignedDamage) {
        Unit bestEnemy = bestEnemyUnit(u, gs, assignedDamage);
        if (bestEnemy != null) {
            Kiter script = new Kiter(pf, retreatDistance);
            script.action = new HitAndRun(u, new UpdateableAttackOnce(u, bestEnemy, pf), retreatDistance);
            return script;
        } else {
            return null;
        }
    }
    
    /**
     * This operation isn't supported as this script cannot be instantiated
     * without including the damage already assigned to units.
     */
    @Override
    public UnitScript instantiate(Unit u, GameState gs) {
        throw new UnsupportedOperationException();
    }
    
    /**
     * Picks the best enemy unit to attack based on their damage per tick
     * to hp ratio. It omits units that have already been assigned a lethal
     * amount of damage. If no enemy is in range, it chooses the closest one.
     */
    private Unit bestEnemyUnit(Unit u, GameState gs, Map<Long, Integer> assignedDamage) {
        Unit bestUnit = null;
        int bestRatio = 0;
        for (Unit u2 : gs.getUnits()) {
            if (u2.getPlayer()>=0 && u2.getPlayer() != u.getPlayer() && assignedDamage.get(u2.getID()) < u2.getHitPoints()) {
                int d = Math.abs(u2.getX() - u.getX()) + Math.abs(u2.getY() - u.getY());
                if (d < u.getAttackRange()) {
                    int ratio = (u2.getMaxDamage() / u2.getAttackTime()) / u2.getHitPoints();
                    if (bestUnit == null || ratio > bestRatio) {
                        bestUnit = u2;
                        bestRatio = ratio;
                    }
                }
            }
        }
        
        // If no unit is in range, attack the closest one.
        int closestDistance = 0;
        if (bestUnit == null) {
            for (Unit u2 : gs.getUnits()) {
                if (u2.getPlayer()>=0 && u2.getPlayer() != u.getPlayer()) {
                    int d = Math.abs(u2.getX() - u.getX()) + Math.abs(u2.getY() - u.getY());
                    if (bestUnit == null || d < closestDistance) {
                        bestUnit = u2;
                        closestDistance = d;
                    }
                }
            }
        }
        
        return bestUnit;
    }
    
    @Override
    public AbstractAction getAbstractAction() {
        return action;
    }
    
    @Override
    public String toString() {
        return "K";
    }
}
