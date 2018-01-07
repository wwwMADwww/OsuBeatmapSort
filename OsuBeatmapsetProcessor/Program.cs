using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            
            try
            {
                var process = new Bootstrap().GetBeatmapsetProcess();

                process?.Process();
            }
            catch (Exception e)
            {
                var _logger = LogManager.GetLogger("logger");
                _logger.Error(e.ToString());
            }

            Console.WriteLine("Press enter to exit;");
            Console.ReadLine();

        }
        

    }

}
