﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProtoScript.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string UserInterfaceLanguage {
            get {
                return ((string)(this["UserInterfaceLanguage"]));
            }
            set {
                this["UserInterfaceLanguage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string CurrentProject {
            get {
                return ((string)(this["CurrentProject"]));
            }
            set {
                this["CurrentProject"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultBundleDirectory {
            get {
                return ((string)(this["DefaultBundleDirectory"]));
            }
            set {
                this["DefaultBundleDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultExportDirectory {
            get {
                return ((string)(this["DefaultExportDirectory"]));
            }
            set {
                this["DefaultExportDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("24")]
        public string PgUsxParserVersion {
            get {
                return ((string)(this["PgUsxParserVersion"]));
            }
            set {
                this["PgUsxParserVersion"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int DefaultFontSize {
            get {
                return ((int)(this["DefaultFontSize"]));
            }
            set {
                this["DefaultFontSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string DefaultSfmDirectory {
            get {
                return ((string)(this["DefaultSfmDirectory"]));
            }
            set {
                this["DefaultSfmDirectory"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool AssignCharactersShowGridView {
            get {
                return ((bool)(this["AssignCharactersShowGridView"]));
            }
            set {
                this["AssignCharactersShowGridView"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::SIL.Windows.Forms.PortableSettingsProvider.FormSettings AssignCharacterDialogFormSettings {
            get {
                return ((global::SIL.Windows.Forms.PortableSettingsProvider.FormSettings)(this["AssignCharacterDialogFormSettings"]));
            }
            set {
                this["AssignCharacterDialogFormSettings"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::SIL.Windows.Forms.PortableSettingsProvider.GridSettings AssignCharactersBlockContextGrid {
            get {
                return ((global::SIL.Windows.Forms.PortableSettingsProvider.GridSettings)(this["AssignCharactersBlockContextGrid"]));
            }
            set {
                this["AssignCharactersBlockContextGrid"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool QuoteMarksDialogShowGridView {
            get {
                return ((bool)(this["QuoteMarksDialogShowGridView"]));
            }
            set {
                this["QuoteMarksDialogShowGridView"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int DataFormatVersion {
            get {
                return ((int)(this["DataFormatVersion"]));
            }
        }
    }
}
