using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Extension;

namespace Reseed
{
	[PublicAPI]
	public sealed class ReseederOptions
	{
		public static readonly ReseederOptions Default = new(
			true,
			new DataExtensionOptions(true));

		public readonly bool ValidateData;
		public readonly DataExtensionOptions ExtensionOptions;

		public ReseederOptions(
			bool validateData,
			[NotNull] DataExtensionOptions extensionOptions)
		{
			ValidateData = validateData;
			ExtensionOptions = extensionOptions ?? throw new ArgumentNullException(nameof(extensionOptions));
		}
	}
	
	[PublicAPI]
	public sealed class DataExtensionOptions
	{
		public readonly bool GenerateIdentityValues;
		public readonly IReadOnlyCollection<ITableExtension> CustomTableExtensions;

		public DataExtensionOptions(bool generateIdentityValues)
			: this(generateIdentityValues, Array.Empty<ITableExtension>())
		{
		}

		public DataExtensionOptions(
			bool generateIdentityValues,
			[NotNull] IReadOnlyCollection<ITableExtension> customTableExtensions)
		{
			if (customTableExtensions == null)
				throw new ArgumentNullException(nameof(customTableExtensions));
			if (customTableExtensions.Any(extension => extension == null))
				throw new ArgumentException(
					"Custom table extensions cannot contain null values.",
					nameof(customTableExtensions));

			GenerateIdentityValues = generateIdentityValues;
			CustomTableExtensions = customTableExtensions.ToArray();
		}
	}
}