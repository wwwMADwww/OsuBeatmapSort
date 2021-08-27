using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using NLog;

namespace OsuBeatmapSort
{
    class Program
    {
        static void Main(string[] args)
        {
            
            try
            {
                var process = new Bootstrap().GetBeatmapsetProcess();

                process.Process().Wait();
            }
            catch (Exception e)
            {
                var logger = LogManager.GetLogger("logger");
                logger.Error(e.ToString());
                throw;
            }

            Console.WriteLine("Press enter to exit;");
            Console.ReadLine();

        }
        

    }

}
