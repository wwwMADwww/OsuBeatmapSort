using CommandLine;
using OsuBeatmapsetProcessor.MovePlayedBeatmapset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuBeatmapsetProcessor
{
    public class Bootstrap
    {

        public IBeatmapsetProcess GetBeatmapsetProcess()
        {

            IOptions options = null;
            if (!ProcessParams(out options) || options == null)
                return null;

            IBeatmapsetProcessFactory factory = null;

            if (options is MovePlayedBeatmapsetProcessOptions)
                factory = new MovePlayedBeatmapsetProcessFactory((MovePlayedBeatmapsetProcessOptions) options);

            // else if (options is )
            //   factory = 

            var process = factory.Create();

            return process;
        }
        
        bool ProcessParams(out IOptions options)
        {
            String[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            IOptions optbuf = null;
            var result = CommandLine.Parser.Default.ParseArguments<Options, MovePlayedBeatmapsetProcessOptions>(args);

            var res = result.MapResult
            ( 
                (Options opt) => true,
                (MovePlayedBeatmapsetProcessOptions opt) =>
                {
                    optbuf = opt;
                    return true;
                },
                errors => false
            );
            options = optbuf;
            return res;
        }


    }

    interface IOptions { }


    [Verb("Stub")]
    class Options: IOptions { }

}
