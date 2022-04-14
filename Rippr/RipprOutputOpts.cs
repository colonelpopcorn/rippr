namespace Rippr
{
    public class RipprOutputOpts
    {
        public string ISOOutputPath { get; internal set; }
        public string HDMovieOutputPath { get; internal set; }
        public string SDMovieOutputPath { get; internal set; }
        public string HDTVOutputPath { get; internal set; }
        public string SDTVOutputPath { get; internal set; }
        public string MusicOutputPath { get; internal set; }

        public static RipprOutputOpts getDefault()
        {
            var outputOpts = new RipprOutputOpts();
            outputOpts.ISOOutputPath = @"C:\ProgramData\Rips\Output\ISOs";
            outputOpts.HDMovieOutputPath = @"C:\ProgramData\Rips\Output\Movies";
            outputOpts.SDMovieOutputPath = @"C:\ProgramData\Rips\Output\Movies";
            outputOpts.HDTVOutputPath = @"C:\ProgramData\Rips\Output\Television";
            outputOpts.SDTVOutputPath = @"C:\ProgramData\Rips\Output\Television";
            outputOpts.MusicOutputPath = @"C:\ProgramData\Rips\Output\Music";
            return outputOpts;
        }
    }
}