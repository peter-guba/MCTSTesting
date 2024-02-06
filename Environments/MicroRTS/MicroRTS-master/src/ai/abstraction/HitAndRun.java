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
import util.XMLWriter;
import util.Pair;

/**
 * Represents a sequence of steps during which a unit moves towards another
 * unit, attacks it once and then flees. It is basically a combination of the
 * UpdateableAttackOnce and UpdateableMove actions.
 */
public class HitAndRun extends UpdateableAbstractAction {
    UpdateableAttackOnce a1;
    
    UpdateableMove a2;
    
    /**
     * The distance that the unit is supposed to retreat after attacking.
     */
    int retreatDistance;
    
    public HitAndRun(Unit u, UpdateableAttackOnce a1, int retreatDistance) {
        super(u);
        this.a1 = a1;
        a2 = null;
        this.retreatDistance = retreatDistance;
    }
    
    private HitAndRun(Unit u, UpdateableAttackOnce a1, UpdateableMove a2, int retreatDistance) {
        super(u);
        this.a1 = a1;
        this.a2 = a2;
        this.retreatDistance = retreatDistance;
    }
    
    public UpdateableAbstractAction clone() {
        if (a2 == null) {
            return new HitAndRun(unit, (UpdateableAttackOnce)a1.clone(), retreatDistance);
        }
        else {
            return new HitAndRun(unit, (UpdateableAttackOnce)a1.clone(), (UpdateableMove)a2.clone(), retreatDistance);
        }
    }
    
    @Override
    public boolean completed(GameState gs) {
        // If the attack once abstract action was completed.
        if (a1.completed(gs)) {       
            // If the target no longer exists, the abstract action is completed.
            if (a1.target == null) {
                return true;
            }
            
            // If the move abstract action hasn't been instantiated yet,
            //  instantiate it and return false.
            if (a2 == null) {       
                int dirX = unit.getX() - a1.target.getX();
                int dirY = unit.getY() - a1.target.getY();
                
                a2 = new UpdateableMove(
                        unit,
                        clamp(unit.getX() + dirX * retreatDistance, gs.getPhysicalGameState().getHeight()),
                        clamp(unit.getY() + dirY * retreatDistance, gs.getPhysicalGameState().getWidth()),
                        a1.pf
                );
                
                return false;
            }
            
            // Check if the move abstract action has been completed.
            return a2.completed(gs);
        }
        
        return false;
    }
    
    @Override
    public boolean equals(Object o)
    {
        if (!(o instanceof HitAndRun)) return false;
        HitAndRun a = (HitAndRun)o;
        return a1.equals(a.a2) && a1.equals(a.a2);
    }

    
    public void toxml(XMLWriter w)
    {
        w.tag("HitAndRun");
        a1.toxml(w);
        w.tag("/HitAndRun");
    }       

    public UnitAction execute(GameState gs, ResourceUsage ru) {
        // Execute the attack once abstract action until it is completed.
        if (!a1.completed(gs)) {
            return a1.execute(gs);
        }
        // The execute the move abstract action.
        else {            
            return a2.execute(gs);
        }
    }
    
    /**
     * Clams the given value to between 0 and {@code max}.
     */
    private int clamp(int originalValue, int max) {
        return Math.min(Math.max(0, originalValue), max - 1);
    } 
    
    @Override
    public void update(GameState currentGs) {
        // Updates the unit to which the abstract action is assigned.
        // This is necessary as the 'same' units in two different
        // states are in fact different objects.
        unit = currentGs.getUnit(unit.getID());
        
        a1.update(currentGs);
        if (a2 != null) {
            a2.update(currentGs);
        }
    }
    
    @Override
    public Pair<Unit, Integer> getAssignedDamage() {
        return a1.getAssignedDamage();
    }
}
