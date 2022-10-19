// See https://aka.ms/new-console-template for more information
using SL.Console;
using SL.Shared;

var rootDir = "D:\\code\\C#\\Slitherlink\\";
// var game = GameLoader.Load(new StreamReader("D:\\code\\C#\\Slitherlink/SquareSmallEasy53.txt"));

foreach (var filePath in Directory.GetFiles(rootDir, "*.csv"))
{
    Console.WriteLine(filePath);
    var game = new Game(GameLoader.ReadCsv(new StreamReader(filePath)));
    /*
    ConsoleDisplay.ShowBoard(game.Board);
    System.Console.WriteLine();
    */

    try
    {
        // Run  few times to get better profiling data
        for (var i = 0; i < 5; i++)
        {
            game.Reset();
            Solver.SolveWithLookAhead(game);
        }
    }
    catch (Exception ioe)
    {
        Console.WriteLine(ioe);
    }

    ConsoleDisplay.ShowBoard(game.Board);
    System.Console.WriteLine();
}