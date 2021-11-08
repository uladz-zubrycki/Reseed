using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Tests.Integration.Core;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.Fixtures)]
	public sealed class GenerationValidationTests: TestFixtureBase
	{
		[Test]
		// Should fail with clear error,
		// when there are no tables in database
		public Task ShouldFailGracefully_NoSchemas() =>
			AssertSeedFails(ex => ex.Message.StartsWith(
				"The specified database doesn't contain any tables, " +
				$"therefore can't be used as {nameof(Reseeder)} target."));

		[Test]
		// Should fail with clear error,
		// when there are no entities in data files
		public Task ShouldFailGracefully_NoEntities() =>
			AssertSeedFails(ex => ex.Message.StartsWith(
				"The specified IDataProvider wasn't able to find any entities, " +
				"while at least one is required."));

		[Test]
		// Should fail with clear error,
		// when entities load don't match any of the existing tables
		public Task ShouldFailGracefully_UnknownEntity() =>
			AssertSeedFails(ex => ex.Message.StartsWith(
				"Some of the entities found don't have matching database tables. " +
				"Make sure that names are correct and expected."));

		private Task AssertSeedFails(Expression<Func<Exception, bool>> assertError) =>
			Conventional.AssertGenerationFails(
				this,
				SeedModes.BasicScriptPreferTruncate,
				assertError);
	}
}
