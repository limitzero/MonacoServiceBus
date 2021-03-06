﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;

namespace Monaco.Visualizations.Providers
{
	internal class SVGGenerateDotEngine : IDotEngine
	{
		private Stream standardOutput;
		private MemoryStream memoryStream = new MemoryStream();
		private byte[] buffer = new byte[4096];

		public string Run(GraphvizImageType imageType, string dot, string outputFileName)
		{
			using (Process process = new Process())
			{
				//We'll launch dot.exe in command line
				process.StartInfo.FileName = @"C:\Program Files\Graphviz 2.28\bin\dot.exe";
				//Let's give the type we want to generate to, and a charset
				//to support accent
				process.StartInfo.Arguments = "-Tsvg -Gcharset=latin1";
				process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

				//We'll receive the bitmap thru the standard output stream
				process.StartInfo.RedirectStandardOutput = true;
				//We'll need to give the dot structure in the standard input stream
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.UseShellExecute = false;
				process.Start();
				standardOutput = process.StandardOutput.BaseStream;
				standardOutput.BeginRead(buffer, 0, buffer.Length, StandardOutputReadCallback, null);
				//Let's sent the dot structure and close the stream to send the data and tell
				//we won't give any more
				process.StandardInput.Write(dot);
				process.StandardInput.Close();
				//Wait the process is finished and get back the image (binary format)
				process.WaitForExit();
				var result = Encoding.Default.GetString(memoryStream.ToArray());
				return result;
			}
		}

		private void StandardOutputReadCallback(IAsyncResult result)
		{
			int numberOfBytesRead = standardOutput.EndRead(result);
			memoryStream.Write(buffer, 0, numberOfBytesRead);

			// Read next bytes.   
			standardOutput.BeginRead(buffer, 0, buffer.Length, StandardOutputReadCallback, null);
		}
	}
}