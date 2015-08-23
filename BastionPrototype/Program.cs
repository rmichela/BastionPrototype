using LibBastion;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bastiond
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (var repo = new Repository(@"C:\Users\delta\Documents\GitHub\Bastion\testbare"))
            //{
            //    var ms = new MemoryStream(Encoding.ASCII.GetBytes("My first blob " + Guid.NewGuid().ToString()));
            //    Blob blob = repo.ObjectDatabase.CreateBlob(ms);
            //    TreeDefinition td = new TreeDefinition();
            //    //td.Add("firstBlob.txt", blob, Mode.NonExecutableFile);
            //    Tree tree = repo.ObjectDatabase.CreateTree(td);

            //    Signature me = new Signature("Ryan", "deltahat@gmail.com", DateTimeOffset.Now);
            //    Branch master = repo.Branches["master"];
            //    Commit commit = repo.ObjectDatabase.CreateCommit(me, me, "Because I can", tree, new List<Commit>() { master.Tip }, true);

            //    repo.Refs.UpdateTarget(repo.Refs[master.CanonicalName], commit.Id);

            //    //repo.CreateBranch("master", c);

            //    Console.WriteLine(commit.Sha);
            //}

            var repoDir = new DirectoryInfo(args[0]);
            var bastion = new Bastion(repoDir);
            if (!repoDir.Exists)
            {
                bastion.Init(new DeclarationOfExistence
                {
                    Name = "The Bastion about Bastion",
                    Owner = "Ryan"
                });
            }
        }
    }
}
