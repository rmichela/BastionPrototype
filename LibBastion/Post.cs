using System;

namespace LibBastion
{
    public class Post
    {
        public string Title { get; set; }

        public string Text { get; set; }

        public Uri Link { get; set; }

        public Identity Author { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string Id { get; set; }
    }
}
