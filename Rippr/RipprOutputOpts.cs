namespace Rippr
{
    public class RipprOutputOpts
    {
        public string ISOOutputPath { get; internal set; }
        public string MovieOutputPath { get; internal set; }
        public string TVOutputPath { get; internal set; }
        public string MusicOutputPath { get; internal set; }

        public static RipprOutputOpts getDefault()
        {
            var outputOpts = new RipprOutputOpts();
            outputOpts.ISOOutputPath = @"C:\ProgramData\Rips\Output\ISOs";
            outputOpts.MovieOutputPath = @"C:\ProgramData\Rips\Output\Movies";
            outputOpts.TVOutputPath = @"C:\ProgramData\Rips\Output\Television";
            outputOpts.MusicOutputPath = @"C:\ProgramData\Rips\Output\Music";
            return outputOpts;
        }
    }
}