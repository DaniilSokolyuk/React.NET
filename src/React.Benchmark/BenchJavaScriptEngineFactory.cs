using System;
using System.Collections.Generic;
using System.Text;
using JavaScriptEngineSwitcher.Core;
using JSPool;

namespace React.Benchmark
{
    class BenchJavaScriptEngineFactory : IJavaScriptEngineFactory
    {
	    public IJsEngine GetEngineForCurrentThread()
	    {
		    throw new NotImplementedException();
	    }

	    public void DisposeEngineForCurrentThread()
	    {
		    throw new NotImplementedException();
	    }

	    public PooledJsEngine GetEngine()
	    {
		    return new CustomPooledeEngine();

	    }
    }
}
