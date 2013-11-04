using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Management.Automation;
using FuzzyDirCompletion;

namespace FuzzyPathCompletion
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	///   Lazy/fuzzy path evaluator cmdlet. Wraps FuzzyPathEvaluator for use in PowerShell.
	/// </summary>
	///
	/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
	/// 
	////////////////////////////////////////////////////////////////////////////////////////////////////
	[Cmdlet(VerbsCommon.Get, "FuzzyPath")]
	public class FuzzyPathCmdlet : Cmdlet
	{
		#region Parameters

		[Parameter(Position = 0, Mandatory = true)]
		public string PathQuery
		{
			get { return pathQuery; }
			set { pathQuery = value; }
		}
		private string pathQuery;

		[Parameter(Position = 1)]
		public string StartPath
		{
			get { return startPath; }
			set { startPath = value; }
		}
		private string startPath = Environment.CurrentDirectory;

		#endregion


		#region Member Variables

		FuzzyPathEvaluator lp = new FuzzyPathEvaluator();

		#endregion


		protected override void ProcessRecord()
		{
			WriteObject(CallPathEvaluator(this.StartPath, this.PathQuery));
		}

		private string[] CallPathEvaluator(string startPath, string pathQuery)
		{
			return lp.FindPaths(startPath, pathQuery);
		}
	}
}