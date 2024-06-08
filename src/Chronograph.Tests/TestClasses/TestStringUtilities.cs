using Chronograph.Core.Infrastructure;
using Chronograph.Tests.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chronograph.Tests.TestClasses;

[TestClass]
public class TestStringUtilities
{
	[TestMethod]
	public void TestEscapeBraces_BracesEscaped()
	{
		var testString = "test{ some value }";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test{{ some value }}");
	}

	[TestMethod]
	public void TestEscapeBraces_Record_BracesEscaped()
	{
		var testString = $"test {new TestRecord(42, "test", 1567L)}";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test TestRecord {{ TestProperty0 = 42, TestProperty1 = test, TestProperty2 = 1567 }}");
	}

	[TestMethod]
	public void TestEscapeBraces_Nested_BracesEscaped()
	{
		var testString = "test{ some {SomeOtherValue} value }";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test{{ some {{SomeOtherValue}} value }}");
	}

	[TestMethod]
	public void TestEscapeBraces_NoBracesEscaped()
	{
		var testString = "test {Parameter} value";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be(testString);
	}

	[TestMethod]
	public void TestEscapeBraces_Mixed_SomeBracesEscaped()
	{
		var testString = "test {Parameter} value {Some values} {Parameter2} some more {Value{what ever}part}";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test {Parameter} value {{Some values}} {Parameter2} some more {{Value{{what ever}}part}}");
	}

	[TestMethod]
	public void TestEscapeBraces_UnbalancedBraces1()
	{
		var testString = "test Parameter} value";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be(testString);
	}

	[TestMethod]
	public void TestEscapeBraces_UnbalancedBraces2()
	{
		var testString = "test {Parameter value";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test {{Parameter value");
	}

	[TestMethod]
	public void TestEscapeBraces_UnbalancedBraces3()
	{
		var testString = "test {Parameter{value}";

		var escaped = testString.EscapeCurlyBraces();

		escaped.Should().Be("test {{Parameter{{value}}");
	}
}