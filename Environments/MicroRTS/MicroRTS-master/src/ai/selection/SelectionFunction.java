/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.selection;

import ai.mcts.uct.MCTSNode;

/**
 * An abstract parent class for classes that encapsulate tree policies for
 * MCTS implementations.
 */
public abstract class SelectionFunction {
    public abstract MCTSNode getBestChild(MCTSNode node);
    
    public String toString() {
        return getClass().getSimpleName();
    }
}
