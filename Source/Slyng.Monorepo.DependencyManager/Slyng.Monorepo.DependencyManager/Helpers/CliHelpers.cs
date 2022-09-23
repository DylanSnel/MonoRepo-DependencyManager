using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slyng.Monorepo.DependencyManager.Helpers
{
    public static class Cli
    {

        public static T AskFor<T>(string Question, string originalValue= null)
        {
            if (originalValue != null)
            {
                Console.WriteLine($"{Question}: [{originalValue}]");
            }
            else
            {
                Console.WriteLine($"{Question}: ");
            }
            var input = Console.ReadLine();
            if (input == "" && originalValue != null)
            {
                return (T)Convert.ChangeType(originalValue, typeof(T));
            }

            return (T)Convert.ChangeType(input, typeof(T));
        }

        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{title} [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }
    }
}
