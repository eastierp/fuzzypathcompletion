Fuzzy Path Completion
=====================

A C# and Powershell project to enable the lazy evaluation of query strings to
file/folder paths (similar to Sublime Text's find anything).

The eventual goal is to override the autocompletion function in Powershell and
fire on failure of tab to autocomplete a directory, returning a list of best
matched directories for a given query, which Powershell will automatically make
iterable with Tab or Shift+Tab.

The classes uses simple filtering and string fitness to filter out bad candidates,
and try to only evaluate good ones. Dead ends are skipped.

Usage:

    Import-Module FuzzyDirCompletion.dll

    Get-FuzzyPath /pf/mg/ms

Should result in

    C:/Program Files/Microsoft Games/Minesweeper

If there were similar matching items those would also be returned in order of
matching weight.