/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package benchmarking;

import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.io.File;
import org.xml.sax.SAXException;
import java.io.IOException;
import java.util.Random;

public class BenchmarkRunner {
    private static Random r = new Random();
    
    public static void main(String[] args) throws Exception {
        String benchmarkSet = args.length == 1 ? args[0] : "TestBenchSet";
        BenchmarkRunner br = new BenchmarkRunner();
        br.Run(benchmarkSet);
    }
    
    /**
    * Tries to create and run given benchmark set.
    * @param benchmarkSetId File name of the benchmark set.
    */
    public void Run(String benchmarkSetId) throws Exception
    {
        DateTimeFormatter dtf = DateTimeFormatter.ofPattern("yyyy_MM_dd-HH_mm_ss"); 
        String resultsDir = Paths.get("results", benchmarkSetId, dtf.format(LocalDateTime.now()) + '_' + (r.nextInt(900000) + 100000)).toString();
        File f = new File(resultsDir);
        if (!f.exists() || !f.isDirectory())
            f.mkdir();
        try
        {
            System.out.println("Benchmarking started...");

            List<Benchmark> benchmarkSet = BenchmarkFactory.MakeBenchmarkSet(benchmarkSetId);
            for (Benchmark benchmark : benchmarkSet)
            {
                benchmark.Run(resultsDir, false, false, false, "");
            }

            System.out.println("Benchmarking finished...");
        }
        catch (ResourceMissingException e)
        {
            System.out.println(e.getMessage());
            e.printStackTrace();
        }
        catch (SAXException e) {
            System.out.println(e.getMessage());
            e.printStackTrace();
        }
        catch (IOException e) {
            System.out.println(e.getMessage());
            e.printStackTrace();
        }
        catch (InvalidXmlDataException e)
        {
            System.out.println(e.getMessage());
            e.printStackTrace();
        }
        catch (InvalidResourceReferenceException e)
        {
            System.out.println("Invalid resource reference");
            System.out.println(e.getMessage());
            e.printStackTrace();
        }

        System.out.println("Terminating...");
    }
}
