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

import java.util.List;
import rts.PhysicalGameState;
import java.util.Map;
import java.util.concurrent.atomic.AtomicInteger;
import rts.units.UnitTypeTable;

public class BattleSettings {
    /**
    * {@code GameEnvironment} in which the battle takes place.
    */
    public final PhysicalGameState gameState;
    
    /**
    * How many times should be the battle repeated.
    */
    public final int repeats;
    
    /**
    * Name of the battle.
    */
    public final String name;
    
    /**
    * Numbers of units of given type for player 1 and player 2.
    */
    public final List<Map<String, AtomicInteger>> unitCounts;
    
    public final UnitTypeTable utt;

    /**
    * Creates a battle with given settings.
    */
    public BattleSettings(PhysicalGameState gameState, int repeats, String name, List<Map<String, AtomicInteger>> unitCounts, UnitTypeTable utt)
    {
        this.gameState = gameState;
        this.repeats = repeats;
        this.name = name;
        this.unitCounts = unitCounts;
        this.utt = utt;
    }
}