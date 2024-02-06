/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.evaluation;

import rts.GameState;

/**
 * Returns the difference between the sums of the remaining hit points of
 * the two players run through a sigmoid function.
 */
public class Sigmoid extends EvaluationFunction {
    HPDifference hpdiff;
    float k;
    
    public Sigmoid(float k) {
        this.k = k;
        hpdiff = new HPDifference();
    }
    
    @Override
    public float evaluate(int maxplayer, int minplayer, GameState gs) {
        return sigmoidFunction(hpdiff.evaluate(maxplayer, minplayer, gs));
    }
    
    @Override
    public float upperBound(GameState gs) {
        return 1.f;
    }
    
    private float sigmoidFunction(float x) {
        return (float) (1.0 / (1.0 + Math.exp(-1.0 * k * x)) * 2 - 1);
    }
}
