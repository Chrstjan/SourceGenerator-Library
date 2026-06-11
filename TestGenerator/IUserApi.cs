using System;
using System.Collections.Generic;
using System.Text;
using SourceGenerator_Library;

namespace TestGenerator
{
	[GenerateApiClient("https://jsonplaceholder.typicode.com/")]
	public interface IUserApi
	{
		Task<Post> GetUserByIdAsync(int id);
	}
}
