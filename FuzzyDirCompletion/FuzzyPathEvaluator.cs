using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FuzzyDirCompletion
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	A class for evaluating paths lazily/fuzzily with query strings. </summary>
	///
	/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
	///
	/// <example>
	///   Being at the C:\ root, this class would be used for finding a deeply nested directory or
	///   file easily, and many matches if your query wasn't accurate enough.
	///   
	///   Expansion should occur thusly:
	///   /us/kj/md/mg/aoe/d
	///   Should expand to C:/Users/kjerk/My Documents/My Games/Age of Empires 3/Data
	///   
	///   This because at each level in the heirarchy either there is a single result
	///   or multiple results that could possibly have fit have been gauged for fitness and the best
	///   results returned, any dead ends are skipped.
	/// </example>
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public class FuzzyPathEvaluator
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Finds directory paths based on pathQuery originating at startPath. </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		/// <remarks>	This is the bread and butter of this class and project. To be callable by a
		/// 			command prompt this needs to boil down to one main function. </remarks>
		///
		/// <param name="startPath"> 	Starting directory path. "" is acceptable and will default to CurrentDirectory. </param>
		/// <param name="pathQuery"> 	The path query. </param>
		/// <param name="maxResults">	(Optional) the maximum results. </param>
		///
		/// <returns>	A string[] of directory paths sorted by their fuzzy match to pathQuery. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public string[] FindPaths(string startPath, string pathQuery, int maxResults = 6)
		{
			//Yeah it's dumb, optional parameters have to be a compile time constant anyway.
			if (String.IsNullOrEmpty(startPath)) startPath = Environment.CurrentDirectory;

			string[] startPathBits = startPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
			string[] pathQueryBits = pathQuery.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

			//TODO: Add \\MyFiles, and other absolute paths.
			if (Regex.IsMatch(pathQuery, @"^(\\|/|~)")) // ~, /, or \
			{
				switch (pathQuery[0])
				{
					case '~':
						startPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
						pathQueryBits = pathQueryBits.Skip(1).ToArray(); //Skip tilde element.
						break;

					case '/':
					case '\\':
						startPath = startPathBits[0]+'\\'; // Was removed by the string.split.
						break;
				}
			}
			else if (pathQuery.StartsWith("..")) // Up one/more directories.
			{
				string changeBuf = "";
				foreach (string pathQueryBit in pathQueryBits)
				{
					if (pathQueryBit == "..")
						changeBuf += "..\\";
					else break;
				}

				pathQuery = Regex.Replace(pathQuery, @"^((\.\.)(\\|/))+", "");
				pathQueryBits = pathQuery.Split(new[] { '\\', '/' });
				startPath = Path.GetFullPath(Path.Combine(startPath, changeBuf));
			}
			else if (Regex.IsMatch(pathQuery, "^.:(\\\\|/)")) // C:\ etc.
			{
				startPath = pathQueryBits[0] + "\\";
				pathQueryBits = pathQueryBits.Skip(1).ToArray();
			}

			var matchedPaths = new List<string>();
			var res = DirFitnessRecurse(startPath, new List<string>(pathQueryBits));
			res.ForEach(ws => matchedPaths.Add(ws.Value));

			return matchedPaths.Take(maxResults).ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Recursive fitness comparison initialiser. </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		///
		/// <param name="startdir"> 	The starting directory. </param>
		/// <param name="fragments">	A list of query fragments. </param>
		///
		/// <returns>	A list of WeightedString, sorted by fitness. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		private List<WeightedString> DirFitnessRecurse(string startdir, List<string> fragments)
		{
			var paths = new List<WeightedString>();

			string frag = fragments[0];

			string[] dirNames;

			try
			{
				var glob = "*" + String.Join("*", frag.ToCharArray()) + "*";
				dirNames = GetSubdirectoryNames(startdir, glob);
			}
			catch (Exception) //No read access to that directory.
			{
				return paths;
			}

			var gradedNames = GradeStringFitnessMatches(frag, dirNames);

			var testablePaths = gradedNames.Where(t => t.Weight > 0).Take(6);

			if (fragments.Count > 1)
			{
				foreach (WeightedString ws in testablePaths)
				{
					paths.AddRange(DirFitnessRecurseR(Path.Combine(startdir, ws.Value), ws.Weight, 1, fragments));
				}
			}
			else //Single level.
			{
				return testablePaths.Select(p => new WeightedString(p.Weight, Path.Combine(startdir, p.Value))).ToList();
			}

			return paths;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		///   Recurses down into dubfolders of dir to depth of fragments.Length, weighting directory
		///   strings.
		/// </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		/// <remarks>	This is the actual recursive function, the other being the parent caller. </remarks>
		///
		/// <param name="dir">	    	Current full directory path. </param>
		/// <param name="fitness">  	Accrued fitness level of the current directory. </param>
		/// <param name="depth">    	Directory depth from start. </param>
		/// <param name="fragments">	A list of query fragments. </param>
		///
		/// <returns>	An enumerable list of weighted strings. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		private IEnumerable<WeightedString> DirFitnessRecurseR(string dir, int fitness, int depth, List<string> fragments)
		{
			string frag = fragments[depth];
			var currentDir = dir;

			if (fragments.Count - 1 == depth)
			{
				var list = new List<WeightedString>();

				var filesAndDirNames = new List<string>();

				try
				{
					//Add subdirectories and files at least matching a single character from this fragment.
					filesAndDirNames.AddRange(Directory.GetFiles(dir, "*" + String.Join("*", frag.ToCharArray()) + "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName));
					filesAndDirNames.AddRange(GetSubdirectoryNames(currentDir, "*" + String.Join("*", frag.ToCharArray()) + "*"));
				}
				catch (Exception) //Could not read from the directory (usually permissions)
				{
					return list;
				}

				var gradedNames = GradeStringFitnessMatches(frag, filesAndDirNames);

				List<WeightedString> finalPaths = gradedNames.Where(t => t.Weight > 0).Take(6).ToList();

				foreach (var finalPath in finalPaths)
				{
					list.Add(new WeightedString(finalPath.Weight + fitness, Path.Combine(dir, finalPath.Value)));
				}

				return list;
			}
			else
			{
				var paths = new List<WeightedString>();

				string[] dirStrings;

				try
				{
					dirStrings = Directory.GetDirectories(dir, "*" + String.Join("*", frag.ToCharArray()) + "*", SearchOption.TopDirectoryOnly);
				}
				catch (Exception)
				{
					return paths; //No read access to directory.
				}

				var dirNames = dirStrings.Select(ds => ds.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Last());

				List<WeightedString> gradedNames;

				if (dirNames.Any())
				{
					gradedNames = GradeStringFitnessMatches(frag, dirNames);
				}
				else
				{
					return paths; // No dirs to check.
				}

				var toCheck = gradedNames.Where(t => t.Weight > 0).Take(6);

				foreach (WeightedString ws in toCheck)
				{
					dir = Path.Combine(currentDir, ws.Value);
					paths.AddRange(DirFitnessRecurseR(dir, ws.Weight + fitness, depth + 1, fragments));
				}

				return paths; // No dirs to check.
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets subdirectory names. </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		///
		/// <param name="dir"> 	The dir. </param>
		/// <param name="glob">	The glob. </param>
		///
		/// <returns>	An array of string. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public string[] GetSubdirectoryNames(string dir, string glob)
		{
			var dirStrings = Directory.GetDirectories(dir, glob, SearchOption.TopDirectoryOnly);
			//var dirNames = dirStrings.Select(ds => ds.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Last());

			return dirStrings.Select(Path.GetFileName).ToArray();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		///   Grades a group of strings' fitness against a given term, returning weighted strings.
		///   Weighted strings are returned in a list sorted by their fitness.
		/// </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		///
		/// <param name="needle">			The typed in "query" fragment being evaluated. </param>
		/// <param name="haystacks">			Multiple strings to judge. </param>
		/// <param name="fitnessThreshhold">	(Optional) the fitness threshhold. </param>
		///
		/// <returns>	A list of WeightedString, sorted by fitness. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<WeightedString> GradeStringFitnessMatches(string needle, IEnumerable<string> haystacks, int fitnessThreshhold = 1)
		{
			//Shiny!
			var dict = haystacks.Select(haystack => new WeightedString(GradeStringFitnessMatch(needle, haystack), haystack)).ToList();

			//Filter down to fitness threshold before sorting unnecessary strings.
			dict = dict.Where(ws => ws.Weight >= fitnessThreshhold).ToList();

			//Sort descending.
			dict.Sort((cl,cr) => cr.Weight.CompareTo(cl.Weight));

			return dict;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		///   A rather simplistic approach to fuzzy string matching, more based around intention than
		///   edit distance.
		/// </summary>
		///
		/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
		///
		/// <param name="needle">  	The typed in "query" fragment being evaluated. </param>
		/// <param name="haystack">	The body of a string that is being judged for fitness. </param>
		///
		/// <returns>
		///   An arbitrary integer of assessed fitness of the string, to be compared against other
		///   strings' fitness.
		/// </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public int GradeStringFitnessMatch(string needle, string haystack)
		{
			var q = new Queue<char>(needle.ToCharArray()); // Check off matched characters by Dequeue them.
			var gradeStack = new Stack<int>(); // Sum these to get final fitness.

			//Numeric 'buffers'.
			int gradeAccrue = 0;
			int sinceLastMatch = 0;

			// Character buffers.
			char prev = '#'; // Previous character in the haystack string.
			char lastMatch = '\0'; // Last matching character from needle found in haystack.

			foreach (char hc in haystack)
			{
				var hcl = Char.ToLower(hc); // Haystack character lower.
				var qcl = Char.ToLower(q.Peek()); // Query character lower.
				gradeAccrue = 0; // Reset the per character grade buffer.

				if (qcl == hcl)
				{
					gradeAccrue += 1;

					if (Char.IsUpper(hc)) // Upper case character priority.
						gradeAccrue += 2;

					if (sinceLastMatch == 0) // Consecutive
						gradeAccrue += 1;

					if (!char.IsLetterOrDigit(prev)) // Beginning of new text.
						gradeAccrue += 1;

					lastMatch = q.Dequeue(); // This character has been matched, remove from matching queue.
					sinceLastMatch = 0; // To test sequentialiity of matching characters.
				}
				else if (qcl == Char.ToLower(lastMatch)) // A the last matched character has been encountered again, replace if the grade is higher.
				{ // Note: this is pretty much a copy paste of the above logic, but the loop's buffers are coupled with the grading logic, I haven't figured out a clean refactor yet.
					// This should combat the case of judging "mstf" against a string like "Marcus_Stuff". The first 's' would typically be resolved against the first matching 's' in "Marcus_Stuff", yet
					// the second S should have (much) higher priority, but wouldn't get matched again because of the dequeuing.
					// Since the grades are accrued in a stack if this new match was better than the last one we can pop the old grade and add the new higher one instead.
					gradeAccrue += 1;

					if (Char.IsUpper(hc)) // Upper case haystack character.
					{
						gradeAccrue += 2;

						if (Char.IsUpper(q.Peek())) // Upper case query character too. Strong match.
							gradeAccrue += 1;
					}

					if (!char.IsLetterOrDigit(prev)) // Beginning of new text.
						gradeAccrue += 1;

					if (sinceLastMatch == 0) // Consecutive matches.
						gradeAccrue += 1;

					if (gradeAccrue > gradeStack.Peek()) // This new match was better than the last one
					{
						gradeStack.Pop(); // Remove the last grade.
						gradeStack.Push(gradeAccrue); // Add the new higher result.
					}

					sinceLastMatch = 0;
				}
				else
				{
					// Not a match, consecutive broken.
					sinceLastMatch++;
					// TODO: If 'sinceLastMatch' is equidistant(ish) on a few matches give those higher grade as it's probably the user's intent to match a wide swath of path characters.
					// E.G. "tbfd" against "tonysbestfilesdummy"
					//                      t    b   f    d
				}

				prev = hc; // Stash this iteration's character for referring to whether it is a word boundary/other on next match.

				if (gradeAccrue != 0)
					gradeStack.Push(gradeAccrue);

				if (q.Count == 0)
					break;
			}

			if (q.Count > 0)
				return 0; //Not all characters were matched (meaning didn't get Dequeued).

			return gradeStack.Sum();
		}
	}
}