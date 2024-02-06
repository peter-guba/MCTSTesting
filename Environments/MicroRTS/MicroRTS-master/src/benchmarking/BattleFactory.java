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

import java.io.File;
import java.util.HashMap;
import java.util.Map;
import rts.units.Unit;
import java.nio.file.Paths;
import javax.xml.XMLConstants;
import javax.xml.transform.stream.StreamSource;
import javax.xml.validation.Schema;
import javax.xml.validation.SchemaFactory;
import org.xml.sax.SAXException;
import java.io.IOException;
import java.util.Arrays;
import java.util.List;
import java.util.ArrayList;
import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import rts.PhysicalGameState;
import org.w3c.dom.*;
import java.util.concurrent.atomic.AtomicInteger;
import rts.units.UnitTypeTable;
import rts.units.UnitType;
import rts.Player;
import java.util.Random;

/**
 * Factory class which fills and creates a battle.
 */
public class BattleFactory {   
    private static final String baseFolder = "../../../";
    
    private static final String _battleDbDir = "Resources/Battles";
    private static final String _unitDbDir = "Resources/Units";
    private static final Map<String, Unit> _unitCache = new HashMap<String, Unit>();
 
    private static final String _battleSchemaFile = Paths.get(baseFolder, _battleDbDir, "Battle.xsd").toString();
    private static Schema _battleSchema;
 
    private static boolean _isInitialized;
    
    private static DocumentBuilder dBuilder;
    private static UnitTypeTable utt; 
    
    private static void Initialize() throws ResourceMissingException
    {
        if (!_isInitialized)
        {
            try {
                _isInitialized = true;
            
                utt = new UnitTypeTable(UnitTypeTable.VERSION_ORIGINAL, UnitTypeTable.MOVE_CONFLICT_RESOLUTION_CANCEL_RANDOM);
                
                DocumentBuilderFactory dbFactory = DocumentBuilderFactory.newInstance();
                dBuilder = dbFactory.newDocumentBuilder();

                SchemaFactory factory = SchemaFactory.newInstance(XMLConstants.W3C_XML_SCHEMA_NS_URI);

                CheckResource(_battleSchemaFile);
                _battleSchema = factory.newSchema(new File(_battleSchemaFile));
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
     * Returns a fully created battle specified in file with name {@code id}.
     * @param id Name of the file with the battle config.
     * @param repeats How many times should be the battle repeated.
     */
    public static BattleSettings MakeBattle(String id, int repeats) throws ResourceMissingException, SAXException, IOException, InvalidXmlDataException
    {
        Initialize();
        String docFile = Paths.get(baseFolder, _battleDbDir, id + ".xml").toString();
        CheckResource(docFile);     
        _battleSchema.newValidator().validate(new StreamSource(new File(docFile)));
 
        List<Map<String, AtomicInteger>> unitCounts = Arrays.asList(new HashMap<String, AtomicInteger>(), new HashMap<String, AtomicInteger>());
        PhysicalGameState gameState = MakeGameState(dBuilder.parse(new File(docFile)), unitCounts);
        BattleSettings battle = new BattleSettings(gameState, repeats, id, unitCounts, utt);
        return battle;
    }
 
    private static PhysicalGameState MakeGameState(Document doc, List<Map<String, AtomicInteger>> unitCounts) throws InvalidXmlDataException
    {        
        boolean isSymmetric = doc.getElementsByTagName("SymmetricBattle").getLength() != 0;
        NodeList players = doc.getDocumentElement().getElementsByTagName("Player");
        List<Unit> units = new ArrayList<>();
        List<Unit> symUnits = new ArrayList<>();
        
        int playerIndex = -1;
        int otherPlayer = -1;
        int maxDim = 0;
        
        for (int i = 0; i < players.getLength(); ++i)
        {              
            playerIndex = Integer.parseInt(players.item(i).getAttributes().getNamedItem("Index").getNodeValue());
            NodeList pUnits = ((Element)((Element)players.item(i)).getElementsByTagName("Units").item(0)).getElementsByTagName("Unit");
            for (int j = 0; j < pUnits.getLength(); j++)
            {
                Unit unit = MakeUnit((Element)pUnits.item(j), unitCounts.get(playerIndex), playerIndex);
                units.add(unit);
                
                if (unit.getX() > maxDim) {
                    maxDim = unit.getX();
                }
                if (unit.getY() > maxDim) {
                    maxDim = unit.getY();
                }
            }
        }
 
        if (isSymmetric)
        {
            // There is at least one player with at least one unit
            otherPlayer = 1 - playerIndex;
            for (Unit unit : units)
            {
                Unit symUnit = new Unit(otherPlayer, unit.getType(), -unit.getX(), -unit.getY(), unit.getResources());
                symUnits.add(symUnit);
                
                if (symUnit.getX() > maxDim) {
                    maxDim = symUnit.getX();
                }
                if (symUnit.getY() > maxDim) {
                    maxDim = symUnit.getY();
                }
            }
        }
        
        PhysicalGameState pgs = new PhysicalGameState(maxDim * 2 + 1, maxDim * 2 + 1);
        pgs.addPlayer(new Player(playerIndex, 0));
        pgs.addPlayer(new Player(otherPlayer, 0));
        
        for(Unit u : units) {
            u.setX(u.getX() + maxDim);
            u.setY(u.getY() + maxDim);
            pgs.addUnit(u);
        }
        
        for(Unit u : symUnits) {
            u.setX(u.getX() + maxDim);
            u.setY(u.getY() + maxDim);
            pgs.addUnit(u);
        }
                
        return pgs;
    }
 
    private static Unit MakeUnit(Element unitXml, Map<String, AtomicInteger> unitCounts, int player)
    {
        int x = Integer.parseInt(unitXml.getAttribute("Q"));
        int y = Integer.parseInt(unitXml.getAttribute("R"));
        x += -(int)Math.signum(x) * 5;
 
        String typeString = unitXml.getAttribute("Id").equals("battleship_0") ? "Light" : "Heavy";
        UnitType type = utt.getUnitType(typeString);
        Unit unit = new Unit(player, type, x, y, 0);
        
        if (!unitCounts.containsKey(typeString))
        {
            unitCounts.put(typeString, new AtomicInteger(1));
        }
        else
        {
            unitCounts.get(typeString).incrementAndGet();
        }
 
        return unit;
    }
    
    private static void CheckResource(String docFile) throws ResourceMissingException {
        File tempFile = new File(docFile);
        if (!tempFile.exists()) {
            throw new ResourceMissingException(docFile);
        }
    }
}
