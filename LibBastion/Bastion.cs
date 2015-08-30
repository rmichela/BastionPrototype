using LibGit2Sharp;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Collections.Generic;

namespace LibBastion
{
    public class Bastion
    {
        public const string EMPTY_TREE = "EmptyTree";
        public const string UPVOTE = "Upvote";
        public const string DOWNVOTE = "Downvote";
        public const string CONTROL_BRANCH = "Control";
        public const string CONTENT_BRANCH = "Content";
        public const string VOTES_DIR = "Votes";
        public const string REPLIES_DIR = "Replies";

        private DirectoryInfo _directory;

        public Bastion(DirectoryInfo directory)
        {
            _directory = directory;
        }

        public void Init(DeclarationOfExistence doe)
        {
            if (!_directory.Exists)
            {
                _directory.Create();
            }
            if (!_directory.GetFiles().Any() && !_directory.GetDirectories().Any())
            {
                // Directory is empty
                Repository.Init(_directory.FullName, isBare: true);
                using (var repo = new Repository(_directory.FullName))
                {
                    InitCoreTags(repo);
                    Commit doeCommit = CreateDeclarationOfExistence(repo, doe);
                    repo.CreateBranch(CONTROL_BRANCH, doeCommit);
                    repo.CreateBranch(CONTENT_BRANCH, doeCommit);
                }
            }
            else
            {
                throw new BastionException(string.Format("{0} has already been initialized", _directory.FullName));
            }
        }

        private void InitCoreTags(Repository repo)
        {
            // Create tags for Upvote, Downvote, and EmptyTree
            Tree emptyTree = repo.ObjectDatabase.CreateTree(new TreeDefinition());
            repo.ApplyTag(EMPTY_TREE, emptyTree.Sha);

            Blob upvote = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.ASCII.GetBytes(UPVOTE)));
            repo.ApplyTag(UPVOTE, upvote.Sha);

            Blob downvote = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.ASCII.GetBytes(DOWNVOTE)));
            repo.ApplyTag(DOWNVOTE, downvote.Sha);
        }

        private Commit CreateDeclarationOfExistence(Repository repo, DeclarationOfExistence doe)
        {
            var sig = new Signature(doe.Owner.Name, doe.Owner.Identifier, DateTimeOffset.UtcNow);
            var json = JsonConvert.SerializeObject(doe);
            return repo.ObjectDatabase.CreateCommit(sig, sig, json, EmptyTree(repo), new List<Commit>(), prettifyMessage: true);
        }

        public void NewPost(Post post)
        {
            using (var repo = new Repository(_directory.FullName))
            {
                var json = JsonConvert.SerializeObject(post);
                var sig = new Signature(post.Author.Name, post.Author.Identifier, post.Timestamp);

                // Create post structure
                var votesDir = repo.ObjectDatabase.CreateTree(new TreeDefinition());
                var repliesDir = repo.ObjectDatabase.CreateTree(new TreeDefinition());
                var postRoot = new TreeDefinition();
                postRoot.Add(VOTES_DIR, votesDir);
                postRoot.Add(REPLIES_DIR, repliesDir);
                
                var commit = CommitToBranch(repo, CONTENT_BRANCH, json, sig, repo.ObjectDatabase.CreateTree(postRoot));
                // Create a named branch for all future content on this post
                repo.CreateBranch(commit.Sha, commit);
            }
        }

        public List<Post> ListPosts()
        {
            using (var repo = new Repository(_directory.FullName))
            {
                var posts = new List<Post>();

                // Get the most recent post from the content branch
                Branch branch = repo.Branches[CONTENT_BRANCH];
                DirectReference commitRef = repo.Refs[branch.CanonicalName].ResolveToDirectReference();

                for (Commit commit = repo.Lookup<Commit>(commitRef.Target.Id); commit.Parents.Any(); commit = commit.Parents.First())
                {
                    var post = JsonConvert.DeserializeObject<Post>(commit.Message);
                    post.Id = commit.Sha;
                    posts.Add(post);
                }

                return posts;
            }
        }

        public VotedPost GetPost(string id)
        {
            using (var repo = new Repository(_directory.FullName))
            {
                Commit commit = repo.Lookup<Commit>(id);
                var post = JsonConvert.DeserializeObject<VotedPost>(commit.Message);
                post.Id = commit.Sha;

                // Get the votes for the post
                Commit postTip = repo.Branches[post.Id].Tip;
                Tree postTree = postTip.Tree;
                Tree votesDir = (Tree)postTree[VOTES_DIR].Target;
                post.Votes = votesDir.Where(f => f.Mode == Mode.NonExecutableFile).Select(DecodeVote).ToList();

                return post;
            }
        }

        public void Vote(Post post, Identity voter, VoteType vote)
        {
            using (var repo = new Repository(_directory.FullName))
            {
                var postCommit = repo.Branches[post.Id].Tip;

                // Retrieve existing tree
                var commitRoot = postCommit.Tree;
                var votesDir = (Tree)commitRoot[VOTES_DIR].Target;
                var repliesDir = (Tree)commitRoot[REPLIES_DIR].Target;

                // Copy existing content to new votes treedef
                var newVotesDir = new TreeDefinition();
                foreach (TreeEntry obj in votesDir)
                {
                    newVotesDir.Add(obj.Name, obj);
                }
                // Add new vote to new votes treedef
                Vote(repo, newVotesDir, vote);

                // Assemble new root treedef
                var newPostRoot = new TreeDefinition();
                newPostRoot.Add(VOTES_DIR, repo.ObjectDatabase.CreateTree(newVotesDir));
                newPostRoot.Add(REPLIES_DIR, repliesDir);

                // Commit new root treedef to post branch
                var message = string.Format("{0} by {1}", vote, voter.Name);
                var sig = new Signature(voter.Name, voter.Identifier, DateTimeOffset.UtcNow);
                CommitToBranch(repo, post.Id, message, sig, repo.ObjectDatabase.CreateTree(newPostRoot));
            }
        }

        private void Vote(Repository repo, TreeDefinition votesDir, VoteType vote)
        {
            switch (vote)
            {
                case LibBastion.VoteType.Upvote:
                    votesDir.Add(UPVOTE, Upvote(repo), Mode.NonExecutableFile);
                    break;
                case LibBastion.VoteType.Downvote:
                    votesDir.Add(DOWNVOTE, Downvote(repo), Mode.NonExecutableFile);
                    break;
            }
        }

        private Vote DecodeVote(TreeEntry v)
        {
            // TODO: Decode voter identity
            switch (v.Name)
            {
                case UPVOTE:
                    return new Vote
                    {
                        Voter = null,
                        VoteType = VoteType.Upvote
                    };
                case DOWNVOTE:
                    return new Vote
                    {
                        Voter = null,
                        VoteType = VoteType.Downvote
                    };
                default:
                    throw new BastionException("Unknown vote type " + v.Name);
            }
        }

        public Commit CommitToBranch(Repository repo, string branchName, string message, Signature author, Tree tree)
        {
            Branch branch = repo.Branches[branchName];
            Commit commit = repo.ObjectDatabase.CreateCommit(author, author, message, tree, new List<Commit> { branch.Tip }, prettifyMessage: true);
            repo.Refs.UpdateTarget(repo.Refs[branch.CanonicalName], commit.Id);
            return commit;
        }

        private Stream ObjectToJsonStream(object obj)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented)));
        }

        private Tree EmptyTree(Repository repo)
        {
            return repo.Lookup<Tree>(repo.Tags[EMPTY_TREE].Target.Id);
        }

        private Blob Upvote(Repository repo)
        {
            return repo.Lookup<Blob>(repo.Tags[UPVOTE].Target.Id);
        }

        private Blob Downvote(Repository repo)
        {
            return repo.Lookup<Blob>(repo.Tags[DOWNVOTE].Target.Id);
        }
    }
}
