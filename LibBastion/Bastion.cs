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
                CommitToBranch(repo, CONTENT_BRANCH, json, sig, EmptyTree(repo));
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

        public Post GetPost(string id)
        {
            using (var repo = new Repository(_directory.FullName))
            {
                Commit commit = repo.Lookup<Commit>(id);
                var post = JsonConvert.DeserializeObject<Post>(commit.Message);
                post.Id = commit.Sha;
                return post;
            }
        }

        public void Vote(string id, Vote vote)
        {

        }

        public void CommitToBranch(Repository repo, string branchName, string message, Signature author, Tree tree)
        {
            Branch branch = repo.Branches[branchName];
            Commit commit = repo.ObjectDatabase.CreateCommit(author, author, message, tree, new List<Commit> { branch.Tip }, prettifyMessage: true);
            repo.Refs.UpdateTarget(repo.Refs[branch.CanonicalName], commit.Id);
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
