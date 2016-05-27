﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DesktopAnalytics;
using Glyssen.Bundle;
using Glyssen.Character;
using L10NSharp;
using Paratext;
using SIL.Reporting;
using SIL.IO;
using SIL.Scripture;
using SIL.Xml;
using ScrVers = Paratext.ScrVers;

namespace Glyssen
{
	public enum ReferenceTextType
	{
		English,
		Azeri,
		French,
		Indonesian,
		Portuguese,
		Russian,
		Spanish,
		TokPisin,
		Custom
	}

	public class ReferenceText : ProjectBase
	{
		public const string kDistFilesReferenceTextDirectoryName = "reference_texts";

		private static readonly Dictionary<ReferenceTextType, ReferenceText> StandardReferenceTexts = new Dictionary<ReferenceTextType, ReferenceText>();

		public static ReferenceText GetStandardReferenceText(ReferenceTextType referenceTextType)
		{
			ReferenceText referenceText;
			if (!StandardReferenceTexts.TryGetValue(referenceTextType, out referenceText))
			{
				ScrVers versification;
				switch (referenceTextType)
				{
					case ReferenceTextType.English:
					case ReferenceTextType.Azeri:
					case ReferenceTextType.French:
					case ReferenceTextType.Indonesian:
					case ReferenceTextType.Portuguese:
					case ReferenceTextType.Russian:
					case ReferenceTextType.Spanish:
					case ReferenceTextType.TokPisin:
						versification = ScrVers.English;
						break;
					default:
						throw new ArgumentOutOfRangeException("referenceTextType", referenceTextType, null);
				}
				referenceText = GenerateStandardReferenceText(referenceTextType);
				referenceText.m_vers = versification;

				StandardReferenceTexts[referenceTextType] = referenceText;
			}
			return referenceText;
		}

		public static ReferenceText CreateCustomReferenceText(GlyssenDblTextMetadata metadata)
		{
			return new ReferenceText(metadata, ReferenceTextType.Custom);
		}

		private static GlyssenDblTextMetadata LoadMetadata(ReferenceTextType referenceTextType, out string referenceProjectFilePath,
			Action<Exception, string, string> reportError = null)
		{
			referenceProjectFilePath = GetReferenceTextProjectFileLocation(referenceTextType);
			Exception exception;
			var metadata = GlyssenDblTextMetadata.Load<GlyssenDblTextMetadata>(referenceProjectFilePath, out exception);
			if (exception != null)
			{
				if (reportError != null)
					reportError(exception, referenceTextType.ToString(), referenceProjectFilePath);
				return null;
			}
			return metadata;
		}

		private static ReferenceText GenerateStandardReferenceText(ReferenceTextType referenceTextType)
		{
			string referenceProjectFilePath;
			var metadata = LoadMetadata(referenceTextType, out referenceProjectFilePath, (exception, token, path) =>
			{
				Analytics.ReportException(exception);
				ReportNonFatalLoadError(exception, token, path);
			});

			var referenceText = new ReferenceText(metadata, referenceTextType);

			var projectDir = Path.GetDirectoryName(referenceProjectFilePath);
			Debug.Assert(projectDir != null);
			string[] files = Directory.GetFiles(projectDir, "???" + kBookScriptFileExtension);
			for (int i = 1; i <= BCVRef.LastBook; i++)
			{
				string bookCode = BCVRef.NumberToBookCode(i);
				string possibleFileName = Path.Combine(projectDir, bookCode + kBookScriptFileExtension);
				if (files.Contains(possibleFileName))
					referenceText.m_books.Add(XmlSerializationHelper.DeserializeFromFile<BookScript>(possibleFileName));
			}
			return referenceText;
		}

		public static string GetReferenceTextProjectFileLocation(ReferenceTextType referenceTextType)
		{
			string projectFileName = referenceTextType.ToString().ToLowerInvariant() + kProjectFileExtension;
			return FileLocator.GetFileDistributedWithApplication(kDistFilesReferenceTextDirectoryName, referenceTextType.ToString(), projectFileName);
		}

		// ENHANCE: Change the key from ReferenceTextType to some kind of token that can represent either a standard
		// reference text or a specific custom one.
		public static Dictionary<string, ReferenceTextType> AllAvailable
		{
			get
			{
				var items = new Dictionary<string, ReferenceTextType>();
				Tuple<Exception, string, string> firstLoadError = null;
				var additionalErrors = new List<string>();

				foreach (var itm in Enum.GetValues(typeof(ReferenceTextType)).Cast<ReferenceTextType>())
				{
					if (itm == ReferenceTextType.Custom) continue;

					string refProjectPath;
					var metadata = LoadMetadata(itm, out refProjectPath, (exception, token, path) =>
					{
						Analytics.ReportException(exception);
						if (firstLoadError == null)
							firstLoadError = new Tuple<Exception, string, string>(exception, token, path);
						else
							additionalErrors.Add(token);
					});
					if (metadata == null) continue;

					items.Add(metadata.Language.Name, itm);
				}

				if (firstLoadError != null)
				{
					if (!items.Any())
					{
						throw new Exception(
							String.Format(LocalizationManager.GetString("ReferenceText.NoReferenceTextsLoaded",
							"No reference texts could be loaded. There might be a problem with your {0} installation. See InnerException " +
							"for more details."), Program.kProduct),
							firstLoadError.Item1);
					}
					if (additionalErrors.Any())
					{
						ErrorReport.ReportNonFatalExceptionWithMessage(firstLoadError.Item1,
							String.Format(LocalizationManager.GetString("ReferenceText.MultipleLoadErrors",
							"The following reference texts could not be loaded: {0}, {1}"), firstLoadError.Item2,
							String.Join(", ", additionalErrors)));
					}
					else
					{
						ReportNonFatalLoadError(firstLoadError.Item1, firstLoadError.Item2, firstLoadError.Item3);
					}
				}

				return items;
			}
		}

		private static void ReportNonFatalLoadError(Exception exception, string token, string path)
		{
			ErrorReport.ReportNonFatalExceptionWithMessage(exception,
				LocalizationManager.GetString("ReferenceText.CouldNotLoad", "The {0} reference text could not be loaded from: {1}"),
				token, path);
		}

		private readonly ReferenceTextType m_referenceTextType;

		private ReferenceText(GlyssenDblTextMetadata metadata, ReferenceTextType referenceTextType)
			: base(metadata, referenceTextType.ToString())
		{
			m_referenceTextType = referenceTextType;

			GetBookName = bookId =>
			{
				var book = Books.FirstOrDefault(b => b.BookId == bookId);
				return book == null ? null : book.PageHeader;
			};
		}

		public bool HasSecondaryReferenceText
		{
			get { return m_referenceTextType != ReferenceTextType.English; }
		}

		public string SecondaryReferenceTextLanguageName
		{
			get { return HasSecondaryReferenceText ? "English" : null; }
		}

		/// <summary>
		/// This gets (a copy of) the included books from the project.
		/// As needed, blocks are broken up and matched to correspond to this reference text.
		/// The books and blocks returned are copies, so that the project itself is not modified.
		/// </summary>
		public IEnumerable<BookScript> GetBooksWithBlocksConnectedToReferenceText(Project project)
		{
			foreach (var book in project.IncludedBooks)
			{
				var referenceBook = Books.SingleOrDefault(b => b.BookId == book.BookId);
				if (referenceBook == null)
					yield return book.Clone(true); // Clone(true) to get the joined blocks
				else
				{
					var clone = book.Clone(true);
					ApplyTo(clone, referenceBook.GetScriptBlocks(), GetFormattedChapterAnnouncement, project.Versification, Versification);
					yield return clone;
				}
			}
		}

		public static void ApplyTo(BookScript vernacularBook, IEnumerable<Block> referenceTextBlocks,
			Func<string, int, string> formatReferenceTextChapterAnnouncement,
			ScrVers vernacularVersification, ScrVers referenceVersification, bool oneToOneMatchingOnly = false)
		{
			var refBlockList = referenceTextBlocks.ToList();

			if (!oneToOneMatchingOnly)
				SplitVernBlocksToMatchReferenceText(vernacularBook, refBlockList, vernacularVersification, referenceVersification);

			MatchVernBlocksToReferenceTextBlocks(vernacularBook, refBlockList, formatReferenceTextChapterAnnouncement, vernacularVersification, referenceVersification, oneToOneMatchingOnly);
		}

		private static void MatchVernBlocksToReferenceTextBlocks(BookScript vernacularBook, List<Block> refBlockList,
			Func<string, int, string> formatReferenceTextChapterAnnouncement,
			ScrVers vernacularVersification, ScrVers referenceVersification, bool oneToOneMatchingOnly)
		{
			int bookNum = BCVRef.BookToNumber(vernacularBook.BookId);
			var vernBlockList = vernacularBook.GetScriptBlocks();

			for (int iVernBlock = 0, iRefBlock = 0; iVernBlock < vernBlockList.Count && iRefBlock < refBlockList.Count; iVernBlock++, iRefBlock++)
			{
				var currentVernBlock = vernBlockList[iVernBlock];
				var currentRefBlock = refBlockList[iRefBlock];
				var vernInitStartVerse = new VerseRef(bookNum, currentVernBlock.ChapterNumber, currentVernBlock.InitialStartVerseNumber, vernacularVersification);
				var refInitStartVerse = new VerseRef(bookNum, currentRefBlock.ChapterNumber, currentRefBlock.InitialStartVerseNumber, referenceVersification);

				if (oneToOneMatchingOnly)
				{
					// Clear any past matching information.
					currentVernBlock.ReferenceBlocks.Clear();
					currentVernBlock.MatchesReferenceText = false;
				}

				var type = CharacterVerseData.GetStandardCharacterType(currentVernBlock.CharacterId);
				switch (type)
				{
					case CharacterVerseData.StandardCharacter.BookOrChapter:
						if (currentVernBlock.IsChapterAnnouncement)
						{
							var refChapterBlock = new Block(currentVernBlock.StyleTag, currentVernBlock.ChapterNumber);
							refChapterBlock.BlockElements.Add(new ScriptText(formatReferenceTextChapterAnnouncement(vernacularBook.BookId, currentVernBlock.ChapterNumber)));
							if (currentRefBlock.IsChapterAnnouncement && currentRefBlock.MatchesReferenceText)
								refChapterBlock.SetMatchedReferenceBlock(currentRefBlock.ReferenceBlocks.Single().Clone());
							currentVernBlock.SetMatchedReferenceBlock(refChapterBlock);
							if (currentRefBlock.IsChapterAnnouncement)
								continue;
						}
						goto case CharacterVerseData.StandardCharacter.ExtraBiblical;
					case CharacterVerseData.StandardCharacter.ExtraBiblical:
						if (type == CharacterVerseData.GetStandardCharacterType(currentRefBlock.CharacterId))
						{
							currentVernBlock.SetMatchedReferenceBlock(currentRefBlock);
							continue;
						}
						goto case CharacterVerseData.StandardCharacter.Intro;
					case CharacterVerseData.StandardCharacter.Intro:
						// This will be re-incremented in the for loop, so it effectively allows
						// the vern index to advance while keeping the ref index the same.
						iRefBlock--;
						if (oneToOneMatchingOnly && !currentVernBlock.MatchesReferenceText)
							throw new Exception("Block not matched to reference block: " + currentVernBlock.ToString(true, vernacularBook.BookId));
						continue;
					default:
						if (refInitStartVerse > vernInitStartVerse)
						{
							iRefBlock--;
							if (oneToOneMatchingOnly && !currentVernBlock.MatchesReferenceText)
								throw new Exception("Block not matched to reference block: " + currentVernBlock.ToString(true, vernacularBook.BookId));
							continue;
						}
						break;
				}

				while (CharacterVerseData.IsCharacterStandard(currentRefBlock.CharacterId, false) || vernInitStartVerse > refInitStartVerse)
				{
					iRefBlock++;
					currentRefBlock = refBlockList[iRefBlock];
					refInitStartVerse = new VerseRef(bookNum, currentVernBlock.ChapterNumber, currentRefBlock.InitialStartVerseNumber, vernacularVersification);
				}

				var indexOfVernVerseStart = iVernBlock;
				var indexOfRefVerseStart = iRefBlock;
				var vernInitEndVerse = (currentVernBlock.InitialEndVerseNumber == 0) ? vernInitStartVerse :
					new VerseRef(bookNum, currentVernBlock.ChapterNumber, currentVernBlock.InitialEndVerseNumber, vernacularVersification);
				var refInitEndVerse = (currentRefBlock.InitialEndVerseNumber == 0) ? refInitStartVerse :
					new VerseRef(bookNum, currentRefBlock.ChapterNumber, currentRefBlock.InitialEndVerseNumber, referenceVersification);
				//var vernLastVerse = new VerseRef(bookNum, currentVernBlock.ChapterNumber, currentVernBlock.LastVerse, vernacularVersification);
				//var refLastVerse = new VerseRef(bookNum, currentRefBlock.ChapterNumber, currentRefBlock.LastVerse, referenceVersification);

				FindAllScriptureBlocksThroughVerse(vernBlockList, vernInitEndVerse, ref iVernBlock, bookNum, vernacularVersification);
				FindAllScriptureBlocksThroughVerse(refBlockList, vernInitEndVerse, ref iRefBlock, bookNum, referenceVersification);

				int numberOfVernBlocksInVerse = iVernBlock - indexOfVernVerseStart + 1;
				int numberOfRefBlocksInVerse = iRefBlock - indexOfRefVerseStart + 1;

				if (vernInitStartVerse.CompareTo(refInitStartVerse) == 0 && vernInitEndVerse.CompareTo(refInitEndVerse) == 0 &&
					//vernLastVerse.CompareTo(refLastVerse) == 0 &&
					((numberOfVernBlocksInVerse == 1 && numberOfRefBlocksInVerse == 1) ||
					(numberOfVernBlocksInVerse == numberOfRefBlocksInVerse &&
					vernBlockList.Skip(indexOfVernVerseStart).Take(numberOfVernBlocksInVerse).Select(b => b.CharacterId).SequenceEqual(refBlockList.Skip(indexOfRefVerseStart).Take(numberOfRefBlocksInVerse).Select(b => b.CharacterId)))))
				{
					for (int i = 0; i < numberOfVernBlocksInVerse; i++)
						vernBlockList[indexOfVernVerseStart + i].SetMatchedReferenceBlock(refBlockList[indexOfRefVerseStart + i]);
				}
				else
				{
					if (oneToOneMatchingOnly)
						throw new Exception("Block matched to more than one reference block: " + currentVernBlock.ToString(true, vernacularBook.BookId));
					currentVernBlock.MatchesReferenceText = false;
					currentVernBlock.ReferenceBlocks = new List<Block>(refBlockList.Skip(indexOfRefVerseStart).Take(numberOfRefBlocksInVerse));
				}
			}
		}

		private static void FindAllScriptureBlocksThroughVerse(IReadOnlyList<Block> blockList, VerseRef endVerse, ref int i, int bookNum, ScrVers versification)
		{
			for (; ; )
			{
				var nextScriptureBlock = blockList.Skip(i + 1).FirstOrDefault(b => !CharacterVerseData.IsCharacterStandard(b.CharacterId, false));
				if (nextScriptureBlock == null)
					return;
				var nextVerseRef = new VerseRef(bookNum, nextScriptureBlock.ChapterNumber, nextScriptureBlock.InitialStartVerseNumber, versification);
				if (nextVerseRef > endVerse)
					return;
				i++;
			}
		}

		private static void SplitVernBlocksToMatchReferenceText(BookScript vernacularBook, List<Block> refBlockList,
			ScrVers vernacularVersification, ScrVers referenceVersification)
		{
			int bookNum = BCVRef.BookToNumber(vernacularBook.BookId);

			var versesToSplitAfter = new List<VerseRef>();
			var versesToSplitBefore = new List<VerseRef>();
			Block prevBlock = null;
			foreach (var refBlock in refBlockList)
			{
				if (prevBlock == null)
				{
					prevBlock = refBlock;
					continue;
				}
				if (refBlock.BlockElements.First() is Verse)
				{
					versesToSplitAfter.Add(new VerseRef(bookNum, prevBlock.ChapterNumber, prevBlock.LastVerse, referenceVersification));
					versesToSplitBefore.Add(new VerseRef(bookNum, refBlock.ChapterNumber, refBlock.InitialStartVerseNumber, referenceVersification));
				}
				prevBlock = refBlock;
			}

			if (!versesToSplitAfter.Any())
				return;

			var iSplit = 0;
			var verseToSplitAfter = versesToSplitAfter[iSplit];
			for (int index = 0; index < vernacularBook.Blocks.Count; index++)
			{
				var vernBlock = vernacularBook.Blocks[index];
				var vernInitStartVerse = new VerseRef(bookNum, vernBlock.ChapterNumber, vernBlock.InitialStartVerseNumber, vernacularVersification);
				VerseRef vernInitEndVerse;
				if (vernBlock.InitialEndVerseNumber != 0)
					vernInitEndVerse = new VerseRef(bookNum, vernBlock.ChapterNumber, vernBlock.InitialEndVerseNumber, vernacularVersification);
				else
					vernInitEndVerse = vernInitStartVerse;

				while (vernInitStartVerse > verseToSplitAfter)
				{
					if (iSplit == versesToSplitAfter.Count - 1)
						return;
					verseToSplitAfter = versesToSplitAfter[++iSplit];
				}

				var vernLastVerse = new VerseRef(bookNum, vernBlock.ChapterNumber, vernBlock.LastVerse, vernacularVersification);
				if (vernLastVerse < verseToSplitAfter)
					continue;

				if (vernInitEndVerse.CompareTo(vernLastVerse) != 0 && vernLastVerse >= versesToSplitBefore[iSplit])
				{
					vernacularVersification.ChangeVersification(verseToSplitAfter);
					if (!vernacularBook.TrySplitBlockAtEndOfVerse(vernBlock, verseToSplitAfter.VerseNum))
					{
						if (iSplit == versesToSplitAfter.Count - 1)
							return;
						verseToSplitAfter = versesToSplitAfter[++iSplit];
						index--;
					}
				}
			}
		}

		protected override string ProjectFolder
		{
			get { return FileLocator.GetDirectoryDistributedWithApplication(kDistFilesReferenceTextDirectoryName, m_referenceTextType.ToString()); }
		}
	}
}
