namespace Rippr
{
    public class RipprOptions
    {
        public bool IsDebugMode { get; internal set; }
        public bool ShouldShowHelp { get; internal set; }
        public bool IsBatchMode { get; internal set; }
        public RipprPathInfo CDPathInfo { get; internal set; }
        public RipprPathInfo DVDPathInfo { get; internal set; }
        public RipprPathInfo BluRayPathInfo { get; internal set; }
        public string OmdbApiKey { get; internal set; }

        public static RipprOptions getDefault()
        {
            var ripprOpts = new RipprOptions();
            ripprOpts.IsDebugMode = false;
            ripprOpts.ShouldShowHelp = false;
            ripprOpts.IsBatchMode = false;
            ripprOpts.CDPathInfo = RipprPathInfo.getDefault();
            ripprOpts.DVDPathInfo = RipprPathInfo.getDefault();
            ripprOpts.BluRayPathInfo = RipprPathInfo.getDefault();
            ripprOpts.OmdbApiKey = "";
            return ripprOpts;
        }
    }
}
