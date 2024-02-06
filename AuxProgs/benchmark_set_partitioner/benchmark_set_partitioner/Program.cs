// Takes a benchmark set with multiple battles and separates it into the given number of parts.

using System.IO;

int numOfPartitions = 58;

string bSetName = "Chess_2_mix_set";
int lineCount = 0;
string header = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<BenchmarkSet>\r\n";
string footer = "</BenchmarkSet>\r\n";

var sr = new StreamReader("..\\..\\..\\..\\..\\..\\Resources\\BenchmarkSets\\" + bSetName + ".xml");
while (sr.ReadLine() != null)
{
    ++lineCount;
}

sr.DiscardBufferedData();
sr.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
sr.ReadLine();
sr.ReadLine();
lineCount -= 3;
int part = 1;
int linesPerPart = lineCount / numOfPartitions;
int remainder = lineCount % numOfPartitions;

while (lineCount != 0)
{
    var sw = new StreamWriter("..\\..\\..\\..\\..\\..\\Resources\\BenchmarkSets\\" + bSetName + $"_Pt_{part}.xml");
    sw.Write(header);

    int bound = remainder <= 0 ? linesPerPart : linesPerPart + 1;
    for (int i = 0; i < bound; ++i)
    {
        sw.WriteLine(sr.ReadLine());
        --lineCount;
    }
    --remainder;

    sw.Write(footer);
    sw.Flush();

    ++part;
}