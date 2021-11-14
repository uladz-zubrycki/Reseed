using System;
using JetBrains.Annotations;

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

		public DataExtensionOptions(bool generateIdentityValues)
		{
			GenerateIdentityValues = generateIdentityValues;
		}
	}
}