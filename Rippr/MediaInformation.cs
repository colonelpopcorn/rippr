namespace Rippr
{
    public class MediaInformation
    {
        public string Type { get; internal set; }
        public string Title { get; internal set; }
        public string Year { get; internal set; }
        public int SeasonNumber { get; internal set; }
        public int DiscNumber { get; internal set; }
        public string Runtime { get; internal set; }

        public bool IsEmpty()
        {
            return Title == null && Year == null && Runtime == null;
        }
    }
}