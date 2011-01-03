// 
// Main.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Reflection;
using System.IO;

namespace Benchmark
{
	class Driver
	{
		public static int Main (string[] args)
		{
			var variant = "Simd";
			if (args.Length > 0)
				variant = args[0];
			
			var thisDir = Path.GetDirectoryName (typeof (Driver).Assembly.Location);
			var rootDir = Path.GetDirectoryName (Path.GetDirectoryName (Path.GetDirectoryName (thisDir)));
			var binDir = Path.Combine (Path.Combine (rootDir, "Mono.GameMath"), "bin");
			var mathAssembly = Path.Combine (binDir, Path.Combine (variant, "Mono.GameMath.dll"));
			try {
				Assembly.LoadFile (mathAssembly);
			} catch {
				System.Console.Error.WriteLine ("Assembly variant '{0}' not found", variant);
				return 1;
			}
			
			string type = null;
			string method = null;
			if (args.Length > 1 && args[1] != "*")
				type = args[1];
			if (args.Length > 2)
				method = args[2];
			
			return RunTests (type, method);
		}
		
		//this has to be a separate method so it gets JITed after we load the Mono.GameMath.dll assembly
		static int RunTests (string type, string method)
		{
			Type[] testTypes;
			if (type != null) {
				try {
					testTypes = new Type[] { typeof (Driver).Assembly.GetType ("Benchmark." + type, true) };
				} catch {
					System.Console.Error.WriteLine ("Test type '{0}' not found", type);
					return 2;
				}
			} else {
				testTypes = new Type[] {
					typeof (MatrixTest),
					typeof (Vector4Test),
					typeof (Vector3Test),
					typeof (Vector2Test),
				};
			}
			
			int times =10 * 1000 * 1000 ;
			var sw = new System.Diagnostics.Stopwatch ();
			long total = 0;
			
			foreach (var t in testTypes) {
				MethodInfo[] methodInfos;
				if (method != null) {
					methodInfos = new MethodInfo[] { t.GetMethod (method, BindingFlags.Static | BindingFlags.Public) };
					if (methodInfos[0] == null) {
						System.Console.WriteLine ("{0}.{1}: NotFound", t.Name, method);
						continue;
					}
				} else {
					methodInfos = t.GetMethods (BindingFlags.Static | BindingFlags.Public);
				}
				
				Action<int>[] methods = new Action<int> [methodInfos.Length];
				for (int i = 0; i < methodInfos.Length; i++) {
					methods[i] = (Action<int>) Delegate.CreateDelegate (typeof (Action<int>), methodInfos[i]);
					try {
						methods[i] (1);
					} catch (NotImplementedException) {
						methods[i] = null;
					}
				}
				
				for (int i = 0; i < methodInfos.Length; i++) {
					if (methods[i] == null) {
						System.Console.WriteLine ("{0}.{1}: NotImplemented", t.Name, methodInfos[i].Name);	
						continue;
					}
					sw.Reset ();
					sw.Start ();
					methods[i] (times);
					sw.Stop ();
					System.Console.WriteLine ("{0}.{1}: {2}", t.Name, methodInfos[i].Name, sw.ElapsedMilliseconds);
					total += sw.ElapsedMilliseconds;
				}
			}
			
			System.Console.WriteLine ("Total: {0}", total);
			return 0;
		}
	}
}
