/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

import java.util.Arrays;
import java.util.List;
import java.util.ArrayList;
import java.util.concurrent.atomic.AtomicInteger;

public class Results {
    /**
    * Number of wins for player 1, player 2, and number of draws.
    */
    public List<AtomicInteger> WinCounts = Arrays.asList(new AtomicInteger(0), new AtomicInteger(0), new AtomicInteger(0));
    
    /**
    * Number of sym-wins for player 1, player 2, and number of sym draws.
    */
    public List<AtomicInteger> SymWinCounts = Arrays.asList(new AtomicInteger(0), new AtomicInteger(0), new AtomicInteger(0));
    
    /**
    * Sum of hull remaining for all units of player 1 and player 2.
    */
    public List<AtomicFloat> HullRemaining = Arrays.asList(new AtomicFloat(0.f), new AtomicFloat(0.f), new AtomicFloat(0.f));
    
    /**
    * Number of rounds for respective battles in order.
    */
    public List<Integer> RoundCounts = new ArrayList<>();
    
    /**
    * How long did rounds take for player 1 and player 2 in fractional milliseconds.
    */
    public List<List<Long>> RoundTimes = Arrays.asList(new ArrayList<>(), new ArrayList<>());
    
    /**
    * The number of unfinished games.
    */
    public int Unfinished = 0;

    /**
    * Add {@code other} result to this result maintaining all necessary statistics.
    */
    public void add(Results other)
    {        
        assert WinCounts.size() == other.WinCounts.size();
        assert SymWinCounts.size() == other.SymWinCounts.size();
        assert WinCounts.size() == SymWinCounts.size();
        for (int i = 0; i < WinCounts.size(); i++)
        {
            WinCounts.get(i).getAndAdd(other.WinCounts.get(i).intValue());
            SymWinCounts.get(i).getAndAdd(other.SymWinCounts.get(i).intValue());
        }

        assert HullRemaining.size() == other.HullRemaining.size();
        assert RoundTimes.size() == other.RoundTimes.size();
        for (int i = 0; i < HullRemaining.size(); i++)
        {
            HullRemaining.get(i).add(other.HullRemaining.get(i));
            RoundTimes.get(0).addAll(other.RoundTimes.get(0));
            RoundTimes.get(1).addAll(other.RoundTimes.get(1));
        }

        RoundCounts.addAll(other.RoundCounts);
        Unfinished += other.Unfinished;
    }
}