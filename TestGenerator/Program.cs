using SourceGenerator_Library;

namespace TestGenerator
{
	internal class Program
	{
		static void Main(string[] args)
		{
			foreach (var className in ClassNames.Names) {
				Console.WriteLine(className);
			}

			Console.ReadKey();
		}
	}

	public class Foo { }
}
