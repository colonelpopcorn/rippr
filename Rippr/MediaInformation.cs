﻿namespace Rippr
{
    public class MediaInformation
    {
        public string Type { get; internal set; }
        public string Title { get; internal set; }
        public string Year { get; internal set; }
        public int[] EpisodeNumbers { get; internal set; }
        public string Runtime { get; internal set; }
    }
}