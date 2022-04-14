namespace Rippr
{
    public class RipprOptions
    {
        public bool IsDebugMode { get; internal set; }
        public bool ShouldShowHelp { get; internal set; }
        public bool IsBatchMode { get; internal set; }
        public RipprInputOpts CdInputOpts { get; internal set; }
        public RipprInputOpts DvdInputOpts { get; internal set; }
        public RipprInputOpts BluRayInputOpts { get; internal set; }
        public RipprOutputOpts OutputOpts { get; internal set; }
        public string OmdbApiKey { get; internal set; }

        public static RipprOptions getDefault()
        {
            var ripprOpts = new RipprOptions();
            ripprOpts.IsDebugMode = false;
            ripprOpts.ShouldShowHelp = false;
            ripprOpts.IsBatchMode = false;
            ripprOpts.CdInputOpts = RipprInputOpts.getDefault("CD");
            ripprOpts.DvdInputOpts = RipprInputOpts.getDefault("DVD");
            ripprOpts.BluRayInputOpts = RipprInputOpts.getDefault("Blu-Ray");
            ripprOpts.OutputOpts = RipprOutputOpts.getDefault();
            ripprOpts.OmdbApiKey = "";
            return ripprOpts;
        }
    }
}