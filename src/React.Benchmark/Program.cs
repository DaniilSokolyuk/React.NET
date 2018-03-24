using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using React.AspNet;
using React.Core;

namespace React.Benchmark
{
	class Program
	{
		public static void Main()
		{
			//BenchmarkRunner.Run<BenchmarksSmall>();
			BenchmarkRunner.Run<BenchmarksLarge>();
			Console.ReadKey();
		}
	}

	[MemoryDiagnoser]
	[InProcess]
	public class BenchmarksSmall
	{
		private static PagedPooledTextWriter tw = new PagedPooledTextWriter();

		private static object obj = Enumerable.Range(0, 30).ToDictionary(i => i, i => new string('5', 50));

		[GlobalSetup]
		public void GlobalSetup()
		{
			Initializer.Initialize(options => options.AsSingleton());
			var environment = new ReactEnvironment(new BenchJavaScriptEngineFactory(), new ReactSiteConfiguration(), new NullCache(), new SimpleFileSystem(), new FileCacheHash());
			AssemblyRegistration.Container.Register((IReactEnvironment)environment);

			for (int i = 0; i < 20; i++)
			{
				HtmlHelperExtensions.React(null, "some.comp", obj).WriteTo(tw, HtmlEncoder.Default);
			}

			tw.Clear();
		}

		[Benchmark]
		public void CreateComponent()
		{
			ReactEnvironment.GetCurrentOrThrow.CreateComponent("_some.Test", obj);
		}

		[Benchmark]
		public void React()
		{
			tw.Clear();
			HtmlHelperExtensions.React(null, "some.comp", obj).WriteTo(tw, HtmlEncoder.Default);
		}

		[Benchmark]
		public void ReactInitJavaScript()
		{
			tw.Clear();
			HtmlHelperExtensions.ReactInitJavaScript(null).WriteTo(tw, HtmlEncoder.Default);
		}
	}

	[MemoryDiagnoser]
	[InProcess]
	public class BenchmarksLarge
	{
		private static PagedPooledTextWriter tw = new PagedPooledTextWriter();

		private static object obj = Enumerable.Range(0, 100).ToDictionary(i => i, i => new string('5', 50));

		[GlobalSetup]
		public void GlobalSetup()
		{
			var environment = new ReactEnvironment(new BenchJavaScriptEngineFactory(), new ReactSiteConfiguration(), new NullCache(), new SimpleFileSystem(), new FileCacheHash());
			AssemblyRegistration.Container.Register((IReactEnvironment)environment);

			for (int i = 0; i < 20; i++)
			{
				HtmlHelperExtensions.React(null, "some.comp", obj).WriteTo(tw, HtmlEncoder.Default);
			}

			tw.Clear();
		}

		[Benchmark]
		public void CreateComponent()
		{
			ReactEnvironment.GetCurrentOrThrow.CreateComponent("_some.Test", obj);
		}

		[Benchmark]
		public void React()
		{
			tw.Clear();
			HtmlHelperExtensions.React(null, "some.comp", obj).WriteTo(tw, HtmlEncoder.Default);
		}

		[Benchmark]
		public void ReactInitJavaScript()
		{
			tw.Clear();
			HtmlHelperExtensions.ReactInitJavaScript(null).WriteTo(tw, HtmlEncoder.Default);
		}
	}
}
