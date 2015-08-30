using LibBastion;
using LibGit2Sharp;
using Newtonsoft.Json;
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
            var repoDir = new DirectoryInfo(args[0]);
            ConsoleWriteLine(ConsoleColor.Yellow, "Using Bastion {0}", repoDir);
            var bastion = new Bastion(repoDir);

            if (!repoDir.Exists)
            {
                Init(bastion);
            }

            string c = null;
            while (c != "0")
            {
                c = ConsolePrompt(ConsoleColor.White, "1: Post, 2: List, 3: Get, 4: Vote");

                switch (c)
                {
                    case "1": NewPost(bastion); break;
                    case "2": ListPosts(bastion); break;
                    case "3": GetPost(bastion); break;
                    case "4": PostVote(bastion); break;
                }
            }
        }

        static void Init(Bastion bastion)
        {
            string name = ConsolePrompt(ConsoleColor.White, "Bastion name");
            string ownerName = ConsolePrompt(ConsoleColor.White, "Owner's name");

            bastion.Init(new DeclarationOfExistence
            {
                Name = "The Bastion about Bastion",
                Owner = new Identity
                {
                    Name = name,
                    Identifier = IdentifierForName(name).ToString()
                }
            });
        }

        static void NewPost(Bastion bastion)
        {
            string title = ConsolePrompt(ConsoleColor.White, "Post title");
            string text = ConsolePrompt(ConsoleColor.White, "Post text");
            string link = ConsolePrompt(ConsoleColor.White, "Post link");
            string author = ConsolePrompt(ConsoleColor.White, "Post author");

            bastion.NewPost(new Post
            {
                Title = title,
                Text = text,
                Link = string.IsNullOrWhiteSpace(link) ? null : new Uri(link),
                Author = new Identity
                {
                    Name = author,
                    Identifier = IdentifierForName(author).ToString()
                },
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        static void ListPosts(Bastion bastion)
        {
            foreach (var post in bastion.ListPosts())
            {
                ConsoleWrite(ConsoleColor.White, post.Id);
                ConsoleWrite(ConsoleColor.Green, " {0}", post.Title);
                ConsoleWriteLine(ConsoleColor.Yellow, " {0} <{1}>", post.Author.Name, post.Author.Identifier);
            }
        }

        static void GetPost(Bastion bastion)
        {
            string id = ConsolePrompt(ConsoleColor.White, "ID");
            Post p = bastion.GetPost(id);
            ConsoleWriteLine(ConsoleColor.Gray, JsonConvert.SerializeObject(p, Formatting.Indented));
        }

        static void PostVote(Bastion bastion)
        {
            string id = ConsolePrompt(ConsoleColor.White, "ID");
            Post p = bastion.GetPost(id);
            VoteType v = ConsolePrompt(ConsoleColor.White, "U/D") == "U" ? VoteType.Upvote : VoteType.Downvote;
            string name = ConsolePrompt(ConsoleColor.White, "Post author");
            Identity voter = new Identity
            {
                Name = name,
                Identifier = IdentifierForName(name).ToString()
            };

            bastion.Vote(p, voter, v);
        }

        static void ConsoleWriteLine(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static void ConsoleWriteLine(ConsoleColor color, string format)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(format);
            Console.ResetColor();
        }

        static void ConsoleWrite(ConsoleColor color, string format)
        {
            Console.ForegroundColor = color;
            Console.Write(format);
            Console.ResetColor();
        }

        static void ConsoleWrite(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.Write(format, args);
            Console.ResetColor();
        }

        static string ConsolePrompt(ConsoleColor color, string format, params object[] args)
        {
            Console.ForegroundColor = color;
            Console.Write(format + " > ", args);
            Console.ResetColor();
            return Console.ReadLine();
        }

        static Guid IdentifierForName(string name)
        {
            var r = new Random(name.GetHashCode());
            var b = new byte[16];
            r.NextBytes(b);
            return new Guid(b);
        }
    }
}
