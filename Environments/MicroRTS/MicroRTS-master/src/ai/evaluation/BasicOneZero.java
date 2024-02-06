/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.evaluation;

import rts.GameState;
import rts.PhysicalGameState;
import rts.units.Unit;

/**
 * Returns 1, 0 or -1, based on whether the state was a win, draw or loss for
 * the maxplayer.
 */
public class BasicOneZero extends EvaluationFunction {
    @Override
    public float evaluate(int maxplayer, int minplayer, GameState gs) {
        PhysicalGameState pgs = gs.getPhysicalGameState();
        boolean maxUnitsPresent = false;
        boolean minUnitsPresent = false;
        
        for(Unit u:pgs.getUnits()) {
            if (u.getPlayer() == maxplayer) {
                maxUnitsPresent = true;      
            }
            else {
                minUnitsPresent = true;
            }
            
            if (maxUnitsPresent && minUnitsPresent) {
                return 0;
            }
        }
        
        if (maxUnitsPresent) {
            return 1;
        }
        
        return -1;
    }
    
    @Override
    public float upperBound(GameState gs) {
        return 1.0f;
    }
}
