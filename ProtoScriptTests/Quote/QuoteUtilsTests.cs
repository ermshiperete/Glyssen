﻿using System.Linq;
using NUnit.Framework;
using ProtoScript.Quote;
using SIL.WritingSystems;

namespace ProtoScriptTests.Quote
{
	[TestFixture]
	public class QuoteUtilsTests
	{
		[Test]
		public void GetLevel2Possibilities_OnePossibility()
		{
			var level1 = new QuotationMark("»", "«", "»", 1, QuotationMarkingSystemType.Normal);
			var level2Possibilities = QuoteUtils.GetLevel2Possibilities(level1);
			Assert.AreEqual(1, level2Possibilities.Count());
			var level2 = level2Possibilities.First();
			Assert.AreEqual("›", level2.Open);
			Assert.AreEqual("‹", level2.Close);
		}

		[Test]
		public void GetLevel2Possibilities_MultiplePossibilities()
		{
			var level1 = new QuotationMark("«", "»", "«", 1, QuotationMarkingSystemType.Normal);
			var level2Possibilities = QuoteUtils.GetLevel2Possibilities(level1);
			Assert.AreEqual(2, level2Possibilities.Count());
			var level2A = level2Possibilities.First();
			Assert.AreEqual("“", level2A.Open);
			Assert.AreEqual("”", level2A.Close);
			var level2B = level2Possibilities[1];
			Assert.AreEqual("‹", level2B.Open);
			Assert.AreEqual("›", level2B.Close);
		}

		[Test]
		public void GetLevel2Default_OnePossibility()
		{
			var level1 = new QuotationMark("»", "«", "»", 1, QuotationMarkingSystemType.Normal);
			var level2 = QuoteUtils.GetLevel2Default(level1);
			Assert.AreEqual("›", level2.Open);
			Assert.AreEqual("‹", level2.Close);
			Assert.AreEqual("›", level2.Continue);
			Assert.AreEqual(2, level2.Level);
			Assert.AreEqual(QuotationMarkingSystemType.Normal, level2.Type);
		}

		[Test]
		public void GetLevel2Default_MultiplePossibilities()
		{
			var level1 = new QuotationMark("«", "»", "«", 1, QuotationMarkingSystemType.Normal);
			var level2 = QuoteUtils.GetLevel2Default(level1);
			Assert.AreEqual("“", level2.Open);
			Assert.AreEqual("”", level2.Close);
			Assert.AreEqual("“", level2.Continue);
			Assert.AreEqual(2, level2.Level);
			Assert.AreEqual(QuotationMarkingSystemType.Normal, level2.Type);
		}
	}
}
