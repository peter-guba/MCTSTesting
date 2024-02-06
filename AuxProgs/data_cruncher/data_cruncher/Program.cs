// A program for crunching the data produced by the three MCTS testing environments (Chess, CotG and microRTS).

namespace data_cruncher
{
    class Cruncher
    {
        public enum GameType { CotG, MicroRTS, Chess }

        //public static string sourcePath = "ChessData/";
        //public static string outputPath = "ChessData/";

        public static string sourcePath = "";
        public static string outputPath = "";
        
        public static GameType type = GameType.Chess;
        public static string tableBaseName = "chess_data_";
        public const double zValue = 1.960;

        public static void Main(string[] args)
        {
            //WPExtraDataCruncher.CrunchWPExtraData(args);
            //CheckmateTestDataCruncher.CrunchCheckmateTestData(args);
            PrimaryDataCruncher.CrunchPrimaryData(args);

            /*/if (args[0] == "--chess")
            {
                sourcePath = "ChessData/";
                outputPath = "ChessData/";
                type = GameType.Chess;
                tableBaseName = "Chess_data_";
            }
            else if (args[0] == "--cotg")
            {
                sourcePath = "CotGData/";
                outputPath = "CotGData/";
                type = GameType.CotG;
                tableBaseName = "CotG_data_";
            }
            else if (args[0] == "--microrts")
            {
                sourcePath = "MicroRTSData/";
                outputPath = "MicroRTSData/";
                type = GameType.MicroRTS;
                tableBaseName = "MicroRTS_data_";
            }
            else
            {
                throw new Exception($"Unknown game type parameter: {args[0]}");
            }

            if (args[1] == "-p")
            {
                PrimaryDataCruncher.CrunchPrimaryData(args);
            }

            if (args[1] == "-s")
            {
                SecondaryDataCruncher.CrunchSecondaryData(args);
            }

            if (args[1] == "-c")
            {
                CheckmateTestDataCruncher.CrunchCheckmateTestData(args);
            }/**/
        }
    }
}