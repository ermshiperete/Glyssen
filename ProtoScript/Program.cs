﻿using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using L10NSharp;
using Paratext;
using ProtoScript.Properties;
using SIL.IO;
using SIL.Reporting;
using SIL.Windows.Forms.Reporting;
using ConfigurationErrorsException = System.Configuration.ConfigurationErrorsException;

namespace ProtoScript
{
	static class Program
	{
		public const string kCompany = "FCBH-SIL";
		public const string kProduct = "Protoscript Generator";

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			SetUpErrorHandling();
			SetUpLocalization();

			// The following not only gets the location of the settings file;
			// it also detects corruption and deletes it if needed so we don't crash. 
			string userConfigSettingsPath = GetUserConfigFilePath();

			if ((Control.ModifierKeys & Keys.Shift) > 0 && !string.IsNullOrEmpty(userConfigSettingsPath))
				HandleDeleteUserSettings(userConfigSettingsPath);

			// Initialize Paratext versification table based on the vrs files we know ship
			var vrsFolder = Path.GetDirectoryName(FileLocator.GetFileDistributedWithApplication("eng.vrs"));
			foreach (var vrsFile in Directory.GetFiles(vrsFolder, "*.vrs"))
				Versification.Table.Load(vrsFile);
			
			// TODO (PG-18) Add analytics

			Application.Run(new SandboxForm());
		}

		public static string GetUserConfigFilePath()
		{
			try
			{
				return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
			}
			catch (ConfigurationErrorsException e)
			{
				File.Delete(e.Filename);
				return e.Filename;
			}
		}

		private static void HandleDeleteUserSettings(string userConfigSettingsPath)
		{
				var confirmationString = LocalizationManager.GetString("Program.ConfirmDeleteUserSettingsFile",
					"Do you want to delete your user settings? (This will clear your most-recently-used project, publishing settings, UI language settings, etc.  It will not affect your project data.)");

				if (DialogResult.Yes == MessageBox.Show(confirmationString, kProduct, MessageBoxButtons.YesNo, MessageBoxIcon.Warning))
					File.Delete(userConfigSettingsPath);
		}

		private static void SetUpErrorHandling()
		{
			ErrorReport.SetErrorReporter(new WinFormsErrorReporter());
			ErrorReport.EmailAddress = "protoscript_generator@sil.org";
			ErrorReport.AddStandardProperties();
			ExceptionHandler.Init(new WinFormsExceptionHandler());
			// TODO (Analytics): ExceptionHandler.AddDelegate(ReportError);
		}

		//private static void ReportError(object sender, CancelExceptionHandlingEventArgs e)
		//{
		//	Analytics.ReportException(e.Exception);
		//}

		private static void SetUpLocalization()
		{
			string installedStringFileFolder = FileLocator.GetDirectoryDistributedWithApplication("localization");
			string targetTmxFilePath = Path.Combine(kCompany, kProduct);
			string desiredUiLangId = Settings.Default.UserInterfaceLanguage;

			LocalizationManager.Create(desiredUiLangId, "ProtoscriptGenerator", Application.ProductName, Application.ProductVersion,
				installedStringFileFolder, targetTmxFilePath, Resources.PgIcon, IssuesEmailAddress, "ProtoScript");

			// For now, do not set up localization for Palaso
			// TODO, should we?
		}

		/// <summary>
		/// The email address people should write to with issues
		/// </summary>
		public static string IssuesEmailAddress
		{
			// TODO get an email address generated
			get { return "issues@protoscript.palaso.org"; }
		}
	}
}
