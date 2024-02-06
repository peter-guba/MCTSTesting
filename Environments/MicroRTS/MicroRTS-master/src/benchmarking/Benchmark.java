/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

import ai.RandomBiasedAI;
import ai.RandomScriptedAI;
import ai.abstraction.pathfinding.AStarPathFinding;
import java.util.List;
import java.util.ArrayList;
import ai.core.AI;
import gui.PhysicalGameStateJFrame;
import gui.PhysicalGameStatePanel;
import java.io.FileWriter;
import java.io.PrintStream;
import java.util.LinkedList;
import java.util.zip.ZipOutputStream;
import rts.GameState;
import rts.PartiallyObservableGameState;
import rts.PhysicalGameState;
import rts.PlayerAction;
import rts.Trace;
import rts.TraceEntry;
import static tests.Experimenter.GC_EACH_FRAME;
import util.XMLWriter;
import java.nio.file.Paths;
import java.io.File;
import rts.units.Unit;
import ai.mcts.uct.BasicMCTS;
import ai.mcts.uct.MCTSHP;
import ai.mcts.uct.SigmoidMCTS;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import java.util.Random;
import ai.mcts.uct.UCT;
import ai.evaluation.*;
import ai.mcts.uct.*;
import ai.selection.*;
import ai.RandomAI;

/**
 *
 * @author Peter Guba
 */

/**
 * Represents a single benchmark from "Resources/Benchmarks"
 */
public class Benchmark
{
    private static Random r = new Random();
    
    private final List<BattleSettings> _battles;
    private final List<AI> _players;
    private final List<String> _playerNames;
    private final int _roundMax;
    private final boolean _isSymmetric;
    private final String _name;

    /**
    * Creates a benchmark with given parameters.    
    * @param battles Battles to be run in this benchmark.
    * @param players Players which battle in this benchmark.
    * @param roundMax Maximum number of rounds for each battle.
    * @param isSymmetric Should the players be switched and play again?
    * @param name File name of the benchmark.
    * @param playerNames Names of the players.
    * */
    public Benchmark(List<BattleSettings> battles, List<AI> players, int roundMax, boolean isSymmetric, String name, List<String> playerNames)
    {
        _battles = battles;
        _players = players;        
        _roundMax = roundMax;
        _isSymmetric = isSymmetric;
        _name = name;
        _playerNames = playerNames;
    }

    /**
    * Starts this benchmark.
    */
    public void Run(String resultDir, boolean visualize, boolean partiallyObservable, boolean saveTrace, String traceDir) throws Exception {      
        System.out.println("Benchmark started");
        System.out.println(_players.get(0).toString());
        System.out.println(_players.get(1).toString());
        
        Results sumResults = new Results();
        
        long startTime = System.currentTimeMillis();
        int battleNumber = 1;
        boolean prevUnfinishedOrDraw = false;

        File resultsFile = new File(Paths.get(resultDir, _name + ".csv").toString());
        if (resultsFile.exists())
            System.out.println("[WARNING] File " + resultsFile.getPath() + " already exists, overwriting");
        
        resultsFile.getParentFile().mkdirs();

        List<AI> bots2 = new LinkedList<>();
        for(AI bot:_players) bots2.add(bot.clone());
        
        try (PrintStream output = new PrintStream(resultsFile)) {
            output.println(
                "battleName; p1Win; p2Win; p1hull; p2hull; rounds;"
            );
            
            // Run every battle specified in the becnhmark.
            for(BattleSettings bs:_battles) {
                Results res = new Results();
                
                PhysicalGameState pgs = bs.gameState;   

                System.out.println("Battle " + bs.name + " started for " + bs.repeats + " repeats");
                
                // Run the battle a given number of times.
                for (int i = 0; i < bs.repeats; i++) {            
                    int ai1_idx = 0;
                    int ai2_idx = 1;
                        
                    // A variable used to store the remaining hps of the two players after
                    // a battle. As it isn't reset between iterations, it can be used to
                    // decide who the symwin goes to, provided that neither of the games
                    // was unfinished.
                    float[] hps = new float[] {0.0f, 0.0f};
                    
                    // A variable used to store the sum of the remaining hps of the two
                    // players after two iterations. Used to decide who the symwin
                    // goes to in cases where both games were unfinished.
                    float[] hpsSum = new float[] {0.0f, 0.0f};
                    
                    // Perform to repeats of every battle run while switching the players.
                    // This is unnecessary in microRTS, since the players perform moves
                    // at the same time, but since this is the way I did it in Children
                    // of the Galaxy, I kept it this way.
                    for (int iter = 0; iter < 2; ++iter) {
                        System.out.println("  Iter number " + battleNumber++ + " started");
                        
                        // Cloning just in case an AI has a memory leak.
                        // By using a clone, it is discarded, along with the leaked memory,
                        // after each game, rather than accumulating over several games.
                        AI ai1 = bots2.get(ai1_idx).clone();
                        AI ai2 = bots2.get(ai2_idx).clone();
                        long lastTimeActionIssued = 0;

                        ai1.reset();
                        ai2.reset();
                        
                        String rs = "" + (r.nextInt(900000) + 100000);
                        if (ai1 instanceof BasicMCTS) {
                            ((BasicMCTS)ai1).setBattleName(bs.name);
                            ((BasicMCTS)ai1).setRndBattleString(rs);
                        }
                        if (ai2 instanceof BasicMCTS) {
                            ((BasicMCTS)ai2).setBattleName(bs.name);
                            ((BasicMCTS)ai2).setRndBattleString(rs);
                        }

                        GameState gs = new GameState(pgs.clone(), bs.utt);
                        PhysicalGameStateJFrame w = null;
                        if (visualize) w = PhysicalGameStatePanel.newVisualizer(gs, 600, 600, partiallyObservable);

                        boolean gameover = false;
                        Trace trace = null;
                        TraceEntry te;
                        if(saveTrace){
                                trace = new Trace(bs.utt);
                                te = new TraceEntry(gs.getPhysicalGameState().clone(),gs.getTime());
                            trace.addEntry(te);
                        }
                        
                        int roundCounter = 0;
                        
                        // Run the simulation.
                        do {
                            ++roundCounter;
                            
                            if (GC_EACH_FRAME) System.gc();
                            PlayerAction pa1 = null, pa2 = null;
                            if (partiallyObservable) {          
                                pa1 = ai1.getAction(0, new PartiallyObservableGameState(gs,0));
                                pa2 = ai2.getAction(1, new PartiallyObservableGameState(gs,1));
                            } else {
                                pa1 = ai1.getAction(0, gs);
                                pa2 = ai2.getAction(1, gs);
                            }
                            if (saveTrace && (!pa1.isEmpty() || !pa2.isEmpty())) {
                                te = new TraceEntry(gs.getPhysicalGameState().clone(),gs.getTime());
                                te.addPlayerAction(pa1.clone());
                                te.addPlayerAction(pa2.clone());
                                trace.addEntry(te);
                            }

                            if (gs.issueSafe(pa1)) lastTimeActionIssued = gs.getTime();
                            if (gs.issueSafe(pa2)) lastTimeActionIssued = gs.getTime();
                            gameover = gs.cycle();
                            if (w!=null) {
                                w.setStateCloning(gs);
                                w.repaint();
                                try {
                                    Thread.sleep(1);    // give time to the window to repaint
                                } catch (Exception e) {
                                    e.printStackTrace();
                                }
                            }
                        } while (!gameover && (gs.getTime() < _roundMax));
                        ai1.gameOver(gs.winner());
                        ai2.gameOver(gs.winner());
                        
                        res.RoundCounts.add(roundCounter);
                        
                        if (w!=null) w.dispose();
                        int winner = gs.winner();
                        int winnerIndex = ai1_idx == 0 ? winner : 1 - winner;
                                
                        float h0 = 0;
                        float h1 = 0;
                        
                        // If the winner doesn't equal -1, it means one of the
                        // players won.
                        if (winner != -1) {
                            // If the same player won twice in a row, not assigning zero
                            // to this variable would mean that their hps from the two
                            // games would be added up.
                            // On the other hand, if the other player one in the previous
                            // game, this would already be zero, so no information is lost.
                            hps[winnerIndex] = 0.0f;   
                            
                            // Count the remaining hp of the winner.
                            for (Unit u:gs.getUnits()) {
                                if (u.getPlayer() == winner) {
                                    if (u.getPlayer() == ai1_idx) {
                                        hps[0] += u.getHitPoints();
                                    }
                                    else {
                                        hps[1] += u.getHitPoints();
                                    }
                                }
                            }
                            
                            // This is probably redundant, as the hpsSum variable
                            // only comes into play if there was no winner in either game.
                            hpsSum[0] += hps[0];
                            hpsSum[1] += hps[1];
                            
                            res.WinCounts.get(winnerIndex).incrementAndGet();
                            res.HullRemaining.get(winnerIndex).add(hps[winnerIndex]);
                            System.out.println("    Winner: " + winner + " " + _players.get(winnerIndex));
                        }
                        // Otherwise, the game was either a draw or it didn't finish
                        // in the allotted number of rounds.
                        else {
                            // If the maximum number of rounds wasn't reached, the game
                            // was a draw which means that both players ended with 0 hp,
                            // so there is no need to update any hp-related variables.
                            if (gs.getTime() != _roundMax) {                        
                                res.WinCounts.get(2).incrementAndGet();
                            }
                            // Otherwise, the game didn't finish.
                            else {
                                res.Unfinished++;
                                
                                for (Unit u:gs.getUnits()) {
                                    if (u.getPlayer() == ai1_idx) {
                                        h0 += u.getHitPoints();
                                    }
                                    else {
                                        h1 += u.getHitPoints();
                                    }
                                }
                                
                                hpsSum[0] += h0;
                                hpsSum[1] += h1;
                                
                                // The player with more remaining hit points is designated
                                // the winner.
                                winnerIndex = h0 > h1 ? 0 : h1 > h0 ? 1 : 2;
                                
                                res.WinCounts.get(winnerIndex).incrementAndGet();
                                res.HullRemaining.get(0).add(h0);
                                res.HullRemaining.get(1).add(h1);
                            }
                        }
                        
                        if(saveTrace){
                                te = new TraceEntry(gs.getPhysicalGameState().clone(), gs.getTime());
                                trace.addEntry(te);
                                XMLWriter xml;
                                ZipOutputStream zip = null;
                                String filename=ai1.toString()+"Vs"+ai2.toString()+"-"+(i*2 + iter)+"-"+i;
                                filename=filename.replace("/", "");
                                filename=filename.replace(")", "");
                                filename=filename.replace("(", "");
                                filename=traceDir+"/"+filename;
                                xml = new XMLWriter(new FileWriter(filename+".xml"));
                                trace.toxml(xml);
                                xml.flush();
                        }

                        if (!_isSymmetric) {
                            break;
                        }
                        // Decide to whom the symwin goes every other iteration.
                        else {
                            if (iter == 1) {
                                // If both this game and the previous one had no winner,
                                // i.e. were unfinished or drawn, then who the symwin goes to
                                // is decided based on the sum of the players' hps from both
                                // games.
                                if (prevUnfinishedOrDraw && winner == -1)
                                {                                    
                                    int symwinner = hpsSum[0] > hpsSum[1] ? 0 : hpsSum[0] < hpsSum[1] ? 1 : 2;                                    
                                    sumResults.SymWinCounts.get(symwinner).incrementAndGet();
                                }
                                // Otherwise, there are three possibilities.
                                // 1. Only one of the games had a winner, in which case
                                // only one of the fields in hps has a non-zero value.
                                // 2. Both games were won by the same player, in which case
                                // again, one of the fields is zero.
                                // 3. The two games were won by two different players,
                                // in which case both fields in hps are non-zero and the
                                // symwin goes to the player with more remaining hit points.
                                else
                                {
                                    int symwinner = hps[0] > hps[1] ? 0 : hps[0] < hps[1] ? 1 : 2;
                                    sumResults.SymWinCounts.get(symwinner).incrementAndGet();
                                }
                            }
                            
                            ai1_idx = 1;
                            ai2_idx = 0;
                        }
                        
                        prevUnfinishedOrDraw = winner == -1;
                        
                        int p1Win = winnerIndex == 0 ? 1 : 0;
                        int p2Win = winnerIndex == 1 ? 1 : 0;
                        
                        if (winner != -1) {
                            if (p1Win == 1) {
                                OutputResults(output, bs, p1Win, p2Win, hps[0], 0, roundCounter);
                            }
                            else {
                                OutputResults(output, bs, p1Win, p2Win, 0, hps[1], roundCounter);
                            }
                        }
                        else {
                            OutputResults(output, bs, p1Win, p2Win, h0, h1, roundCounter);
                        }
                    } 
                }        
                System.out.println("Battle ended");
                sumResults.add(res);
            }
        }
        OutputFinalBenchmarkResults(sumResults);
        long time = System.currentTimeMillis() - startTime;
        String timeString = String.format("%02d:%02d:%02d.%d", (time / (3600000)), (time / 60000) % 60, (time / 1000) % 60, time % 1000);
        System.out.println("Total benchmark time: " + timeString + "\n");
    }

    /**
     * Prints out the final results to the console. Used at the end of the benchmark.
     */
    private void OutputFinalBenchmarkResults(Results results)
    {   
        System.out.println("Benchmark ended");
        
        String line = results.WinCounts.get(0) + " wins, ";
        line += results.SymWinCounts.get(0) + " symWins, ";
        line += results.HullRemaining.get(0) + " hull ";
        line += _players.get(0);
        System.out.println(line);
        
        line = results.WinCounts.get(1) + " wins, ";
        line += results.SymWinCounts.get(1) + " symWins, ";
        line += results.HullRemaining.get(1) + " hull ";
        line += _players.get(1);
        System.out.println(line);
        
        if (results.WinCounts.get(2).intValue() != 0) {
            System.out.println("Draws: " + results.WinCounts.get(2));
        }
        if (results.SymWinCounts.get(2).intValue() != 0) {
            System.out.println("SymDraws: " + results.SymWinCounts.get(2));
        }
        if (results.Unfinished != 0) {
            System.out.println("Unfinished: " + results.Unfinished);
        }
    }

    /**
     * Prints results using the given {@code PrintStream}.
     */
    private void OutputResults(PrintStream ps, BattleSettings battle, int p1Win, int p2Win, float p1Hull, float p2Hull, int roundCount)
    {
        ps.println(String.join(";",
            battle.name,
            String.valueOf(p1Win),
            String.valueOf(p2Win),
            String.valueOf(p1Hull),
            String.valueOf(p2Hull),
            String.valueOf(roundCount)
        ));
    }
}