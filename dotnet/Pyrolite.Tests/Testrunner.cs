/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Reflection;
using NUnit.Core;
using Pyrolite.Tests.Pickle;

namespace Pyrolite.Tests
{
	public class Testrunner
	{
		public static int Main(String[] args)
		{
		    CoreExtensions.Host.InitializeService();
		    SimpleTestRunner runner = new SimpleTestRunner();
		    TestPackage package = new TestPackage( "Test" );
		    
		    string loc= Assembly.GetAssembly(typeof(UnpickleStackTest)).Location;
		    Console.WriteLine("assembly="+loc);
		    package.Assemblies.Add( loc );

		    bool fail=true;
		    if( runner.Load(package) )
		    {
		    	Console.WriteLine("running tests");
			TestResult results = runner.Run (new MyListener(), new MyTestFilter(), false, LoggingThreshold.Debug);
		        fail=results.IsFailure;
		    }
		    
		    Console.WriteLine("press enter to exit");
		    Console.ReadLine();
		    
		    if(fail) return 10;
		    else return 0;
		}
	}

	class MyTestFilter : TestFilter
	{
		public override bool Match (ITest test)
		{
			return true;
		}
	}

	class MyListener : EventListener
	{
		TestName currentTest;
		int count;
		int failed;
		int skipped;
		
		public void RunStarted(string name, int testCount)
		{
			Console.WriteLine("run started, "+name+" count="+testCount);
			count=testCount;
		}
		
		public void RunFinished(TestResult result)
		{
			Console.WriteLine("\nNumber of tests: "+count+"  Failed: "+failed+"  Skipped: "+skipped);
			if(result.IsSuccess) {
				Console.WriteLine("ALL OK");
			} else {
				Console.WriteLine("TESTRUN FAILED: "+result.Message);
			}
		}
		
		public void RunFinished(Exception exception)
		{
			Console.WriteLine("testrun crashed, error="+exception);
		}
		
		public void TestStarted(TestName testName)
		{
			currentTest=testName;
		}

		public void TestFinished(TestResult result)
		{
			if(!result.Executed) {
				// skipped test
				Console.Write('S');
				skipped++;
			} else {
				if(result.IsFailure) {
					failed++;
					Console.WriteLine("\nTEST FAILED: "+currentTest.FullName);
					Console.WriteLine(result.Message);
					Console.WriteLine(result.StackTrace);
				} else {
					Console.Write('.');
				}
			} 
		}
		
		public void SuiteStarted(TestName testName)
		{
		}
		
		public void SuiteFinished(TestResult result)
		{
		}
		
		public void UnhandledException(Exception exception)
		{
			Console.WriteLine("error! "+exception);
		}
		
		public void TestOutput(TestOutput testOutput)
		{
			Console.WriteLine("output: "+testOutput);
		}
	}
}
