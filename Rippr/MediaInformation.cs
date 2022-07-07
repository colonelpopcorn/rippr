namespace Rippr
{
    public class MediaInformation
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeStart { get; set; }
        public int EpisodeEnd { get; set; }
        public string Runtime { get; set; }

        public bool IsEmpty()
        {
            return Title == null && Year == null && Runtime == null;
        }
    }
}