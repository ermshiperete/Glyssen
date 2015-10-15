﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DesktopAnalytics;
using Glyssen.Character;
using Glyssen.Properties;
using L10NSharp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SIL.Reporting;

namespace Glyssen
{
	public class ProjectExporter
	{
		public enum FileType
		{
			Excel,
			TabSeparated
		}

		private const string ExcelFileExtension = ".xlsx";
		private const string Separator = "\t";

		private readonly Project m_project;

		public ProjectExporter(Project project)
		{
			m_project = project;
			IncludeVoiceActors = m_project.CharacterGroupList.AnyVoiceActorAssigned();
		}

		public bool IncludeVoiceActors { get; private set; }

		public void Export(IWin32Window owner)
		{
			var defaultDir = Settings.Default.DefaultExportDirectory;
			if (string.IsNullOrEmpty(defaultDir))
			{
				defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

			using (var dlg = new SaveFileDialog())
			{
				dlg.Title = LocalizationManager.GetString("DialogBoxes.ExportDlg.Title", "Export Recording Script");
				dlg.OverwritePrompt = true;
				dlg.InitialDirectory = defaultDir;
				dlg.FileName = m_project.PublicationName + " " + LocalizationManager.GetString("DialogBoxes.ExportDlg.RecordingScriptFileNameDefaultSuffix", "Recording Script");
				dlg.Filter = string.Format("{0} ({1})|{1}|{2} ({3})|{3}|{4} ({5})|{5}",
					LocalizationManager.GetString("DialogBoxes.ExportDlg.ExcelFileTypeLabel", "Excel files"), "*" + ExcelFileExtension,
					LocalizationManager.GetString("DialogBoxes.ExportDlg.TabDelimitedFileTypeLabel", "Tab-delimited files"), "*.txt",
					LocalizationManager.GetString("DialogBoxes.FileDlg.AllFilesLabel", "All Files"), "*.*");
				dlg.DefaultExt = ".xlsx";
				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					Settings.Default.DefaultExportDirectory = Path.GetDirectoryName(dlg.FileName);
					try
					{
						FileType fileType;
						switch (dlg.FilterIndex)
						{
							//1-indexed
							case 2: //.txt
								fileType = FileType.TabSeparated;
								break;
							default:
								fileType = FileType.Excel;
								break;
						}
						GenerateFile(dlg.FileName, fileType);
						Analytics.Track("Export",
							new Dictionary<string, string>
							{
								{ "exportType", fileType.ToString() },
								{ "includeVoiceActors", IncludeVoiceActors.ToString() }
							});
					}
					catch (Exception ex)
					{
						Analytics.ReportException(ex);
						ErrorReport.ReportNonFatalExceptionWithMessage(ex,
							string.Format(LocalizationManager.GetString("File.CouldNotExport", "Could not export data to {0}", "{0} is a file name."), dlg.FileName));
					}
				}
			}
		}

		private void GenerateFile(string path, FileType fileType)
		{
			if (fileType == FileType.Excel && Path.GetExtension(path) != ExcelFileExtension)
				path += ExcelFileExtension;

			switch (fileType)
			{
				case FileType.TabSeparated:
					GenerateTabSeparatedFile(path, GetExportData());
					return;
				default:
					GenerateExcelFile(path, GetExportData());
					return;
			}
		}

		private void GenerateTabSeparatedFile(string path, List<List<object>> data)
		{
			using (var stream = new StreamWriter(path, false, Encoding.UTF8))
				foreach (var line in data)
					stream.WriteLine(GetTabSeparatedLine(line));
		}

		private void GenerateExcelFile(string path, List<List<object>> data)
		{
			// If we got this far with a path to an existing file, the user has (in theory)
			// confirmed he wants to overwrite it.
			// We need to delete it first or the code will attempt to modify it instead.
			File.Delete(path);

			using (var xls = new ExcelPackage(new FileInfo(path)))
			{
				var sheet = xls.Workbook.Worksheets.Add("Script");
				sheet.Cells["A1"].LoadFromArrays(data.Select(d => d.ToArray()).ToArray());
				sheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
				sheet.Column(1).AutoFit(2d, sheet.DefaultColWidth); // line number
				int offset = 0;
				if (IncludeVoiceActors)
				{
					offset = 1;
					sheet.Column(2).AutoFit(2d, 20d); // voice actor
				}
				sheet.Column(2 + offset).AutoFit(2d, sheet.DefaultColWidth); // style tag
				sheet.Column(3 + offset).AutoFit(2d, sheet.DefaultColWidth); // book
				sheet.Column(4 + offset).AutoFit(2d, sheet.DefaultColWidth); // chapter
				sheet.Column(5 + offset).AutoFit(2d, sheet.DefaultColWidth); // verse
				sheet.Column(6 + offset).AutoFit(2d, 20d); // character ID
				sheet.Column(8 + offset).Style.WrapText = true; // script text
				sheet.Column(8 + offset).Width = 50d;
				sheet.Column(9 + offset).AutoFit(2d, sheet.DefaultColWidth); // block length
				xls.Save();
			}
		}

		private List<List<object>> GetExportData()
		{
			int blockNumber = 1;
			var data = new List<List<object>>();
			foreach (var book in m_project.IncludedBooks)
			{
				string singleVoiceNarratorOverride = null;
				if (book.SingleVoice)
					singleVoiceNarratorOverride = CharacterVerseData.GetStandardCharacterId(book.BookId, CharacterVerseData.StandardCharacter.Narrator);
				foreach (var block in book.GetScriptBlocks(true))
				{
					if (IncludeVoiceActors)
					{
						VoiceActor.VoiceActor voiceActor = m_project.GetVoiceActorForCharacter(singleVoiceNarratorOverride ?? block.CharacterIdInScript);
						string voiceActorName = voiceActor != null ? voiceActor.Name : null;
						data.Add(GetExportDataForBlock(block, blockNumber++, book.BookId, voiceActorName ?? "", singleVoiceNarratorOverride, IncludeVoiceActors));
					}
					else
						data.Add(GetExportDataForBlock(block, blockNumber++, book.BookId, null, singleVoiceNarratorOverride, IncludeVoiceActors));
				}
			}
			return data;
		}

		internal static List<object> GetExportDataForBlock(Block block, int blockNumber, string bookId, string voiceActor = null, string singleVoiceNarratorOverride = null, bool useCharacterIdInScript = true)
		{
			// NOTE: if the order here changes, there may be changes needed in GenerateExcelFile
			List<object> list = new List<object>();
			list.Add(blockNumber);
			if (voiceActor != null)
				list.Add(voiceActor);
			list.Add(block.StyleTag);
			list.Add(bookId);
			list.Add(block.ChapterNumber);
			list.Add(block.InitialStartVerseNumber);
			string characterId;
			if (singleVoiceNarratorOverride != null)
				characterId = singleVoiceNarratorOverride;
			else
				characterId = useCharacterIdInScript ? block.CharacterIdInScript : block.CharacterId;
			list.Add(CharacterVerseData.IsCharacterStandard(characterId) ? CharacterVerseData.GetStandardCharacterIdAsEnglish(characterId) : characterId);
			list.Add(block.Delivery);
			list.Add(block.GetText(true));
			list.Add(block.GetText(false).Length);
			return list;
		}

		internal static string GetTabSeparatedLine(List<object> items)
		{
			return string.Join(Separator, items);
		}
	}
}
