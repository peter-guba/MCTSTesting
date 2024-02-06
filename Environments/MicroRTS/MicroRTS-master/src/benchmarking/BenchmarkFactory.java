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

import java.nio.file.Paths;
import java.util.List;
import java.util.ArrayList;
import ai.core.AI;
import ai.mcts.uct.*;
import java.io.File;
import java.io.IOException;
import javax.xml.XMLConstants;
import javax.xml.transform.stream.StreamSource;
import javax.xml.validation.Schema;
import javax.xml.validation.SchemaFactory;
import org.xml.sax.SAXException;
import org.w3c.dom.*;
import javax.xml.parsers.*;
import util.Pair;
import java.time.LocalDateTime;
import ai.portfolio.portfoliogreedysearch.DependentUnitScript;
import ai.portfolio.portfoliogreedysearch.Kiter;
import ai.portfolio.portfoliogreedysearch.NOKAV;
import ai.abstraction.pathfinding.AStarPathFinding;
import java.util.Arrays;
import ai.RandomScriptedAI;

/**
 * Factory class which fills and creates a benchmark.
 */
public class BenchmarkFactory {   
    private static final String baseFolder = "../../../";
    
    private static final String _benchmarkDbDir = "Resources/Benchmarks";
    private static final String _benchmarkSetDir = "Resources/BenchmarkSets";
    private static final String _battleSetDbDir = "Resources/BattleSets";
    private static final String _aiDbDir = "Resources/AIs";

    private static final String _benchmarkSchemaFile = Paths.get(baseFolder, _benchmarkDbDir, "Benchmark.xsd").toString();
    private static final String _benchmarkSetSchemaFile = Paths.get(baseFolder, _benchmarkSetDir, "BenchmarkSet.xsd").toString();
    private static final String _battleSetSchemaFile = Paths.get(baseFolder, _battleSetDbDir, "BattleSet.xsd").toString();
    private static final String _aiDbSchemaFile = Paths.get(baseFolder, _aiDbDir, "AI.xsd").toString();

    private static Schema _benchmarkSchema;
    private static Schema _benchmarkSetSchema;
    private static Schema _battleSetSchema;
    private static Schema _aiSchemaSet;
    
    private static DocumentBuilder dBuilder;

    private static boolean _isInitialized;

    private static void Initialize()
    {
        if (!_isInitialized)
        {            
            try {
            _isInitialized = true;
            
            DocumentBuilderFactory dbFactory = DocumentBuilderFactory.newInstance();
            dBuilder = dbFactory.newDocumentBuilder();            
            
            SchemaFactory factory = SchemaFactory.newInstance(XMLConstants.W3C_XML_SCHEMA_NS_URI);
            
            _benchmarkSchema = factory.newSchema(new File(_benchmarkSchemaFile));
            _benchmarkSetSchema = factory.newSchema(new File(_benchmarkSetSchemaFile));
            _battleSetSchema = factory.newSchema(new File(_battleSetSchemaFile));
            _aiSchemaSet = factory.newSchema(new File(_aiDbSchemaFile));
            }
            catch (SAXException e) {
                System.out.println("Problem with loading XML schema.");
                System.exit(-69);
            }
            catch (ParserConfigurationException e) {
                System.out.println("Problem with creating document builder.");
                System.exit(-6969);
            }
        }
    }

    /**
    * Creates and returns fully initialized benchmark specified in file with name {@code id}.    
    * @param id">Name of the file with the battle config.
    */
    public static List<Benchmark> MakeBenchmarkSet(String id) throws ResourceMissingException, SAXException, IOException, InvalidResourceReferenceException, InvalidXmlDataException
    {
        Initialize();
        String docFile = Paths.get(baseFolder, _benchmarkSetDir, id + ".xml").toString();
        CheckResource(docFile);
        Element root = dBuilder.parse(new File(docFile)).getDocumentElement();       
        _benchmarkSetSchema.newValidator().validate(new StreamSource(new File(docFile)));

        NodeList benchmarksXml = root.getElementsByTagName("Benchmark");
        List<Benchmark> benchmarks = new ArrayList<>();
        for (int i = 0; i < benchmarksXml.getLength(); ++i)
        {
            try
            {
                benchmarks.add(MakeBenchmark(benchmarksXml.item(i).getAttributes().getNamedItem("Id").getNodeValue()));
            }
            catch (ResourceMissingException rme)
            {
                throw new InvalidResourceReferenceException(rme.Resource, docFile);
            }
        }

        return benchmarks;
    }

    private static Benchmark MakeBenchmark(String id) throws ResourceMissingException, SAXException, IOException, InvalidResourceReferenceException, InvalidXmlDataException
    {
        String docFile = Paths.get(baseFolder, _benchmarkDbDir, id + ".xml").toString();
        CheckResource(docFile);        
        Element root = dBuilder.parse(new File(docFile)).getDocumentElement();
        _benchmarkSchema.newValidator().validate(new StreamSource(new File(docFile)));
        
        // Repeats
        int repeats = Integer.parseInt(root.getElementsByTagName("Repeats").item(0).getTextContent());

        // Handle battles        
        NodeList battleSetsXml = root.getElementsByTagName("BattleSet");
        List<BattleSettings> battles = new ArrayList<>();
        for (int i = 0; i < battleSetsXml.getLength(); ++i)
        {
            try
            {
                battles.addAll(MakeBattles(battleSetsXml.item(i).getAttributes().getNamedItem("Id").getNodeValue(), repeats));
            }
            catch (ResourceMissingException rme)
            {
                throw new InvalidResourceReferenceException(rme.Resource, docFile);
            }
        }

        // Handle players
        NodeList playersXml = root.getElementsByTagName("Player");
        List<AI> players = Arrays.asList(null, null);
        List<String> playerNames = new ArrayList<String>() {
            {
                add("NO_NAME");
                add("NO_NAME");
            }
        };
        for (int i = 0; i < playersXml.getLength(); ++i)
        {
            String date = "" + LocalDateTime.now().getDayOfMonth();
            date += "_" + LocalDateTime.now().getMonthValue();
            date += "_" + LocalDateTime.now().getHour();
            date += "_" + LocalDateTime.now().getMinute();
            date += "_" + LocalDateTime.now().getSecond() + "-";
            Pair<AI, String> p = MakePlayer((Element)playersXml.item(i), date + id);
            int playerIdx = Integer.parseInt(playersXml.item(i).getAttributes().getNamedItem("Index").getNodeValue());
            players.set(playerIdx, p.m_a);
            playerNames.add(playerIdx, p.m_b);
        }
        
        // Handle max rounds
        int maxRounds = Integer.parseInt(root.getElementsByTagName("MaxRounds").item(0).getTextContent());

        // Handle is symmetric
        boolean isSymmetric = Boolean.parseBoolean(root.getElementsByTagName("IsSymmetric").item(0).getTextContent());

        Benchmark benchmark = new Benchmark(battles, players, maxRounds, isSymmetric, id, playerNames);
        return benchmark;
    }

    private static List<BattleSettings> MakeBattles(String id, int repeats) throws ResourceMissingException, SAXException, IOException, InvalidResourceReferenceException, InvalidXmlDataException
    {
        String docFile = Paths.get(baseFolder, _battleSetDbDir, id + ".xml").toString();
        CheckResource(docFile);        
        Element root = dBuilder.parse(new File(docFile)).getDocumentElement();
        _battleSetSchema.newValidator().validate(new StreamSource(new File(docFile)));

        NodeList battlesXml = root.getChildNodes();
        List<BattleSettings> battles = new ArrayList<>();
        for (int i = 0; i < battlesXml.getLength(); ++i)
        {
            if (battlesXml.item(i).getNodeName().equals("Battle")) {
                try
                {
                    battles.add(BattleFactory.MakeBattle(battlesXml.item(i).getAttributes().getNamedItem("Id").getNodeValue(), repeats));
                }
                catch (ResourceMissingException rme)
                {
                    throw new InvalidResourceReferenceException(rme.Resource, docFile);
                }
            }
        }

        return battles;
    }

    private static Pair<AI, String> MakePlayer(Element playerXml, String bmrkID) throws ResourceMissingException, SAXException, IOException
    {
        String playerName;
        Element ai;
        Element aiElement = (Element)playerXml.getElementsByTagName("AI").item(0);
        if (aiElement != null)
        {
            ai = (Element)aiElement.getFirstChild();
            playerName = ai.getLocalName();
        }
        else // We have reference to AI
        {
            ai = (Element)ResolveAIRef(playerXml.getElementsByTagName("AIRef").item(0));
            playerName = playerXml.getElementsByTagName("AIRef").item(0).getAttributes().getNamedItem("Id").getNodeValue();
        }
        String name = ai.getNodeName();
        switch (name)
        {
            case "FapMcts":
                return new Pair(MakeFapMctsPlayer(ai, bmrkID), playerName);
            case "Mcts":
                return new Pair(MakeBasicMctsPlayer(ai, bmrkID), playerName);
            case "MctsHP":
                return new Pair(MakeMctsHpPlayer(ai, bmrkID), playerName);
            case "RQBonusMcts":
                return new Pair(MakeRQBonusMcts(ai, bmrkID), playerName);
            case "SigmoidMcts":
                return new Pair(MakeSigmoidMctsPlayer(ai, bmrkID), playerName);
            case "SimpleRegretMcts":
                return new Pair(MakeSimpleRegretMctsPlayer(ai, bmrkID), playerName);
            case "UcbTunedMcts":
                return new Pair(MakeUcbTunedMcts(ai, bmrkID), playerName);
            case "VoiMcts":
                return new Pair(MakeVoiMcts(ai, bmrkID), playerName);
            case "WPMcts":
                return new Pair(MakeWPMcts(ai, bmrkID), playerName);
            default:
                throw new IllegalArgumentException(playerXml.getClass().getName());
        }
    }

    private static Node ResolveAIRef(Node aiRefXml) throws ResourceMissingException, SAXException, IOException
    {
        String aiId = aiRefXml.getAttributes().getNamedItem("Id").getNodeValue();
        return MakeAi(aiId);
    }

    private static Node MakeAi(String id) throws ResourceMissingException, SAXException, IOException
    {
        String docFile = Paths.get(baseFolder, _aiDbDir, id + ".xml").toString();
        CheckResource(docFile);
        Element root = dBuilder.parse(new File(docFile)).getDocumentElement();
        _aiSchemaSet.newValidator().validate(new StreamSource(new File(docFile)));        
        
        Node ai = root.getChildNodes().item(1);
        return ai;
    }

    private static AI MakeWPMcts(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        double voiBase = Double.parseDouble(playerXml.getAttribute("VB"));
        double poeBase = Double.parseDouble(playerXml.getAttribute("PB"));
        boolean normalize = Boolean.parseBoolean(playerXml.getAttribute("Normalize"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new WPMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, voiBase, poeBase, normalize, "wp_mcts", bmrkID);
    }

    private static AI MakeFapMctsPlayer(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int numOfSegments = Integer.parseInt(playerXml.getAttribute("NumOfSegments"));
        boolean expSeg = Boolean.parseBoolean(playerXml.getAttribute("ExponentialSegmentation"));
        boolean expMul = Boolean.parseBoolean(playerXml.getAttribute("ExponentialMultiplication"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new FAPMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, expSeg, expMul, numOfSegments, "fap_mcts", bmrkID);
    }

    private static AI MakeBasicMctsPlayer(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new BasicMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, "basic_mcts", bmrkID);
    }

    private static AI MakeMctsHpPlayer(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new MCTSHP(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, "mcts_hp", bmrkID);
    }

    private static AI MakeRQBonusMcts(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        double k = Double.parseDouble(playerXml.getAttribute("K"));
        boolean relEnabled = Boolean.parseBoolean(playerXml.getAttribute("RelativeBonusEnabled"));
        boolean qualEnabled = Boolean.parseBoolean(playerXml.getAttribute("QualitativeBonusEnabled"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        String name = "";
        if (relEnabled && qualEnabled) {
            name = "rq_bonus_mcts";
        }
        else if (relEnabled) {
            name = "r_bonus_mcts";
        }
        else if (qualEnabled) {
            name = "q_bonus_mcts";
        }
        
        return new RQBonusMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, k, relEnabled, qualEnabled, name, bmrkID);
    }

    private static AI MakeSigmoidMctsPlayer(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        float constantK = Float.parseFloat(playerXml.getAttribute("K"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new SigmoidMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, constantK, "sigmoid_mcts", bmrkID);
    }

    private static AI MakeSimpleRegretMctsPlayer(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        boolean useEpsilonGreedy = Boolean.parseBoolean(playerXml.getAttribute("UseEpsilonGreedy"));
        double epsilon = Double.parseDouble(playerXml.getAttribute("Epsilon"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        if (useEpsilonGreedy) {
            return new SimpleRegretMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, epsilon, "simple_regret_mcts", bmrkID);
        }
        else {
            return new SimpleRegretMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, "simple_regret_mcts", bmrkID);
        }
    } 
    
    private static AI MakeUcbTunedMcts(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }

        return new UCBTunedMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, "ucb_tuned_mcts", bmrkID);
    }

    private static AI MakeVoiMcts(Element playerXml, String bmrkID)
    {
        int maxPlayouts = Integer.parseInt(playerXml.getAttribute("MaxPlayouts"));
        int maxTurns = Integer.parseInt(playerXml.getAttribute("PlayoutRoundLimit"));
        
        NodeList portfolioXml = playerXml.getElementsByTagName("Script");
        List<DependentUnitScript> portfolio = new ArrayList<>();
        for (int i = 0; i < portfolioXml.getLength(); ++i)
        {
            portfolio.add(MakeScript(portfolioXml.item(i).getTextContent()));
        }
        
        return new VOIAwareMCTS(0, maxPlayouts, maxTurns, Integer.MAX_VALUE, new RandomScriptedAI(portfolio), portfolio, "voi_mcts", bmrkID);
    }

    private static DependentUnitScript MakeScript(String scriptName)
    {
        switch (scriptName)
        {
            case "NOKAV":
                return new NOKAV(new AStarPathFinding());
            case "Kiter":
                return new Kiter(new AStarPathFinding(), 3);
            default:
                throw new IllegalArgumentException(scriptName);
        }
    }
    
    private static void CheckResource(String docFile) throws ResourceMissingException {
        File tempFile = new File(docFile);
        if (!tempFile.exists()) {
            throw new ResourceMissingException(docFile);
        }
    }
}