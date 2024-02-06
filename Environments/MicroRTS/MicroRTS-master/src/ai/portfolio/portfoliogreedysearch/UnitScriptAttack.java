/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.portfolio.portfoliogreedysearch;

import ai.abstraction.AbstractAction;
import ai.abstraction.Attack;
import ai.abstraction.pathfinding.PathFinding;
import rts.GameState;
import rts.UnitAction;
import rts.units.Unit;

/**
 *
 * @author santi
 */
public class UnitScriptAttack extends UnitScript {
    
    AbstractAction action;
    PathFinding pf;
    
    public UnitScriptAttack(PathFinding a_pf) {
        pf = a_pf;
    }
    
    public UnitAction getAction(Unit u, GameState gs) {
        if (action.completed(gs)) {
            return null;
        } else {
            return action.execute(gs);
        }
    }
    
    public UnitScript instantiate(Unit u, GameState gs) {
        Unit bestEnemy = bestEnemyUnit(u, gs);
        if (bestEnemy != null) {
            UnitScriptAttack script = new UnitScriptAttack(pf);
            script.action = new Attack(u, bestEnemy, pf);
            return script;
        } else {
            return null;
        }
    }
    
    public AbstractAction getAbstractAction() {
        return action;
    }
    
    
    public Unit bestEnemyUnit(Unit u, GameState gs) {
        Unit bestUnit = null;
        int bestRatio = 0;
        for (Unit u2 : gs.getUnits()) {
            if (u2.getPlayer()>=0 && u2.getPlayer() != u.getPlayer()) {
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
    
}
