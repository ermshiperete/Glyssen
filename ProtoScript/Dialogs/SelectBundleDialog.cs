﻿using System;
using System.Windows.Forms;

namespace ProtoScript.Dialogs
{
	public class SelectBundleDialog : IDisposable
	{
		private const string kResourceBundleExtension = ".zip";
		private readonly OpenFileDialog m_fileDialog;

		public SelectBundleDialog()
		{
			m_fileDialog = new OpenFileDialog
			{
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Filter = "Zip files|*" + kResourceBundleExtension
			};
		}

		public void ShowDialog()
		{
			if (m_fileDialog.ShowDialog() == DialogResult.OK)
				FileName = m_fileDialog.FileName;
		}

		public string FileName { get; private set; }

		public void Dispose()
		{
			m_fileDialog.Dispose();
		}
	}
}
