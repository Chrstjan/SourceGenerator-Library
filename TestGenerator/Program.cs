namespace TestGenerator
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var httpClient = new HttpClient();
			var client = new UserApiClient(httpClient);

			Console.WriteLine("Fetching user 1...");
			var user = client.GetUserByIdAsync(1).GetAwaiter().GetResult();

			Console.WriteLine($"Got user: {user.Title} ({user.Body})");

			Console.ReadKey();
		}
	}
}
