using System;
using System.Linq;
using JetBrains.Annotations;

namespace Reseed.Schema
{
	// Check out https://docs.microsoft.com/en-us/sql/t-sql/data-types/data-types-transact-sql for 
	// additional information
	//
	// Consider splitting into multiple types, if we need more precise representation
	public sealed class DataType
	{
		private readonly bool isReal;

		public readonly string Name;
		public readonly int? MaxLength;
		public readonly int? Precision;
		public readonly int? Scale;
		public readonly string Text;

		public readonly bool IsChar;
		public readonly bool IsDate;
		public readonly bool IsTime;
		public readonly bool IsDateTime;
		public readonly bool IsDateTime2;
		public readonly bool IsDateTimeOffset;
		public readonly bool IsText;
		public readonly bool IsBit;
		public readonly bool IsUniqueIdentifier;
		public readonly bool IsDecimal;
		public readonly bool IsFloat;
		public readonly bool IsInt;
		public readonly bool IsMoney;
		public readonly bool IsBinary;
		public readonly bool IsImage;
		public readonly bool IsHierarchyId;
		public readonly bool IsSqlVariant;
		public readonly bool IsRowVersion;
		public readonly bool IsXml;
		public readonly bool IsGeometry;
		public readonly bool IsGeography;

		public readonly bool HasVariableLength;
		public readonly bool HasInfiniteLength;

		public DataType([NotNull] string name, int? maxLength, int? precision, int? scale)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Value cannot be null or empty.", nameof(name));
			if (maxLength == 0 || (maxLength < 0 && maxLength != -1) || maxLength > 8000)
			{
				throw new ArgumentException(
					"Invalid max length value, should be either -1 or from 1 to 8000",
					nameof(maxLength));
			}

			this.Name = name;
			this.MaxLength = maxLength;
			this.Precision = precision;
			this.Scale = scale;
			this.HasVariableLength = this.Name.IndexOf("var", StringComparison.OrdinalIgnoreCase) >= 0;
			this.HasInfiniteLength = this.MaxLength == -1;
			this.IsChar = this.Name.IndexOf("char", StringComparison.OrdinalIgnoreCase) >= 0;
			this.IsDate = this.Name.Equals("date", StringComparison.OrdinalIgnoreCase);
			this.IsTime = this.Name.StartsWith("time", StringComparison.OrdinalIgnoreCase);
			this.IsDateTime = this.Name.Equals("datetime", StringComparison.OrdinalIgnoreCase);
			this.IsDateTime2 = this.Name.StartsWith("datetime2", StringComparison.OrdinalIgnoreCase);
			this.IsDateTimeOffset = this.Name.StartsWith("datetimeoffset", StringComparison.OrdinalIgnoreCase);
			this.IsText = this.Name.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0;
			this.IsBit = this.Name.Equals("bit", StringComparison.OrdinalIgnoreCase);
			this.IsUniqueIdentifier = this.Name.Equals("uniqueidentifier", StringComparison.OrdinalIgnoreCase);
			this.IsDecimal = GetIsDecimal();
			this.isReal = this.Name.Equals("real", StringComparison.OrdinalIgnoreCase);
			this.IsFloat = this.isReal || this.Name.StartsWith("float", StringComparison.OrdinalIgnoreCase);
			this.IsInt = this.Name.IndexOf("int", StringComparison.OrdinalIgnoreCase) >= 0;
			this.IsMoney = this.Name.IndexOf("money", StringComparison.OrdinalIgnoreCase) >= 0;
			this.IsBinary = this.Name.IndexOf("binary", StringComparison.OrdinalIgnoreCase) >= 0;
			this.IsImage = this.Name.Equals("image", StringComparison.OrdinalIgnoreCase);
			this.IsHierarchyId = this.Name.Equals("hierarchyid", StringComparison.OrdinalIgnoreCase);
			this.IsSqlVariant = this.Name.Equals("sql_variant", StringComparison.OrdinalIgnoreCase);
			this.IsRowVersion = this.Name.Equals("rowversion", StringComparison.OrdinalIgnoreCase);
			this.IsXml = this.Name.StartsWith("xml", StringComparison.OrdinalIgnoreCase);
			this.IsGeometry = this.Name.Equals("geometry", StringComparison.OrdinalIgnoreCase);
			this.IsGeography = this.Name.Equals("geography", StringComparison.OrdinalIgnoreCase);
			this.Text = BuildDefinition();
		}

		public override string ToString() => this.Text;

		private bool GetIsDecimal() =>
			this.Name.StartsWith("decimal", StringComparison.OrdinalIgnoreCase) ||
			this.Name.StartsWith("dec", StringComparison.OrdinalIgnoreCase) ||
			this.Name.StartsWith("numeric", StringComparison.OrdinalIgnoreCase);

		private string BuildDefinition()
		{
			return this.Name +
			       (HasLength() ? RenderLengthOptions() : string.Empty) +
			       (this.IsDecimal ? RenderDecimalOptions() : string.Empty) +
			       (HasPrecision() && this.Precision != null ? $"({this.Precision})" : string.Empty);

			string RenderDecimalOptions()
			{
				var options = new[] { this.Precision, this.Scale }
					.Where(o => o != null)
					.ToArray();

				return options.Any() ? $"({string.Join(", ", options)})" : string.Empty;
			}

			string RenderLengthOptions()
			{
				if (this.MaxLength != null)
				{
					return this.HasVariableLength && this.HasInfiniteLength ? "(max)" : $"({this.MaxLength})";
				}
				else
				{
					return string.Empty;
				}
			}

			bool HasPrecision() =>
				this.IsDateTime2 ||
				this.IsDateTimeOffset ||
				this.IsTime ||
				(this.IsFloat && !this.isReal);

			bool HasLength() => this.IsChar || this.IsBinary;
		}
	}
}