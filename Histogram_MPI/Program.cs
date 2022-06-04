﻿using Histogram_MPI;
using MPI;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        var stopWatch = Stopwatch.StartNew();
        using (new MPI.Environment(ref args))
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify file to analyze as command line argument!");
                return;
            }

            Intracommunicator comm = Communicator.world;

            string textPart;
            if (comm.Rank == 0)
            {
                // read the file
                // split file into list of n lists, where n is the comm.size - 1
                string filename = args[0];
                Console.WriteLine($"Analyzing: {filename}");

                string text = File.ReadAllText(filename);
                string[] parts = TextSplitter.SplitIntoChunks(text, comm.Size);
                textPart = comm.Scatter(parts);
            }
            else
            {
                textPart = comm.Scatter<string>(0);
            }

            Result result = AnalyzeTextPart(textPart);
            Result[] results = comm.Gather(result, 0);

            if (comm.Rank == 0)
            {
                Result finalResult = CombineResults(results);
                stopWatch.Stop();
                Console.WriteLine($"It took {stopWatch.Elapsed.TotalSeconds} seconds");

                //HistogramDisplay.Display(finalResult);
            }
        }

    }
    private static Result AnalyzeTextPart(string textPart)
    {
        TextCounter counter = new TextCounter();
        return counter.Count(textPart);
    }

    private static Result CombineResults(Result[] results)
    {
        Dictionary<char, int> characterCounts = new();
        Dictionary<string, int> wordCounts = new();

        characterCounts = results.SelectMany(r => r.CharacterCounts)
                                      .GroupBy(o => o.Key)
                                      .ToDictionary(o => o.Key, o => o.Sum(v => v.Value));
        wordCounts = results.SelectMany(r => r.WordCounts)
                                      .GroupBy(o => o.Key)
                                      .ToDictionary(o => o.Key, o => o.Sum(v => v.Value));

        return new Result(characterCounts, wordCounts);
    }
}