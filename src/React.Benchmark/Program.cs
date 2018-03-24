using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;

namespace React.Benchmark
{
	class Program
	{
		public static void Main()
		{
			//new Benchmarks().CreateComponent();
			BenchmarkRunner.Run<Benchmarks>();
			Console.ReadKey();
		}
	}

	[MemoryDiagnoser]
	[InProcess]
	public class Benchmarks
	{
		public Benchmarks()
		{
			Initializer.Initialize(options => options.AsSingleton());

			var environment = new ReactEnvironment(new BenchJavaScriptEngineFactory(), new ReactSiteConfiguration(), new NullCache(), new SimpleFileSystem(), new FileCacheHash());
			AssemblyRegistration.Container.Register((IReactEnvironment)environment);
		}

		[Benchmark]
		public void CreateComponent()
		{
			ReactEnvironment.GetCurrentOrThrow.CreateComponent("_some.Test", new { });
		}

		public void ReactSmall()
		{

		}

		public void ReactBig()
		{

		}

		public void RenderJavaScriptSmall()
		{

		}

		public void RenderJavaScriptBig()
		{

		}
	}
}
