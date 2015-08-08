using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using VRage.Compiler;

namespace ScriptChecker
{
    class Program
    {
        private const int MAX_NUM_EXECUTED_INSTRUCTIONS = 50000;
		private const int MAX_NUM_METHOD_CALLS = 10000;
        
        const string CODE_WRAPPER_BEFORE = "using System;\nusing System.Collections.Generic;\nusing VRageMath;\nusing VRage.Game;\nusing System.Text;\nusing Sandbox.ModAPI.Interfaces;\nusing Sandbox.ModAPI.Ingame;\npublic class Program: MyGridProgram\n{\n";
        const string CODE_WRAPPER_AFTER = "\n}";

        static int Main(string[] args)
        {
            
            var scriptFileName = args.FirstOrDefault();
            if(String.IsNullOrEmpty(scriptFileName)) throw new ArgumentException("No filename specified.");
            if(!File.Exists(scriptFileName)) throw new ArgumentException("Input file does not exist.");

            var program = File.ReadAllText(scriptFileName);

            MySandboxGame.InitModAPI();
            Assembly temp = null;
            var compilerErrors = new List<string>();

            string finalCode = CODE_WRAPPER_BEFORE + program + CODE_WRAPPER_AFTER;

            if (!IlCompiler.CompileStringIngame("IngameScript.dll", new string[] { finalCode }, out temp, compilerErrors))
            {
                foreach(var error in compilerErrors) Console.WriteLine(error);
                Console.WriteLine("Compilation failed.");
                return 2;
            }

            if (temp == null) {
                Console.WriteLine("No assembly produced by compiler.");
                return 2;
            }

            var assembly = IlInjector.InjectCodeToAssembly("IngameScript_safe", temp, typeof (IlInjector).GetMethod("CountInstructions", BindingFlags.Public | BindingFlags.Static), typeof (IlInjector).GetMethod("CountMethodCalls", BindingFlags.Public | BindingFlags.Static), true);

            var type = assembly.GetType("Program");
            if (type == null)
            {
                Console.WriteLine("No Program class found.");
                return 3;
            }

            IlInjector.RestartCountingInstructions(MAX_NUM_EXECUTED_INSTRUCTIONS);
            IlInjector.RestartCountingMethods(MAX_NUM_METHOD_CALLS);

            var instance = (IMyGridProgram)Activator.CreateInstance(type);
            if (instance == null)
            {
                Console.WriteLine("No instance created.");
                return 4;
            }
            return 0;
        }
    }
}
