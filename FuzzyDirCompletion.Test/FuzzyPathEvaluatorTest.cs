using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FuzzyDirCompletion.Test
{
	[TestClass]
	public class FuzzyPathEvaluatorTest
	{
		private FuzzyPathEvaluator lp;

		[TestInitialize]
		public void InitTests()
		{
			lp = new FuzzyPathEvaluator();
		}

		[TestMethod]
		public void FindDirPathsTest()
		{
			var res = lp.FindPaths("", @"..\..\..\tf/sd");

			Assert.AreEqual(res.Length, 2);
			Assert.IsTrue(res[0].EndsWith("SimpleDir"));
			Assert.IsTrue(res[1].EndsWith("alllcasedir"));
		}

		[TestMethod]
		public void FindFilePathsTest()
		{
			var res = lp.FindPaths("", @"..\..\..\tf/ft");

			Assert.AreEqual(res.Length, 2);
			Assert.IsTrue(res[0].EndsWith("FileTwo.txt"));
			Assert.IsTrue(res[1].EndsWith("File.txt"));
		}

		[TestMethod]
		public void GetSubdirectoryNamesTest()
		{
			var res = lp.GetSubdirectoryNames(@"..\..\..\TestFolders", "*i*");

			Assert.AreEqual(res.Length, 3);
		}
	}
}
