/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.abstraction;

import ai.abstraction.pathfinding.PathFinding;
import rts.GameState;
import rts.PhysicalGameState;
import rts.ResourceUsage;
import rts.UnitAction;
import rts.units.Unit;
import util.Pair;
import util.XMLWriter;

/**
 * Represents a sequence of steps during which a unit moves towards another
 * unit and attacks it once.
 */
public class UpdateableAttackOnce extends UpdateableAbstractAction {
    public Unit target;
    
    public PathFinding pf;
    
    /**
     * A variable used to keep track of the target's hp.
     */
    int targetHP;
    
    /**
     * Determines whether the action has finished executing.
     */
    boolean finished = false;
    
    /**
     * The time at which the last atomic action was assigned.
     */
    int timeOfAssignment = 0;
    
    /**
     * The last atomic action that was assigned in the course of completing this
     * abstract action.
     */
    UnitAction lastAssignedAction = null;
    
    public UpdateableAttackOnce(Unit u, Unit a_target, PathFinding a_pf) {
        super(u);
        target = a_target;
        pf = a_pf;
        targetHP = target.getHitPoints();
    }
    
    /**
     * A private constructor used only for cloning.
     */
    private UpdateableAttackOnce(Unit u, Unit a_target, PathFinding a_pf, boolean finished, int time, UnitAction last) {
        super(u);
        target = a_target;
        pf = a_pf;
        targetHP = target.getHitPoints();
        this.finished = finished;
        timeOfAssignment = time;
        lastAssignedAction = last;
    }
    
    public UpdateableAbstractAction clone() {
        return new UpdateableAttackOnce(unit, target, pf, finished, timeOfAssignment, lastAssignedAction);
    }
    
    @Override
    public boolean completed(GameState gs) {
        if (finished) {
            return true;
        }
        
        // If the target unit has been destroyed, the action is completed.
        if (target == null) {
            return true;
        }
        
        // Checks if the target has been successfully attacked by looking at
        // whether the last atomic action was an attack action and whether it
        // was completed. If so the whole abstract action was completed.
        if (lastAssignedAction != null &&
            lastAssignedAction.getType() == UnitAction.TYPE_ATTACK_LOCATION &&
            lastAssignedAction.ETA(unit) + timeOfAssignment <= gs.getTime() &&
            target.getHitPoints() < targetHP) {
            finished = true;
            return true;
        }
        
        return false;
    }
    
    @Override
    public boolean equals(Object o)
    {
        if (!(o instanceof UpdateableAttackOnce)) return false;
        UpdateableAttackOnce a = (UpdateableAttackOnce)o;
        return unit.getID() == a.unit.getID() && target.getID() == a.target.getID() && pf.getClass() == a.pf.getClass();
    }

    
    public void toxml(XMLWriter w)
    {
        w.tagWithAttributes("Attack","unitID=\""+unit.getID()+"\" target=\""+target.getID()+"\" pathfinding=\""+pf.getClass().getSimpleName()+"\"");
        w.tag("/Attack");
    }
    

    @Override
    public UnitAction execute(GameState gs, ResourceUsage ru) {        
        timeOfAssignment = gs.getTime();
            
        int dx = target.getX()-unit.getX();
        int dy = target.getY()-unit.getY();
        
        // The distance to the target.
        double d = Math.sqrt(dx*dx+dy*dy);
        
        // If the target is in attack range.
        if (d <= unit.getAttackRange()) {
            targetHP = target.getHitPoints();
            lastAssignedAction = new UnitAction(UnitAction.TYPE_ATTACK_LOCATION,target.getX(),target.getY());
            return lastAssignedAction;
        }
        // Otherwise move towards the target.
        else {
            UnitAction move = pf.findPathToPositionInRange(unit, target.getX()+target.getY()*gs.getPhysicalGameState().getWidth(), unit.getAttackRange(), gs, ru);
            if (move!=null && gs.isUnitActionAllowed(unit, move)) {
                lastAssignedAction = move;
                return move;
            }
            lastAssignedAction = null;
            return null;
        }        
    }  
    
    @Override
    public void update(GameState currentGs) {
        // Updates the unit to which the abstract action is assigned and the
        // target unit. This is necessary as the 'same' units in two different
        // states are in fact different objects.
        unit = currentGs.getUnit(unit.getID());
        target = currentGs.getUnit(target.getID());
        
        // Updates the target's hp.
        if (target != null) {
            int dist = Math.abs(unit.getX() - target.getX()) + Math.abs(unit.getY() - target.getY());
            if (dist > unit.getAttackRange()) {
                targetHP = target.getHitPoints();
            }
        }
    }
    
    @Override
    public Pair<Unit, Integer> getAssignedDamage() {
        return new Pair<>(target, unit.getMinDamage());
    }
}
