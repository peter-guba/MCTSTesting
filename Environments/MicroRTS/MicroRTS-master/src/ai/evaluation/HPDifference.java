/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.evaluation;

import static ai.evaluation.SimpleEvaluationFunction.RESOURCE;
import static ai.evaluation.SimpleEvaluationFunction.RESOURCE_IN_WORKER;
import static ai.evaluation.SimpleEvaluationFunction.UNIT_BONUS_MULTIPLIER;
import rts.GameState;
import rts.PhysicalGameState;
import rts.units.Unit;

/**
 * Returns the difference between the sums of the remaining hit points of
 * the two players.
 */
public class HPDifference extends EvaluationFunction {
    @Override
    public float evaluate(int maxplayer, int minplayer, GameState gs) {
        PhysicalGameState pgs = gs.getPhysicalGameState();
        float score = 0;
        for(Unit u:pgs.getUnits()) {
            if (u.getPlayer()==maxplayer) {
                score += u.getHitPoints();
            }
            else {
                score -= u.getHitPoints();
            }
        }
        return score;
    }
    
    @Override
    public float upperBound(GameState gs) {
        return 1.f;
    }
}
