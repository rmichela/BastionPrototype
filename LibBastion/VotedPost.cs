using System.Collections.Generic;

namespace LibBastion
{
    public class VotedPost : Post
    {
        public List<Vote> Votes { get; set; }
    }
}
