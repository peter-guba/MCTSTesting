/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package ai.mcts.uct;

import ai.RandomAI;
import ai.abstraction.pathfinding.AStarPathFinding;
import ai.core.AI;
import ai.evaluation.BasicOneZero;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.selection.UCB;
import ai.selection.SelectionFunction;
import ai.evaluation.EvaluationFunction;
import java.util.Arrays;
import rts.GameState;
import rts.units.UnitTypeTable;
import java.util.List;
import ai.RandomScriptedAI;
import ai.abstraction.UpdateableAbstractAction;
import ai.evaluation.HPDifference;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.Map;


public class FAP_RQBonus_MCTS extends BasicMCTS {
    /**
    * A constant used in computing the sigmoid function of the bonuses.
    */
    private double _k = 1.0;

    /**
    * The average combined distance of the partial game tree traversal and the playout during
    * an iteration.
    */
    private double _averageDistance = 0;

    /**
    * The sample standard deviation of the sampled simulation lengths.
    */
    private double _sampleStandardRelativeDeviation = 0;

    /**
    * Like averageDistance, but all the lengths of playouts where player 0 lost get substituted with zeros.
    */
    private double _averageWinDistance = 0;

    /**
    * A list of all the simulation distances encountered so far.
    * Has to be kept for the sample standard deviation to be computable.
    */
    private List<Double> _distances = new ArrayList<>();

    /**
    * A list of all the final state qualities encountered so far.
    * Has to be kept for the sample standard deviation to be computable.
    */
    private List<Double> _qualities = new ArrayList<>();

    /**
    * A list of the results of all the playouts until now. Used when computing alpha.
    */
    private List<Double> _playoutResults = new ArrayList<>();

    /**
    * The average quality of final states encountered in playouts.
    */
    private double _averageQuality = 0;

    /**
    * The sample standard deviation of the sampled simulation lengths.
    */
    private double _sampleStandardQualitativeDeviation = 0;

    /**
    * Like averageQuality, but all the lengths of playouts where player 0 lost get substituted with zeros.
    */
    private double _averageWinQuality = 0;

    /**
    * Determines whether the relative bonus is supposed to be added to the result.
    */
    private boolean relativeBonusEnabled;

    /**
    * Determines whether the qualitative bonus is supposed to be added to the result.
    */
    private boolean qualitativeBonusEnabled;
    
    /**
     * An additional evaluation function used to compute state quality when
     * qualitative bonus is enabled.
     */
    private HPDifference hpdiff = new HPDifference();
    
    /**
     * Stores the depth of the last playout.
     */
    private int playoutDepth = 0;
    
    /**
    * The number of segments into which the playouts are supposed to be separated.
    */
    private int numOfSegments;

    /**
    * Determines whether the segmentation is supposed to be exponential or linear.
    */
    private boolean exponentialSegmentation;

    /**
    * Determines whether the multiplicative factor should be computed in an exponential
    * or linear fashion.
    */
    private boolean exponentialMultiplication;
    
    public FAP_RQBonus_MCTS(UnitTypeTable utt) {
        super(100,-1,100,10,
             new RandomScriptedAI(Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3))),
             new BasicOneZero(),
             new UCB(),
             Arrays.asList(new NOKAV(new AStarPathFinding()), new Kiter(new AStarPathFinding(), 3)),
             "fap_rq_bonus_mcts",
             "");
        
        numOfSegments = 10;
        exponentialSegmentation = true;
        exponentialMultiplication = true;
    }  
    
    public FAP_RQBonus_MCTS(int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts, boolean eS, boolean eM, int nS, double k, boolean rE, boolean qE, String name, String bmrkID) {
        super(available_time, max_playouts, lookahead, max_depth, policy, new BasicOneZero(), new UCB(), scripts, name, bmrkID);
        
        numOfSegments = nS;
        exponentialSegmentation = eS;
        exponentialMultiplication = eM;
        
        _k = k;
        relativeBonusEnabled = rE;
        qualitativeBonusEnabled = qE;
    }
    
    private FAP_RQBonus_MCTS(
            int available_time, int max_playouts, int lookahead, int max_depth, RandomScriptedAI policy, List<DependentUnitScript> scripts,
            int nS, boolean eS, boolean eM,
            double k, boolean rE, boolean qE,
            double avgD, double sSRD, double avgWD, List<Double> dists, List<Double> quals, List<Double> pRes, double avgQ, double sSQD, double avgWQ,
            String name, String bmrkID,
            SelectionFunction sf, EvaluationFunction ef
    ) {
        super(available_time, max_playouts, lookahead, max_depth, policy, ef, sf, scripts, name, bmrkID);
        
        numOfSegments = nS;
        exponentialSegmentation = eS;
        exponentialMultiplication = eM;
        
        _k = k;
        relativeBonusEnabled = rE;
        qualitativeBonusEnabled = qE;
        
        _averageDistance = avgD;
        _sampleStandardRelativeDeviation = sSRD;
        _averageWinDistance = avgWD;
        
        _distances = new ArrayList<Double>(dists);
        _qualities = new ArrayList<Double>(quals);
        _playoutResults = new ArrayList<Double>(pRes);
        
        _averageQuality = avgQ;
        _sampleStandardQualitativeDeviation = sSQD;
        _averageWinQuality = avgWQ;
    }
    
    @Override
    public AI clone() {
        return new FAP_RQBonus_MCTS(
                TIME_BUDGET, ITERATIONS_BUDGET, MAXSIMULATIONTIME, MAX_TREE_DEPTH, defaultPolicy, scripts,
                numOfSegments, exponentialSegmentation, exponentialMultiplication,
                _k, relativeBonusEnabled, qualitativeBonusEnabled,
                _averageDistance, _sampleStandardRelativeDeviation, _averageWinDistance,
                _distances, _qualities, _playoutResults,
                _averageQuality, _sampleStandardQualitativeDeviation, _averageWinQuality,
                name, bmrkID,
                sf, ef
        );
    }
    
    @Override
    public void computeDuringOneGameFrame() throws Exception {
        // Reset variables between moves.
        _averageDistance = 0;
        _sampleStandardRelativeDeviation = 0;
        _averageWinDistance = 0;
        _distances = new ArrayList<>();
        _qualities = new ArrayList<>();
        _playoutResults = new ArrayList<>();
        _averageQuality = 0;
        _sampleStandardQualitativeDeviation = 0;
        _averageWinQuality = 0;
        
        long start = System.currentTimeMillis();
        nPlayouts = 0;
        long cutOffTime = start + TIME_BUDGET;
        if (TIME_BUDGET<=0) cutOffTime = 0;

        PrintStream ps = null;
        try {
            ps = new PrintStream(new FileOutputStream(path + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true));
        }       
        catch (FileNotFoundException e) {
            throw new Error("Can't log time and depth.");
        }
        
        while(true) {                      
            long roundStart = System.nanoTime();
            if (cutOffTime>0 && System.currentTimeMillis() > cutOffTime) break;
            nPlayouts++;
            if (ITERATIONS_BUDGET>0 && nPlayouts>ITERATIONS_BUDGET) break;
            monteCarloRun(playerForThisComputation, cutOffTime);
            logTimeAndDepth(System.nanoTime() - roundStart, ps);
            
            // If there is only one possible action at the root, there is no reason
            // to continue running the algorithm.
            if (root.children.size() == 1 && !root.hasMoreActions) {
                break;
            }
        }
        
        total_cycles_executed++;
    }
    
    @Override
    public double monteCarloRun(int player, long cutOffTime) throws Exception {
        MCTSNode leaf = root.UCTSelectLeaf(player, 1-player, MAX_TREE_DEPTH, true);
        if (leaf.depth > maxDepth) {
            maxDepth = leaf.depth;
        }    

        if (leaf!=null) {
            GameState gs2 = leaf.gs.clone();
            simulate(gs2, gs2.getTime() + MAXSIMULATIONTIME, leaf.getAssignedAbstractActions());

            // Compute the multiplicative factor which will multiply the result
            // of the evaluation function.
            int segmentNumber = GetSegmentNumber(nPlayouts);
            double multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = Math.pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }
            
            double evaluation = ef.evaluate(player, 1-player, gs2);

            _playoutResults.add(evaluation);
            
            // Compute relative bonus, if it is enabled.
            if (relativeBonusEnabled)
            {
                int distance = leaf.depth + playoutDepth;
                
                if (_sampleStandardRelativeDeviation > 0)
                {
                    // Compute the relative bonus based on simulation length.
                    double lambdaR = Bonus(_averageDistance - (distance), _sampleStandardRelativeDeviation);
                    evaluation += evaluation * GetAlpha(_averageDistance, _averageWinDistance, _distances, _sampleStandardRelativeDeviation) * lambdaR;
                }

                // Update the list of encountered distances, the mean, the mean of winning distances and the sample standard deviation.
                _distances.add((double)distance);
                _averageDistance = (_averageDistance * (nPlayouts - 1) + distance) / nPlayouts;
                _averageWinDistance = (_averageWinDistance * (nPlayouts - 1) + Math.max(0, Math.signum(evaluation)) * distance) / nPlayouts;

                if (nPlayouts > 1)
                {
                    _sampleStandardRelativeDeviation = 0;
                    for (double d : _distances)
                    {
                        _sampleStandardRelativeDeviation += (d - _averageDistance) * (d - _averageDistance);
                    }
                    _sampleStandardRelativeDeviation = (float)Math.sqrt(_sampleStandardRelativeDeviation / (nPlayouts - 1));
                }
            }

            // Compute qualitative bonus, if it is enabled.
            if (qualitativeBonusEnabled)
            {
                double quality = hpdiff.evaluate(player, 1-player, gs2);

                if (_sampleStandardQualitativeDeviation > 0)
                {
                    // Compute the qualitative bonus based on simulation length.
                    double lambdaR = Bonus(quality - _averageQuality, _sampleStandardQualitativeDeviation);
                    evaluation += evaluation * GetAlpha(_averageQuality, _averageWinQuality, _qualities, _sampleStandardQualitativeDeviation) * lambdaR;
                }

                // Update the list of encountered qualities, the mean, the mean of winning qualities and the sample standard deviation.
                _qualities.add(quality);
                _averageQuality = (_averageQuality * (nPlayouts - 1) + quality) / nPlayouts;
                _averageWinQuality = (_averageQuality * (nPlayouts - 1) + Math.max(0, Math.signum(evaluation)) * quality) / nPlayouts;

                if (nPlayouts > 1)
                {
                    _sampleStandardQualitativeDeviation = 0;
                    for (double q : _qualities)
                    {
                        _sampleStandardQualitativeDeviation += (q - _averageQuality) * (q - _averageQuality);
                    }
                    _sampleStandardQualitativeDeviation = (float)Math.sqrt(_sampleStandardQualitativeDeviation / (nPlayouts - 1));
                }
            }
            
            evaluation *= (float) multiplicativeFactor;

            Backpropagate(leaf, evaluation, (int)multiplicativeFactor);
            
            total_runs++;
            total_runs_this_move++;
            return evaluation;
        } else {
            // no actions to choose from :)
            System.err.println(this.getClass().getSimpleName() + ": claims there are no more leafs to explore...");
            return 0;
        }
    }
    
    @Override
    public void simulate(GameState gs, int time, Map<Long, UpdateableAbstractAction> aaas) throws Exception {
        boolean gameover = false;
        playoutDepth = 0;
        RandomScriptedAI randomPlayer1 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer1.setAbstractActions(aaas);
        RandomScriptedAI randomPlayer2 = (RandomScriptedAI) defaultPolicy.clone();
        randomPlayer2.setAbstractActions(aaas);

        do{
            if (gs.isComplete()) {
                gameover = gs.cycle();
            } else {
                gs.issue(defaultPolicy.getAction(0, gs));
                gs.issue(defaultPolicy.getAction(1, gs));
                playoutDepth += 2;
            }
        }while(!gameover && gs.getTime()<time);   
    }
    
    /**
     * Computes the bonus from the sample standard deviation and offset from the mean.
     */
    private double Bonus(double offsetFromMean, double sampleStandardDeviation)
    {
        double lambda = offsetFromMean / sampleStandardDeviation;
        return -1 + 2 / (1 + Math.exp(-_k * lambda));
    }

    /**
    * Computes the alpha multiplier used when computing the bonus.
    */
    private double GetAlpha(double averageMetric, double averageWinMetric, List<Double> metricList, double sampleStandardDeviation)
    {
        double covariance = 0;
        for (int i = 0; i < metricList.size(); ++i)
        {
            double winM = Math.max(0, _playoutResults.get(i)) * metricList.get(i);
            covariance += (winM - averageWinMetric) * (metricList.get(i) - averageMetric);
        }
        covariance /= nPlayouts - 1;

        return Math.abs(covariance / sampleStandardDeviation);
    }
    
    /**
    * Computes the number of the segment into which the n-th playout falls.
    */
    private int GetSegmentNumber(int n)
    {
        if (exponentialSegmentation)
        {
            double aux = Math.pow(2, numOfSegments) - 1;
            int result = 1;

            for (int i = 1; i <= numOfSegments; ++i)
            {
                double bound = ITERATIONS_BUDGET * (Math.pow(2, i) / aux);
                if (n < bound)
                {
                    break;
                }
                else
                {
                    ++result;
                }
            }

            return result;
        }
        else
        {
            return ((n - 1) / (ITERATIONS_BUDGET / numOfSegments)) + 1;
        }
    }

    // Works just like in default MCTS, it just has to increment the number of times
    // a node has been visited by the multiplicative factor used when evaluating
    // the previous playout.
    protected void Backpropagate(MCTSNode node, double evaluation, int visitCount)
    {
        do
        {
            node.visit_count += visitCount;
            node.accum_evaluation += evaluation;
            node = node.parent;
        } while (node != null);
    }
    
    @Override
    public String toString()
    {
        String segmentation = exponentialSegmentation ? "exp" : "lin";
        String multiplication = exponentialMultiplication ? "exp" : "lin";

        return "FAP RQBonus MCTS k: " + _k + ", relative bonus: " + relativeBonusEnabled + ", qualitative bonus: " + qualitativeBonusEnabled + ", segments: " + numOfSegments + ", segmentation: " + segmentation + ", multiplication: " + multiplication + ", playouts: " + ITERATIONS_BUDGET;
    }
}