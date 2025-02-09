﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using CsConsole.Command;
using CsConsole.CustomConsole;
using CsConsole.Configuration;

namespace CsConsole
{
    class Program
    {
        private readonly string[] StandardImports =
        {
            "System",
            "System.Linq",
            "System.Threading.Tasks",
            "System.Linq"
        };

        public const string Version = "1.0.0";

        public CommandExecutor Executor { get; set; }

        public static CommandConfigureManager Manager { get; set; }
    
        static void Main(string[] args)
        {
            var prog = new Program();

            prog.Index();
        }

        public void Index()
        {
            Init();

            Console.WriteLine("**C# Analysis Console**");
            Console.WriteLine($"C# Version : {Utils.GetVersion(Manager.Configure.Configure.Version)} | Current Version : {Version}");

            while (true)
            {
                Console.Write("C# > ");

                // var cmd = ConsoleReader.ReadLine();
                var cmd = Console.ReadLine();

                if (cmd.StartsWith("`"))
                {
                    Executor.RunCommand(cmd);
                    continue;
                }

                var result = Executor.Run(cmd).GetAwaiter().GetResult();

                if (result.State == ResultState.Ok)
                    Console.WriteLine(result.Result);
                else if (result.State == ResultState.Error)
                    Console.WriteLine(ExceptionToString(result.Command, result.Exception));
                    //Console.WriteLine($"{result.Exception.GetType().ToString()} {result.Exception.Message}");
            }

            string ExceptionToString(string cmd, Exception ex)
            {
                string e = ex.GetType().ToString();

                var ind = ex.Message.IndexOf("error CS") + 8;
                var code = int.Parse(ex.Message[ind..(ind + 4)]);

                if (code == 234)
                    return $"{ex.GetType().ToString()} {ex.Message}";

                var tmp = ex.Message.Split(')')[0][1..].Split(',');
                (int Y, int X) loc = (int.Parse(tmp[0]), int.Parse(tmp[1]));

                var l = loc.X - 5;
                l = l - 1 < 0 ? 0 : l - 1;
                var len = Math.Min(cmd.Length - l, 20);

                string src = cmd.Substring(l, len);

                string msg = ex.Message.Split("error")[1].Trim();

                return $"{e}({loc.X},{loc.Y})\n\n{src}\n{" ".Multiple(loc.X - l - 1)}^\n\n{msg}";
            }
        }

        public void Init()
        {
            string config = AppDomain.CurrentDomain.BaseDirectory + "/config.cf";

            if (!File.Exists(config))
                File.Create(config).Close();

            var json = File.ReadAllText(config);
            
            Manager = new CommandConfigureManager(config,
                                string.IsNullOrWhiteSpace(json) ?
                                new CommandConfigure()
                                {
                                    Configure = new CommandExecutorConfigure()
                                    {
                                        Imports = StandardImports,
                                        Version = LanguageVersion.CSharp9
                                    }
                                } : JsonConvert.DeserializeObject<CommandConfigure>(json));
            
            Executor = new CommandExecutor();

            Console.Clear();
        }
    }
}
