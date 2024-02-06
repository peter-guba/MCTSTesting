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
 * Represents a sequence of steps during which a unit moves to some specified
 * location.
 */
public class UpdateableMove extends UpdateableAbstractAction {
    
    /**
     * The coordinates that the unit is supposed to move to.
     */
    int x,y;
    
    PathFinding pf;
    
    public UpdateableMove(Unit u, int a_x, int a_y, PathFinding a_pf) {
        super(u);
        x = a_x;
        y = a_y;
        pf = a_pf;
    }
    
    public UpdateableAbstractAction clone() {
        return new UpdateableMove(unit, x, y, pf);
    }
    
    public boolean completed(GameState gs) {
        return unit.getX() == x && unit.getY() == y;
    }
    
    @Override
    public boolean equals(Object o)
    {
        if (!(o instanceof UpdateableMove)) return false;
        UpdateableMove a = (UpdateableMove)o;
        return unit.getID() == a.unit.getID() && x == a.x && y == a.y && pf.getClass() == a.pf.getClass();
    }

    
    public void toxml(XMLWriter w)
    {
        w.tagWithAttributes("Move","unitID=\""+unit.getID()+"\" x=\""+x+"\" y=\""+y+"\" pathfinding=\""+pf.getClass().getSimpleName()+"\"");
        w.tag("/Move");
    }       

    public UnitAction execute(GameState gs, ResourceUsage ru) {
        PhysicalGameState pgs = gs.getPhysicalGameState();
        UnitAction move = pf.findPath(unit, x+y*pgs.getWidth(), gs, ru);
        if (move!=null && gs.isUnitActionAllowed(unit, move)) return move;
        return null;
    } 
    
    @Override
    public void update(GameState currentGs) {
        // Updates the unit to which the abstract action is assigned.
        // This is necessary as the 'same' units in two different
        // states are in fact different objects.
        unit = currentGs.getUnit(unit.getID());
    }
    
    @Override
    public Pair<Unit, Integer> getAssignedDamage() {
        return null;
    }
}
