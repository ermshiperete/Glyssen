﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using NUnit.Framework;
using ProtoScript;
using ProtoScript.Bundle;
using ProtoScript.Character;
using ProtoScript.Dialogs;

namespace ProtoScriptTests.Dialogs
{
	[TestFixture]
	class AssignCharacterViewModelTests
	{
		private const string kTest = "test~~";

		private Project m_testProject;
		private AssignCharacterViewModel m_model;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			// Use a test version of the file so the tests won't break every time we fix a problem in the production control file.
			ControlCharacterVerseData.TabDelimitedCharacterVerseData = Properties.Resources.TestCharacterVerse;
			CreateTestProject();
			m_testProject = Project.Load(Project.GetProjectFilePath(kTest, kTest));
		}

		[SetUp]
		public void SetUp()
		{
			m_model = new AssignCharacterViewModel(m_testProject);
			m_model.SetUiStrings("narrator ({0})",
				"book title or chapter ({0})",
				"introduction ({0})",
				"section head ({0})",
				"normal");
			m_model.BackwardContextBlockCount = 10;
			m_model.ForwardContextBlockCount = 10;
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			DeleteTestProjectFolder();
		}

		public static void DeleteTestProjectFolder()
		{
			var testProjFolder = Path.Combine(Project.ProjectsBaseFolder, kTest);
			if (Directory.Exists(testProjFolder))
				Directory.Delete(testProjFolder, true);
		}

		public static void CreateTestProject()
		{
			DeleteTestProjectFolder();
			var sampleMetadata = new DblMetadata();
			sampleMetadata.AvailableBooks = new List<Book>();
			var bookOfMarkMetadata = new Book();
			bookOfMarkMetadata.Code = "MRK";
			bookOfMarkMetadata.IncludeInScript = true;
			bookOfMarkMetadata.LongName = "Gospel of Mark";
			bookOfMarkMetadata.ShortName = "Mark";
			sampleMetadata.AvailableBooks.Add(bookOfMarkMetadata);
			sampleMetadata.FontFamily = "Times New Roman";
			sampleMetadata.FontSizeInPoints = 12;
			sampleMetadata.id = kTest;
			sampleMetadata.language = new DblMetadataLanguage { iso = kTest };

			XmlDocument sampleMark = new XmlDocument();
			sampleMark.LoadXml(Properties.Resources.TestMRK);
			UsxDocument mark = new UsxDocument(sampleMark);

			var project = new Project(sampleMetadata, new[] { mark }, SfmLoader.GetUsfmStylesheet());
			while (!project.IsInitialized)
				Thread.Sleep(100);
		}

		[Test]
		public void Constructor_FirstQuoteIsUnexpected_FirstUnexpectedBlockLoaded()
		{
			Assert.AreEqual("MRK", m_model.CurrentBookId);
			Assert.AreEqual(1, m_model.CurrentBlock.ChapterNumber);
			Assert.AreEqual(2, m_model.CurrentBlock.InitialStartVerseNumber);
		}

		[Test]
		public void Narrator_CurrentBookIsMark_ToStringIncludesBookName()
		{
			Assert.AreEqual("narrator (MRK)", AssignCharacterViewModel.Character.Narrator.ToString());
		}

		[Test]
		public void GetCharactersForCurrentReference_UnexpectedQuoteWithNoContext_GetsNarratorOnly()
		{
			var characters = m_model.GetCharactersForCurrentReference(false).ToList();
			Assert.AreEqual(1, characters.Count);
			Assert.IsTrue(characters[0].IsNarrator);
			Assert.IsFalse(characters[0].ProjectSpecific);
		}

		[Test]
		public void GetCharactersForCurrentReference_UnexpectedQuoteWithContext_GetsNarratorOnly()
		{
			// Note: Default forward/backward cvontext is 10 blocks.
			var characters = m_model.GetCharactersForCurrentReference().ToList();
			Assert.AreEqual(1, characters.Count);
			Assert.IsTrue(characters[0].IsNarrator);
			Assert.IsFalse(characters[0].ProjectSpecific);
		}

		[Test]
		public void GetCharactersForCurrentReference_UnexpectedQuoteWith20BlockForwardContext_GetsNarratorAndJesus()
		{
			m_model.ForwardContextBlockCount = 20;
			var characters = m_model.GetCharactersForCurrentReference().ToList();
			Assert.AreEqual(2, characters.Count);
			Assert.IsTrue(characters[0].IsNarrator);
			Assert.IsFalse(characters[0].ProjectSpecific);
			Assert.AreEqual("Jesus", characters[1].CharacterId);
			Assert.IsTrue(characters[1].ProjectSpecific);
		}

		[Test]
		public void GetCharactersForCurrentReference_AmbiguousQuote_GetsBothCharactersPlusNarrator()
		{
			FindRefInMark(5, 9);
			var characters = m_model.GetCharactersForCurrentReference().ToList();
			Assert.AreEqual(3, characters.Count);
			Assert.AreEqual("Jesus", characters[0].CharacterId);
			Assert.IsFalse(characters[0].ProjectSpecific);
			Assert.AreEqual("man with evil spirit", characters[1].CharacterId);
			Assert.IsFalse(characters[1].ProjectSpecific);
			Assert.IsTrue(characters[2].IsNarrator);
			Assert.IsFalse(characters[2].ProjectSpecific);
		}

		[Test]
		public void GetUniqueCharacters_AmbiguousQuoteNoFilter_GetsAllCharactersInMark()
		{
			FindRefInMark(5, 9);
			var characters = m_model.GetUniqueCharacters().ToList();
			Assert.AreEqual(100, characters.Count);
			Assert.IsTrue(characters.Any(c => c.IsNarrator));
			Assert.IsTrue(characters.Any(c => c.CharacterId == "Jesus"));
			Assert.IsTrue(characters.Any(c => c.CharacterId == "man with evil spirit"));
		}

		[Test]
		public void GetUniqueCharacters_AmbiguousQuoteFilter_GetsAllCharactersInMark()
		{
			FindRefInMark(5, 9);
			var characters = m_model.GetUniqueCharacters("zeru").ToList();
			Assert.AreEqual(4, characters.Count);
			Assert.AreEqual("Zerubbabel/Jeshua/rest of heads of families", characters[0].CharacterId);
			Assert.IsTrue(characters.Any(c => c.CharacterId == "Jesus"));
			Assert.IsTrue(characters.Any(c => c.CharacterId == "man with evil spirit"));
			Assert.IsTrue(characters[3].IsNarrator);
		}

		[Test]
		public void LoadNextRelevantBlock_DataHasSomeContinuationBlocksNeedingAssignment_ContinuationBlocksNeverGetLoaded()
		{
			Assert.IsTrue(m_testProject.IncludedBooks.SelectMany(b => b.Blocks).Any(b => b.CharacterIsUnclear() && b.MultiBlockQuote == MultiBlockQuote.Continuation),
				"Test data does not have the required characteristics. Need at least one unassigned block that is a continuation of a quote from earlier paragraph.");

			do
			{
				Assert.IsFalse(m_model.CurrentBlock.MultiBlockQuote == MultiBlockQuote.Continuation);
				m_model.LoadNextRelevantBlock();
			} while (!m_model.CanNavigateToNextRelevantBlock);
			Assert.IsFalse(m_model.CurrentBlock.MultiBlockQuote == MultiBlockQuote.Continuation);
		}

		[Test]
		public void SetMode_MoreQuotesThanExpectedSpeakers_LoadsBlocksWithMoreQuotesThanExpectedSpeakers()
		{
			m_model.Mode = BlocksToDisplay.MoreQuotesThanExpectedSpeakers;

			Assert.AreEqual("MRK", m_model.CurrentBookId);
			Assert.AreEqual(1, m_model.CurrentBlock.ChapterNumber);
			Assert.AreEqual(2, m_model.CurrentBlock.InitialStartVerseNumber);
			FindRefInMark(2, 5);
		}

		[Test]
		public void SetMode_ExcludeUserConfirmed_UserConfirmedBlockSkipped()
		{
			var block1 = m_model.CurrentBlock;
			m_model.LoadNextRelevantBlock();
			var block2 = m_model.CurrentBlock;
			m_model.LoadPreviousRelevantBlock();

			block1.CharacterId = "Sigmund";
			block1.UserConfirmed = true;

			m_model.Mode |= BlocksToDisplay.ExcludeUserConfirmed;

			Assert.AreEqual(block2, m_model.CurrentBlock);
		}

		[Test]
		public void SetMode_AlternateBetweenModes_AssignedBlockCountDoesNotGrowContinuously()
		{
			Assert.AreEqual(0, m_model.AssignedBlockCount);

			m_model.SetCharacterAndDelivery(new AssignCharacterViewModel.Character("Jesus"), AssignCharacterViewModel.Delivery.Normal);

			Assert.AreEqual(1, m_model.AssignedBlockCount);

			m_model.Mode = BlocksToDisplay.NeedAssignments | BlocksToDisplay.ExcludeUserConfirmed;			
			Assert.AreEqual(0, m_model.AssignedBlockCount);

			m_model.Mode = BlocksToDisplay.NeedAssignments;
			Assert.AreEqual(1, m_model.AssignedBlockCount);

			// The assignment call above actually affects 5 blocks because they are all in the same quote.
			m_model.Mode = BlocksToDisplay.AllScripture;
			Assert.AreEqual(1, m_model.AssignedBlockCount);

			m_model.Mode = BlocksToDisplay.HotSpots | BlocksToDisplay.ExcludeUserConfirmed;
			Assert.AreEqual(0, m_model.AssignedBlockCount);
		}

		[Test]
		public void SetMode_All_UnmodifiableStandardBlocksSkipped()
		{
			m_model.Mode = BlocksToDisplay.AllScripture;
			Assert.IsFalse(CharacterVerseData.IsCharacterStandard(m_model.CurrentBlock.CharacterId, false));
		}

		[Test]
		public void GetDeliveriesForCharacter_NullCharacter_GetsEmptyEnumeration()
		{
			Assert.False(m_model.GetDeliveriesForCharacter(null).Any());
		}

		[Test]
		public void IsModified_NormalDeliveryNoChange_ReturnsFalse()
		{
			var block1 = m_model.CurrentBlock;
			m_model.CurrentBlock.Delivery = null;
			Assert.IsFalse(m_model.IsModified(new AssignCharacterViewModel.Character(block1.CharacterId), AssignCharacterViewModel.Delivery.Normal));
			m_model.CurrentBlock.Delivery = string.Empty;
			Assert.IsFalse(m_model.IsModified(new AssignCharacterViewModel.Character(block1.CharacterId), AssignCharacterViewModel.Delivery.Normal));
		}

		[Test]
		public void IsModified_CharacterChanged_ReturnsTrue()
		{
			var block1 = m_model.CurrentBlock;
			block1.Delivery = null;
			Assert.IsTrue(m_model.IsModified(new AssignCharacterViewModel.Character("Ferdinand"), AssignCharacterViewModel.Delivery.Normal));
		}

		[Test]
		public void IsModified_DeliveryChangedToNormal_ReturnsTrue()
		{
			var block1 = m_model.CurrentBlock;
			m_model.CurrentBlock.Delivery = "annoyed";
			Assert.IsTrue(m_model.IsModified(new AssignCharacterViewModel.Character(block1.CharacterId), AssignCharacterViewModel.Delivery.Normal));
		}

		[Test]
		public void IsModified_DeliveryChangedFromNormal_ReturnsTrue()
		{
			var block1 = m_model.CurrentBlock;
			m_model.CurrentBlock.Delivery = null;
			Assert.IsTrue(m_model.IsModified(new AssignCharacterViewModel.Character(block1.CharacterId), new AssignCharacterViewModel.Delivery("peeved")));
		}

		[Test]
		public void BlockCountForCurrentBook_TestMrk_ReturnsTrue()
		{
			int expectedCount = m_testProject.IncludedBooks[0].Blocks.Count;
			Assert.AreEqual(expectedCount, m_model.BlockCountForCurrentBook, "Test data may have been changed");
		}

		[Test]
		public void GetIndexOfClosestRelevantBlock_MinGreaterThanMax_ReturnsNegative1()
		{
			Assert.AreEqual(-1, AssignCharacterViewModel.GetIndexOfClosestRelevantBlock(
				new List<Tuple<int, int>>(), new Tuple<int, int>(1, 2), true, 1, 0));
		}

		[Test]
		public void GetIndexOfClosestRelevantBlock_PreviousBlockIsRelevant_ReturnsClosestPreviousRelevantBlock()
		{
			var relevantBlocks = new List<Tuple<int, int>>();
			relevantBlocks.Add(new Tuple<int, int>(1, 2));
			relevantBlocks.Add(new Tuple<int, int>(1, 20));
			relevantBlocks.Add(new Tuple<int, int>(2, 1));
			relevantBlocks.Add(new Tuple<int, int>(2, 7));
			relevantBlocks.Add(new Tuple<int, int>(2, 8));
			relevantBlocks.Add(new Tuple<int, int>(2, 14));
			relevantBlocks.Add(new Tuple<int, int>(3, 2));
			Assert.AreEqual(4, AssignCharacterViewModel.GetIndexOfClosestRelevantBlock(
				relevantBlocks, new Tuple<int, int>(2, 10), true, 0, relevantBlocks.Count - 1));
		}

		[Test]
		public void GetIndexOfClosestRelevantBlock_NoPreviousBlockIsRelevant_ReturnsNegative1()
		{
			var relevantBlocks = new List<Tuple<int, int>>();
			relevantBlocks.Add(new Tuple<int, int>(2, 14));
			relevantBlocks.Add(new Tuple<int, int>(3, 2));
			Assert.AreEqual(-1, AssignCharacterViewModel.GetIndexOfClosestRelevantBlock(
				relevantBlocks, new Tuple<int, int>(1, 3), true, 0, relevantBlocks.Count - 1));
		}

		[Test]
		public void GetIndexOfClosestRelevantBlock_FollowingBlockIsRelevant_ReturnsClosestFollowingRelevantBlock()
		{
			var relevantBlocks = new List<Tuple<int, int>>();
			relevantBlocks.Add(new Tuple<int, int>(1, 2));
			relevantBlocks.Add(new Tuple<int, int>(1, 20));
			relevantBlocks.Add(new Tuple<int, int>(2, 1));
			relevantBlocks.Add(new Tuple<int, int>(2, 7));
			relevantBlocks.Add(new Tuple<int, int>(2, 8));
			relevantBlocks.Add(new Tuple<int, int>(2, 14));
			relevantBlocks.Add(new Tuple<int, int>(3, 2));
			Assert.AreEqual(2, AssignCharacterViewModel.GetIndexOfClosestRelevantBlock(
				relevantBlocks, new Tuple<int, int>(1, 21), false, 0, relevantBlocks.Count - 1));
		}

		[Test]
		public void GetIndexOfClosestRelevantBlock_NoFollowingBlockIsRelevant_ReturnsNegative1()
		{
			var relevantBlocks = new List<Tuple<int, int>>();
			relevantBlocks.Add(new Tuple<int, int>(1, 2));
			relevantBlocks.Add(new Tuple<int, int>(1, 20));
			relevantBlocks.Add(new Tuple<int, int>(2, 1));
			relevantBlocks.Add(new Tuple<int, int>(2, 7));
			relevantBlocks.Add(new Tuple<int, int>(2, 8));
			relevantBlocks.Add(new Tuple<int, int>(2, 14));
			relevantBlocks.Add(new Tuple<int, int>(3, 2));
			Assert.AreEqual(-1, AssignCharacterViewModel.GetIndexOfClosestRelevantBlock(
				relevantBlocks, new Tuple<int, int>(3, 3), false, 0, relevantBlocks.Count - 1));
		}

		[Test]
		public void CanNavigateToPreviousRelevantBlock_CurrentBlockIsFirstRelevantBlock_ReturnsFalse()
		{
			Assert.IsFalse(m_model.CanNavigateToPreviousRelevantBlock);
		}

		[Test]
		public void CanNavigateToNextRelevantBlock_CurrentBlockIsFirstRelevantBlock_ReturnsTrue()
		{
			Assert.IsTrue(m_model.CanNavigateToNextRelevantBlock);
		}

		[Test]
		public void CanNavigateToPreviousRelevantBlock_CurrentBlockIsSecondRelevantBlock_ReturnsTrue()
		{
			m_model.LoadNextRelevantBlock();
			Assert.IsTrue(m_model.CanNavigateToPreviousRelevantBlock);
		}

		[Test]
		public void CanNavigateToNextRelevantBlock_CurrentBlockIsLastRelevantBlock_ReturnsFalse()
		{
			m_model.CurrentBlockIndexInBook = m_testProject.IncludedBooks[0].GetScriptBlocks().Count - 1;
			m_model.LoadPreviousRelevantBlock();
			Assert.IsFalse(m_model.CanNavigateToNextRelevantBlock);
		}

		[Test]
		public void CanNavigateToPreviousRelevantBlock_CurrentBlockIsVeryFirstBlock_ReturnsFalse()
		{
			m_model.CurrentBlockIndexInBook = 0;
			Assert.IsFalse(m_model.CanNavigateToPreviousRelevantBlock);
		}

		[Test]
		public void CanNavigateToNextRelevantBlock_CurrentBlockIsVeryLastBlockInBook_ReturnsFalse()
		{
			m_model.CurrentBlockIndexInBook = m_testProject.IncludedBooks[0].GetScriptBlocks().Count - 1;
			Assert.IsFalse(m_model.CanNavigateToNextRelevantBlock);
		}

		[Test]
		public void CanNavigateToPreviousRelevantBlock_CurrentBlockIsAdHocLocationInMIddleOfBook_ReturnsTrue()
		{
			m_model.CurrentBlockIndexInBook = 400;
			Assert.IsFalse(m_model.IsCurrentBlockRelevant, "If this fails, we chose a relevant block index by accident.");
			Assert.IsTrue(m_model.CanNavigateToPreviousRelevantBlock);
		}

		[Test]
		public void CanNavigateToNextRelevantBlock_CurrentBlockIsAdHocLocationInMIddleOfBook_ReturnsTrue()
		{
			m_model.CurrentBlockIndexInBook = 400;
			Assert.IsFalse(m_model.IsCurrentBlockRelevant, "If this fails, we chose a relevant block index by accident.");
			Assert.IsTrue(m_model.CanNavigateToNextRelevantBlock);
		}

		[Test]
		public void SetCurrentBlockIndexInBook_BlockIsRelevantSingleBlockQuote_StateReflectsRelevantBlock()
		{
			m_model.LoadNextRelevantBlock();
			var index = m_model.CurrentBlockIndexInBook;
			Assert.AreEqual(MultiBlockQuote.None, m_model.CurrentBlock.MultiBlockQuote, "If this fails, choose a different block.");
			m_model.LoadNextRelevantBlock();
			m_model.LoadNextRelevantBlock();
			m_model.CurrentBlockIndexInBook = index;
			Assert.IsTrue(m_model.IsCurrentBlockRelevant);
			Assert.IsTrue(m_model.CanNavigateToPreviousRelevantBlock);
			Assert.IsTrue(m_model.CanNavigateToNextRelevantBlock);
			Assert.AreEqual(2, m_model.CurrentBlockDisplayIndex);
		}

		[Test]
		public void SetCurrentBlockIndexInBook_BlockIsNotRelevantSingleBlockQuote_StateReflectsRelevantBlock()
		{
			m_model.CurrentBlockIndexInBook = 400;
			Assert.AreEqual(MultiBlockQuote.None, m_model.CurrentBlock.MultiBlockQuote, "If this fails, choose a different block.");
			Assert.IsFalse(m_model.IsCurrentBlockRelevant);
			Assert.IsTrue(m_model.CanNavigateToPreviousRelevantBlock);
			Assert.IsTrue(m_model.CanNavigateToNextRelevantBlock);
			Assert.AreEqual(0, m_model.CurrentBlockDisplayIndex);
		}

		[Test]
		public void SetCurrentBlockIndexInBook_BlockIsQuoteContinuationBlockForRelevantQuote_StateReflectsQuoteStartBlock()
		{
			var blocks = m_testProject.IncludedBooks[0].GetScriptBlocks();
			int i = 1;
			for (; i < blocks.Count; i++)
			{
				if (blocks[i].MultiBlockQuote == MultiBlockQuote.Continuation)
				{
					var quoteStart = i - 1;
					if (blocks[quoteStart].CharacterIsUnclear())
						break;
					do
					{
						i++;
					} while (blocks[i].MultiBlockQuote == MultiBlockQuote.Continuation);
				}
			}
			m_model.CurrentBlockIndexInBook = i;
			Assert.AreEqual(MultiBlockQuote.Start, m_model.CurrentBlock.MultiBlockQuote);
			Assert.IsTrue(m_model.GetIndicesOfQuoteContinuationBlocks(m_model.CurrentBlock).Any());
			Assert.IsTrue(m_model.IsCurrentBlockRelevant);
			Assert.IsTrue(m_model.CurrentBlockDisplayIndex > 0);
		}

		[Test]
		public void SetCurrentBlockIndexInBook_BlockIsNotRelevantQuoteContinuationBlock_StateReflectsQuoteStartBlock()
		{
			var blocks = m_testProject.IncludedBooks[0].GetScriptBlocks();
			int i = 1;
			for (; i < blocks.Count; i++)
			{
				if (blocks[i].MultiBlockQuote == MultiBlockQuote.Continuation)
				{
					var quoteStart = i - 1;
					if (!blocks[quoteStart].CharacterIsUnclear())
						break;
					do
					{
						i++;
					} while (blocks[i].MultiBlockQuote == MultiBlockQuote.Continuation);
				}
			}
			m_model.CurrentBlockIndexInBook = i;
			Assert.AreEqual(MultiBlockQuote.Start, m_model.CurrentBlock.MultiBlockQuote);
			Assert.IsTrue(m_model.GetIndicesOfQuoteContinuationBlocks(m_model.CurrentBlock).Any());
			Assert.IsFalse(m_model.IsCurrentBlockRelevant);
			Assert.AreEqual(0, m_model.CurrentBlockDisplayIndex);
		}

		[Test]
		public void GetIsBlockScripture_ScriptureBlock_ReturnsTrue()
		{
			m_model.Mode = BlocksToDisplay.AllScripture;
			FindRefInMark(1, 12);
			Assert.IsTrue(m_model.GetIsBlockScripture(m_model.CurrentBlock));
			Assert.IsTrue(m_model.GetIsBlockScripture(m_model.CurrentBlockIndexInBook));
		}

		[Test]
		public void GetIsBlockScripture_TitleBlock_ReturnsFalse()
		{
			Assert.IsFalse(m_model.GetIsBlockScripture(m_testProject.IncludedBooks[0].Blocks[0]));
			Assert.IsFalse(m_model.GetIsBlockScripture(0));
		}

		private void FindRefInMark(int chapter, int verse)
		{
			while (m_model.CurrentBlock.ChapterNumber <= chapter && m_model.CurrentBlock.InitialStartVerseNumber != verse)
				m_model.LoadNextRelevantBlock();
			Assert.AreEqual("MRK", m_model.CurrentBookId);
			Assert.AreEqual(chapter, m_model.CurrentBlock.ChapterNumber);
			Assert.AreEqual(verse, m_model.CurrentBlock.InitialStartVerseNumber);			
		}
	}
}
