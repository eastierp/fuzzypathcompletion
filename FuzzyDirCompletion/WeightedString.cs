using System;

namespace FuzzyPathCompletion
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	///   This class is a simple way to have a shade better functionality than a tuple. Essentially
	///   an int, string named pair to allow sorting and grouping. "Weight" being the key ingredient.
	/// </summary>
	///
	/// <remarks>	kjerk (kjerkdev@gmail.com), 11/3/2013. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public class WeightedString : IEquatable<WeightedString>, IComparable<WeightedString>
	{
		#region Constructors
		public WeightedString()
		{

		}

		public WeightedString(int weight, string value)
		{
			this.weight = weight;
			this.value = value;
		}
		#endregion


		#region Public Properties
		public string Value
		{
			get { return this.value; }
			set { this.value = value; }
		}

		public int Weight
		{
			get { return weight; }
			set { weight = value; }
		}
		#endregion


		#region Private Variables

		private string value;
		private int weight;

		#endregion


		#region Overrides

		public int CompareTo(WeightedString other)
		{
			return this.Weight.CompareTo(other.Weight);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((WeightedString) obj);
		}

		public override int GetHashCode()
		{
			return weight;
		}

		public bool Equals(WeightedString other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return weight == other.weight;
		}
		#endregion
	}
}