﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Glyssen.Character;
using Glyssen.Shared;
using SIL.Scripture;

namespace Glyssen
{
	public class PortionScript
	{
		public const int kSplitAtEndOfVerse = -999;
		protected string m_id;
		protected List<Block> m_blocks;

		public PortionScript(string id, IEnumerable<Block> blocks)
		{
			m_id = id;
			if (blocks != null)
				m_blocks = blocks.ToList();
		}

		public string Id => m_id;

		public virtual IReadOnlyList<Block> GetScriptBlocks()
		{
			return m_blocks;
		}

		private int GetSplitId(Block blockToSplit, bool userSplit)
		{
			var splitId = blockToSplit.SplitId;
			if (userSplit && splitId == Block.kNotSplit)
				splitId = m_blocks.Max(b => b.SplitId) + 1;
			return splitId;
		}

		public Block SplitBlock(Block blockToSplit, string verseToSplit, int characterOffsetToSplit, bool userSplit = true,
			string characterId = null, ScrVers versification = null)
		{
			var iBlock = m_blocks.IndexOf(blockToSplit);

			if (iBlock < 0)
				throw new ArgumentException(@"Block not found in the list for " + Id, "blockToSplit");

			if (verseToSplit == null && characterOffsetToSplit == 0)
			{
				SplitBeforeBlock(iBlock, GetSplitId(blockToSplit, userSplit), userSplit, characterId, versification);
				return blockToSplit;
			}

			Block newBlock = blockToSplit.SplitBlock(verseToSplit, characterOffsetToSplit);
			if (newBlock == null)
			{
				blockToSplit = m_blocks[++iBlock];
				SplitBeforeBlock(iBlock, GetSplitId(blockToSplit, userSplit), userSplit, characterId, versification);
				return blockToSplit;
			}

			m_blocks.Insert(iBlock + 1, newBlock);
			OnBlocksInserted(iBlock + 1);

			if (userSplit)
			{
				if (string.IsNullOrEmpty(characterId))
				{
					newBlock.SetNonDramaticCharacterId(CharacterVerseData.kUnknownCharacter);
					newBlock.UserConfirmed = false;
				}
				else
				{
					newBlock.Delivery = null;
					if (versification == null)
						throw new ArgumentNullException("versification");
					var bookNum = BCVRef.BookToNumber(Id);
					if (bookNum < 0)
						throw new InvalidOperationException("Attempting a user-originated split of a block which is not part of a known Scripture book. Possible characters cannot be determined.");
					newBlock.SetCharacterIdAndCharacterIdInScript(characterId, BCVRef.BookToNumber(Id), versification);
					newBlock.UserConfirmed = true;
				}

				if (blockToSplit.MultiBlockQuote == MultiBlockQuote.Start)
				{
					blockToSplit.MultiBlockQuote = MultiBlockQuote.None;
					newBlock.MultiBlockQuote = MultiBlockQuote.Start;
				}
				else if (blockToSplit.IsContinuationOfPreviousBlockQuote &&
					iBlock < m_blocks.Count - 2 && m_blocks[iBlock + 2].IsContinuationOfPreviousBlockQuote)
				{
					newBlock.MultiBlockQuote = MultiBlockQuote.Start;
				}

				blockToSplit.ClearReferenceText();
			}
			blockToSplit.SplitId = newBlock.SplitId = GetSplitId(blockToSplit, userSplit);

			return newBlock;
		}

		protected virtual void OnBlocksInserted(int insertionIndex, int countOfInsertedBlocks = 1)
		{
			// No op
		}

		internal string GetVerseStringToUseForSplittingBlock(Block block, int verseNum)
		{
			var firstVerseElement = block.BlockElements.OfType<Verse>().FirstOrDefault();
			if (firstVerseElement == null)
				return null;
			var blockBeginsWithVerse = block.BlockElements.First() is Verse;
			var verseString = verseNum.ToString();

			if (block.InitialEndVerseNumber == verseNum)
			{
				verseString = block.InitialVerseNumberOrBridge;
				if (firstVerseElement.Number != verseString && blockBeginsWithVerse)
				{
					var secondPartOfVerse = block.BlockElements.Skip(2).OfType<Verse>().FirstOrDefault();
					if (secondPartOfVerse == null)
						return null;
					verseString = secondPartOfVerse.Number;
				}
			}
			else if (blockBeginsWithVerse ||
				!(block.InitialEndVerseNumber == 0 && block.InitialStartVerseNumber == verseNum))
			{
				foreach (var verse in block.BlockElements.OfType<Verse>())
				{
					if (verse.Number == verseString)
						break;
					if (verse.EndVerse == verseNum)
					{
						verseString = verse.Number;
						break;
					}
					if (verse.StartVerse >= verseNum)
						return null;
				}
			}

			return verseString;
		}

		public bool TrySplitBlockAtEndOfVerse(Block block, int verseNum)
		{
			var verseString = GetVerseStringToUseForSplittingBlock(block, verseNum);
			if (verseString == null)
				return false;
			try
			{
				var newBlock = SplitBlock(block, verseString, kSplitAtEndOfVerse, false);
				if (block.MultiBlockQuote == MultiBlockQuote.None)
				{
					if (block.IsQuote)
					{
						block.MultiBlockQuote = MultiBlockQuote.Start;
						newBlock.MultiBlockQuote = MultiBlockQuote.Continuation;
					}
				}
				else
				{
					newBlock.MultiBlockQuote = MultiBlockQuote.Continuation;
				}
				if (block.MatchesReferenceText)
				{
					// REVIEW: Should this be First or Single, or do we need to possibly handle the case of a sequence?
					// For now, at least, matching implies there is exactly one reference block.
					var refBlock = block.ReferenceBlocks.Single();
					try
					{
						newBlock.SetMatchedReferenceBlock(refBlock.SplitBlock(verseString, kSplitAtEndOfVerse));
					}
					catch (ArgumentException)
					{
						while (refBlock != null)
						{
							var lastVerseOfRefBlock = refBlock.LastVerse;
							var newRefBlock = new Block(newBlock.StyleTag, newBlock.ChapterNumber, lastVerseOfRefBlock.StartVerse, lastVerseOfRefBlock.EndVerse);
							newRefBlock.BlockElements.Add(new ScriptText(""));
							newBlock.SetMatchedReferenceBlock(newRefBlock);
							newBlock = newRefBlock;
							refBlock = refBlock.ReferenceBlocks.FirstOrDefault();
						}
					}
				}
			}
			catch (ArgumentException)
			{
				return false;
			}
			return true;
		}
		
		protected void SplitBeforeBlock(int indexOfBlockToSplit, int splitId, bool userSplit, string characterId, ScrVers versification, bool reapplyingUserSplits = false)
		{
			if (indexOfBlockToSplit == 0)
				throw new IndexOutOfRangeException("Split cannot occur before first block!");

			if (!reapplyingUserSplits && (m_blocks[indexOfBlockToSplit].MultiBlockQuote == MultiBlockQuote.None || m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote == MultiBlockQuote.None))
				throw new ArgumentException("Split allowed only between blocks that are part of a multi-block quote");

			if (m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote == MultiBlockQuote.Start)
				m_blocks[indexOfBlockToSplit - 1].MultiBlockQuote = MultiBlockQuote.None;

			if (indexOfBlockToSplit < m_blocks.Count - 1 && m_blocks[indexOfBlockToSplit + 1].MultiBlockQuote == MultiBlockQuote.Continuation)
				m_blocks[indexOfBlockToSplit].MultiBlockQuote = MultiBlockQuote.Start;
			else
				m_blocks[indexOfBlockToSplit].MultiBlockQuote = MultiBlockQuote.None;

			m_blocks[indexOfBlockToSplit - 1].SplitId = m_blocks[indexOfBlockToSplit].SplitId = splitId;

			if (userSplit && characterId != null)
				m_blocks[indexOfBlockToSplit].SetCharacterIdAndCharacterIdInScript(characterId, BCVRef.BookToNumber(Id), versification);
		}
	}
}
