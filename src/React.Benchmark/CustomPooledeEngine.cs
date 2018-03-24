using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JavaScriptEngineSwitcher.Core;
using JSPool;

namespace React.Benchmark
{
    class CustomPooledeEngine : PooledJsEngine
	{
		public override IJsEngine InnerEngine => new BenchEngine();

		public override void Dispose()
		{
		}
	}

	public class BenchEngine : IJsEngine
	{
		public void Dispose()
		{
		}

		public object Evaluate(string expression)
		{
			throw new NotImplementedException();
		}

		public object Evaluate(string expression, string documentName)
		{
			throw new NotImplementedException();
		}

		public T Evaluate<T>(string expression)
		{
			if (typeof(T) == typeof(bool))
			{
				return (T)(object)true;
			}

			return (T)(object)expression;
		}

		public T Evaluate<T>(string expression, string documentName)
		{
			throw new NotImplementedException();
		}

		public void Execute(string code)
		{
			throw new NotImplementedException();
		}

		public void Execute(string code, string documentName)
		{
			throw new NotImplementedException();
		}

		public void ExecuteFile(string path, Encoding encoding = null)
		{
			throw new NotImplementedException();
		}

		public void ExecuteResource(string resourceName, Type type)
		{
			throw new NotImplementedException();
		}

		public void ExecuteResource(string resourceName, Assembly assembly)
		{
			throw new NotImplementedException();
		}

		public object CallFunction(string functionName, params object[] args)
		{
			throw new NotImplementedException();
		}

		public T CallFunction<T>(string functionName, params object[] args)
		{
			throw new NotImplementedException();
		}

		public bool HasVariable(string variableName)
		{
			if (variableName == "_ReactNET_UserScripts_Loaded")
			{
				return true;
			}

			return false;
		}

		public object GetVariableValue(string variableName)
		{
			throw new NotImplementedException();
		}

		public T GetVariableValue<T>(string variableName)
		{
			throw new NotImplementedException();
		}

		public void SetVariableValue(string variableName, object value)
		{
			throw new NotImplementedException();
		}

		public void RemoveVariable(string variableName)
		{
			throw new NotImplementedException();
		}

		public void EmbedHostObject(string itemName, object value)
		{
			throw new NotImplementedException();
		}

		public void EmbedHostType(string itemName, Type type)
		{
			throw new NotImplementedException();
		}

		public void CollectGarbage()
		{
			throw new NotImplementedException();
		}

		public string Name { get; }
		public string Version { get; }
		public bool SupportsGarbageCollection { get; }
	}
}
