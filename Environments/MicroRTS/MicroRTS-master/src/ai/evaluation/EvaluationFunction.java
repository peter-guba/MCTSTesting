/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.evaluation;

import rts.GameState;

/**
 * An abstract parent class for classes that encapsulate functions used to
 * evaluate game states.
 * @author santi
 */
public abstract class EvaluationFunction {    
    public static float VICTORY = 10000;
    
    public abstract float evaluate(int maxplayer, int minplayer, GameState gs);
    public abstract float upperBound(GameState gs);
    
    public String toString() {
        return getClass().getSimpleName();
    }
}
