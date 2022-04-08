namespace Rippr
{
    public class RipprPathInfo
    {
        public string OutputPath { get; internal set; }
        public string InputPath { get; internal set; }
        public string RipperExePath { get; internal set; }
        public string RipperExeOpts { get; internal set; }

        public static RipprPathInfo getDefault()
        {
            var ripprPathInfo = new RipprPathInfo();
            ripprPathInfo.InputPath = @"C:\ProgramData\Rips\Input";
            ripprPathInfo.OutputPath = @"C:\ProgramData\Rips\Output";
            ripprPathInfo.RipperExePath = @"C:\Program Files (x86)\MakeMKV\makemkvcon64.exe";
            ripprPathInfo.RipperExeOpts = @"""{0}"" --minlength={1} -r --decrypt --directio=true mkv disc:{2} all ""{3}""";
            return ripprPathInfo;
        }
    }
}