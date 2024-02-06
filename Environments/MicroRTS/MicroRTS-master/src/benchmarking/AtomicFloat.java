/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

/**
 *
 * @author Peter Guba
 */

/**
 * A float wrapper that enables addition. Can be used in lists
 * that require mutable floats. 
 */
public class AtomicFloat {
    private float value;
    
    public AtomicFloat(float v) {
        value = v;
    }
    
    /**
     * Fetches the float value of this AtomicFloat.
     */
    public float get() {
        return value;
    }
    
    /**
     * Adds the given number to the float stored in this wrapper. 
     */
    public void add(float other) {
        value += other;
    }
    
    /**
     * Adds the value stored in another AtomicFloat to this one.
     */
    public void add(AtomicFloat other) {
        value += other.get();
    }
    
    @Override
    public String toString() {
        return "" + value;
    }
}
