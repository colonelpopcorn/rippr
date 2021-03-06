namespace Rippr
{
    public class RipprInputOpts
    {
        public string InputPath { get; internal set; }
        public string RipperExePath { get; internal set; }
        public string RipperExeOpts { get; internal set; }

        public static RipprInputOpts getDefault()
        {
            return RipprInputOpts.getDefault("Blu-Ray");
        }

        public static RipprInputOpts getDefault(string type)
        {
            var ripprPathInfo = new RipprInputOpts();
            ripprPathInfo.InputPath = @"C:\ProgramData\Rips\Input";
            if (type != "CD")
            {
                ripprPathInfo.RipperExePath = @"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe";
                ripprPathInfo.RipperExeOpts =
                    @"""{0}"" --minlength={1} -r --decrypt --directio=true mkv disc:{2} all ""{3}""";
            }
            else
            {
                ripprPathInfo.RipperExePath = @"C:\Program Files\AnyBurn\abcmd.exe";
                ripprPathInfo.RipperExeOpts = @"""{0}"" rip {1} -od ""{2}"" -ot flac";
            }

            return ripprPathInfo;
        }
    }
}