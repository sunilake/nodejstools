﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.NodejsTools.Commands;
using Microsoft.NodejsTools.Debugger.DebugEngine;
using Microsoft.NodejsTools.Debugger.Remote;
using Microsoft.NodejsTools.Options;
using Microsoft.NodejsTools.Project;
using Microsoft.NodejsTools.Repl;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudioTools;
using Microsoft.Win32;

namespace Microsoft.NodejsTools {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", AssemblyVersionInfo.Version, IconResourceID = 400)]
    [Guid(GuidList.guidNodePkgString)]
    [ProvideOptionPage(typeof(NodejsGeneralOptionsPage), "Node.js Tools", "General", 114, 115, true)]
    [ProvideDebugEngine("Node.js Debugging", typeof(AD7ProgramProvider), typeof(AD7Engine), AD7Engine.DebugEngineId)]
    [ProvideLanguageService(typeof(NodejsLanguageInfo), NodejsConstants.Nodejs, 106, RequestStockColors = true, ShowSmartIndent = true, ShowCompletion = true, DefaultToInsertSpaces = true, HideAdvancedMembersByDefault = true, EnableAdvancedMembersOption = true, ShowDropDownOptions = true)]
    [ProvideDebugLanguage(NodejsConstants.Nodejs, GuidList.guidNodejsDebugLanguageStr, NodeExpressionEvaluatorGuid, AD7Engine.DebugEngineId)]
    [ProvideBraceCompletion(NodejsConstants.Nodejs)]
    [WebSiteProject("JavaScript", "JavaScript")]
    // Keep declared exceptions in sync with those given default values in NodeDebugger.GetDefaultExceptionTreatments()
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOENT)", State = enum_EXCEPTION_STATE.EXCEPTION_NONE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EACCES)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EADDRINUSE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EADDRNOTAVAIL)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EAFNOSUPPORT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EAGAIN)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EWOULDBLOCK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EALREADY)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EBADF)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EBADMSG)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EBUSY)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ECANCELED)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ECHILD)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ECONNABORTED)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ECONNREFUSED)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ECONNRESET)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EDEADLK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EDESTADDRREQ)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EDOM)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EEXIST)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EFAULT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EFBIG)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EHOSTUNREACH)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EIDRM)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EILSEQ)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EINPROGRESS)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EINTR)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EINVAL)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EIO)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EISCONN)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EISDIR)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ELOOP)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EMFILE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EMLINK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EMSGSIZE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENAMETOOLONG)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENETDOWN)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENETRESET)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENETUNREACH)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENFILE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOBUFS)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENODATA)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENODEV)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOENT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOEXEC)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOLINK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOLCK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOMEM)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOMSG)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOPROTOOPT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOSPC)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOSR)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOSTR)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOSYS)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTCONN)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTDIR)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTEMPTY)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTSOCK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTSUP)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENOTTY)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ENXIO)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EOVERFLOW)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EPERM)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EPIPE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EPROTO)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EPROTONOSUPPORT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EPROTOTYPE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ERANGE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EROFS)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ESPIPE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ESRCH)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ETIME)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ETIMEDOUT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(ETXTBSY)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(EXDEV)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGHUP)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGINT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGILL)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGABRT)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGFPE)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGKILL)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGSEGV)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGTERM)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGBREAK)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "Error", "Error(SIGWINCH)", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "EvalError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "RangeError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "ReferenceError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "SyntaxError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "TypeError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideDebugException(AD7Engine.DebugEngineId, "Node.js Exceptions", "URIError", State = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE)]
    [ProvideProjectFactory(typeof(NodejsProjectFactory), null, null, null, null, ".\\NullPath", LanguageVsTemplate = NodejsConstants.Nodejs)]   // outer flavor, no file extension
    [ProvideDebugPortSupplier("Node remote debugging", typeof(NodeRemoteDebugPortSupplier), NodeRemoteDebugPortSupplier.PortSupplierId)]
    [ProvideMenuResource(1000, 1)]                              // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideEditorExtension2(typeof(NodejsEditorFactory), NodeJsFileType, 50, "*:1", ProjectGuid = "{78D985FC-2CA0-4D08-9B6B-35ACD5E5294A}", NameResourceID = 102, DefaultName = "server", TemplateDir="FileTemplates\\NewItem")]
    [ProvideEditorExtension2(typeof(NodejsEditorFactoryPromptForEncoding), NodeJsFileType, 50, "*:1", ProjectGuid = "{78D985FC-2CA0-4D08-9B6B-35ACD5E5294A}", NameResourceID = 113, DefaultName = "server")]
    [ProvideProjectItem(typeof(BaseNodeProjectFactory), NodejsConstants.Nodejs, "FileTemplates\\NewItem", 0)]
    [ProvideLanguageTemplates("{349C5851-65DF-11DA-9384-00065B846F21}", NodejsConstants.Nodejs, GuidList.guidNodePkgString, "Web", "Node.js Project Templates", "{" + BaseNodeProjectFactory.BaseNodeProjectGuid + "}", ".js", NodejsConstants.Nodejs, "{" + BaseNodeProjectFactory.BaseNodeProjectGuid + "}")]
    [ProvideTextEditorAutomation(NodejsConstants.Nodejs, 106, 102, ProfileMigrationType.PassThrough)]
    internal sealed class NodejsPackage : CommonPackage {
        internal const string NodeExpressionEvaluatorGuid = "{F16F2A71-1C45-4BAB-BECE-09D28CFDE3E6}";
        private IContentType _contentType;
        internal const string NodeJsFileType = ".njs";
        internal static readonly Guid _jsLangSvcGuid = new Guid("{71d61d27-9011-4b17-9469-d20f798fb5c0}");
        internal static NodejsPackage Instance;
        private string _surveyNewsUrl;
        private object _surveyNewsUrlLock = new object();

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public NodejsPackage() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            Debug.Assert(Instance == null, "NodejsPackage created multiple times");
            Instance = this;
        }

        public NodejsGeneralOptionsPage GeneralOptionsPage {
            get {
                return (NodejsGeneralOptionsPage)GetDialogPage(typeof(NodejsGeneralOptionsPage));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            var langService = new NodejsLanguageInfo(this);
            ((IServiceContainer)this).AddService(langService.GetType(), langService, true);

            RegisterProjectFactory(new NodejsProjectFactory(this));
            RegisterEditorFactory(new NodejsEditorFactory(this));
            RegisterEditorFactory(new NodejsEditorFactoryPromptForEncoding(this));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            RegisterCommands(new Command[] { 
                new OpenReplWindowCommand(),
                new OpenRemoteDebugProxyFolderCommand(),
                new SurveyNewsCommand(),
            }, GuidList.guidNodeCmdSet);
        }

        internal void OpenReplWindow() {
            var compModel = ComponentModel;
            var provider = compModel.GetService<IReplWindowProvider>();

            var window = provider.FindReplWindow(NodejsReplEvaluatorProvider.NodeReplId);
            if (window == null) {
                window = provider.CreateReplWindow(
                    ReplContentType,
                    "Node.js Interactive Window",
                    _jsLangSvcGuid,
                    NodejsReplEvaluatorProvider.NodeReplId
                );
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)((ToolWindowPane)window).Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
            ((IReplWindow)window).Focus();
        }

        internal static bool TryGetStartupFileAndDirectory(out string fileName, out string directory) {
            var startupProject = GetStartupProject();
            if (startupProject != null) {
                fileName = startupProject.GetStartupFile();
                directory = startupProject.GetWorkingDirectory();                
            } else {
                var textView = CommonPackage.GetActiveTextView();
                if (textView == null) {
                    fileName = null;
                    directory = null;
                    return false;
                }
                fileName = textView.GetFilePath();
                directory = Path.GetDirectoryName(fileName);
            }
            return true;
        }

        private static string remoteDebugProxyFolder = null;
        public static string RemoteDebugProxyFolder {
            get {
                // Lazily evaluated
                if (remoteDebugProxyFolder != null) {
                    return remoteDebugProxyFolder;
                }

                // Try HKCU
                try {
                    using (
                        RegistryKey software = Registry.CurrentUser.OpenSubKey("Software"),
                        microsoft = software.OpenSubKey("Microsoft"),
                        node = microsoft.OpenSubKey("NodeJSTools")
                    ) {
                        if (node != null) {
                            remoteDebugProxyFolder = (string)node.GetValue("RemoteDebugProxyScript");
                        }
                    }
                } catch (Exception) {
                }

                // Try HKLM
                if (remoteDebugProxyFolder == null) {
                    try {
                        using (
                            RegistryKey software = Registry.LocalMachine.OpenSubKey("Software"),
                            microsoft = software.OpenSubKey("Microsoft"),
                            node = microsoft.OpenSubKey("NodeJSTools")
                        ) {
                            if (node != null) {
                                remoteDebugProxyFolder = (string)node.GetValue("RemoteDebugProxyScript");
                            }
                        }
                    } catch (Exception) {
                    }
                }

                return remoteDebugProxyFolder;
            }
        }

        private IContentType ReplContentType {
            get {
                if (_contentType == null) {
                    _contentType = ComponentModel.GetService<IContentTypeRegistryService>().GetContentType(NodejsConstants.NodejsRepl);
                }
                return _contentType;
            }
        }

        #endregion

        internal override VisualStudioTools.Navigation.LibraryManager CreateLibraryManager(CommonPackage package) {
            return new NodejsLibraryManager(this);
        }

        public override Type GetLibraryManagerType() {
            return typeof(NodejsLibraryManager);
        }

        public override bool IsRecognizedFile(string filename) {
            var ext = Path.GetExtension(filename);

            return String.Equals(ext, NodejsConstants.FileExtension, StringComparison.OrdinalIgnoreCase);
        }

        internal new object GetService(Type serviceType) {
            return base.GetService(serviceType);
        }

        public static string NodejsReferencePath {
            get {
                return Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "nodejsref.js"
                );
            }
        }

        public string BrowseForDirectory(IntPtr owner, string initialDirectory = null) {
            IVsUIShell uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (null == uiShell) {
                using (var ofd = new FolderBrowserDialog()) {
                    ofd.RootFolder = Environment.SpecialFolder.Desktop;
                    ofd.ShowNewFolderButton = false;
                    DialogResult result;
                    if (owner == IntPtr.Zero) {
                        result = ofd.ShowDialog();
                    } else {
                        result = ofd.ShowDialog(NativeWindow.FromHandle(owner));
                    }
                    if (result == DialogResult.OK) {
                        return ofd.SelectedPath;
                    } else {
                        return null;
                    }
                }
            }

            if (owner == IntPtr.Zero) {
                ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out owner));
            }

            VSBROWSEINFOW[] browseInfo = new VSBROWSEINFOW[1];
            browseInfo[0].lStructSize = (uint)Marshal.SizeOf(typeof(VSBROWSEINFOW));
            browseInfo[0].pwzInitialDir = initialDirectory;
            browseInfo[0].hwndOwner = owner;
            browseInfo[0].nMaxDirName = 260;
            IntPtr pDirName = IntPtr.Zero;
            try {
                browseInfo[0].pwzDirName = pDirName = Marshal.AllocCoTaskMem(520);
                int hr = uiShell.GetDirectoryViaBrowseDlg(browseInfo);
                if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) {
                    return null;
                }
                ErrorHandler.ThrowOnFailure(hr);
                return Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
            } finally {
                if (pDirName != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pDirName);
                }
            }
        }

        private void BrowseSurveyNewsOnIdle(object sender, ComponentManagerEventArgs e) {
            this.OnIdle -= BrowseSurveyNewsOnIdle;

            lock (_surveyNewsUrlLock) {
                if (!string.IsNullOrEmpty(_surveyNewsUrl)) {
                    OpenVsWebBrowser(_surveyNewsUrl);
                    _surveyNewsUrl = null;
                }
            }
        }

        internal void BrowseSurveyNews(string url) {
            lock (_surveyNewsUrlLock) {
                _surveyNewsUrl = url;
            }

            this.OnIdle += BrowseSurveyNewsOnIdle;
        }

        private void CheckSurveyNewsThread(Uri url, bool warnIfNoneAvailable) {
            // We can't use a simple WebRequest, because that doesn't have access
            // to the browser's session cookies.  Cookies are used to remember
            // which survey/news item the user has submitted/accepted.  The server 
            // checks the cookies and returns the survey/news urls that are 
            // currently available (availability is determined via the survey/news 
            // item start and end date).
            var th = new Thread(() => {
                var br = new WebBrowser();
                br.Tag = warnIfNoneAvailable;
                br.DocumentCompleted += OnSurveyNewsDocumentCompleted;
                br.Navigate(url);
                Application.Run();
            });
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void OnSurveyNewsDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
            var br = (WebBrowser)sender;
            var warnIfNoneAvailable = (bool)br.Tag;
            if (br.Url == e.Url) {
                List<string> available = null;

                string json = br.DocumentText;
                if (!string.IsNullOrEmpty(json)) {
                    int startIndex = json.IndexOf("<PRE>");
                    if (startIndex > 0) {
                        int endIndex = json.IndexOf("</PRE>", startIndex);
                        if (endIndex > 0) {
                            json = json.Substring(startIndex + 5, endIndex - startIndex - 5);

                            try {
                                // Example JSON data returned by the server:
                                //{
                                // "cannotvoteagain": [], 
                                // "notvoted": [
                                //  "http://ptvs.azurewebsites.net/news/141", 
                                //  "http://ptvs.azurewebsites.net/news/41", 
                                // ], 
                                // "canvoteagain": [
                                //  "http://ptvs.azurewebsites.net/news/51"
                                // ]
                                //}

                                // Description of each list:
                                // voted: cookie found
                                // notvoted: cookie not found
                                // canvoteagain: cookie found, but multiple votes are allowed
                                JavaScriptSerializer serializer = new JavaScriptSerializer();
                                var results = serializer.Deserialize<Dictionary<string, List<string>>>(json);
                                available = results["notvoted"];
                            } catch (ArgumentException) {
                            } catch (InvalidOperationException) {
                            }
                        }
                    }
                }

                if (available != null && available.Count > 0) {
                    BrowseSurveyNews(available[0]);
                } else if (warnIfNoneAvailable) {
                    if (available != null) {
                        BrowseSurveyNews(GeneralOptionsPage.SurveyNewsIndexUrl);
                    } else {
                        BrowseSurveyNews(NodejsToolsInstallPath.GetFile("NoSurveyNewsFeed.html"));
                    }
                }

                Application.ExitThread();
            }
        }

        internal void CheckSurveyNews(bool forceCheckAndWarnIfNoneAvailable) {
            bool shouldQueryServer = false;
            if (forceCheckAndWarnIfNoneAvailable) {
                shouldQueryServer = true;
            } else {
                shouldQueryServer = true;
                var options = GeneralOptionsPage;
                // Ensure that we don't prompt the user on their very first project creation.
                // Delay by 3 days by pretending we checked 4 days ago (the default of check
                // once a week ensures we'll check again in 3 days).
                if (options.SurveyNewsLastCheck == DateTime.MinValue) {
                    options.SurveyNewsLastCheck = DateTime.Now - TimeSpan.FromDays(4);
                    options.SaveSettingsToStorage();
                }

                var elapsedTime = DateTime.Now - options.SurveyNewsLastCheck;
                switch (options.SurveyNewsCheck) {
                    case SurveyNewsPolicy.Disabled:
                        break;
                    case SurveyNewsPolicy.CheckOnceDay:
                        shouldQueryServer = elapsedTime.TotalDays >= 1;
                        break;
                    case SurveyNewsPolicy.CheckOnceWeek:
                        shouldQueryServer = elapsedTime.TotalDays >= 7;
                        break;
                    case SurveyNewsPolicy.CheckOnceMonth:
                        shouldQueryServer = elapsedTime.TotalDays >= 30;
                        break;
                    default:
                        Debug.Assert(false, String.Format("Unexpected SurveyNewsPolicy: {0}.", options.SurveyNewsCheck));
                        break;
                }
            }

            if (shouldQueryServer) {
                var options = GeneralOptionsPage;
                options.SurveyNewsLastCheck = DateTime.Now;
                options.SaveSettingsToStorage();
                CheckSurveyNewsThread(new Uri(options.SurveyNewsFeedUrl), forceCheckAndWarnIfNoneAvailable);
            }
        }

#if UNIT_TEST_INTEGRATION
        // var testCase = require('./test/test-doubled.js'); for(var x in testCase) { console.log(x); }
        public static string EvaluateJavaScript(string code) {
            // TODO: Escaping code
            string args = "-e \"" + code + "\"";
            var psi = new ProcessStartInfo(NodePath, args);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            var proc = Process.Start(psi);
            var outpReceiver = new OutputReceiver();
            proc.OutputDataReceived += outpReceiver.DataRead;
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            return outpReceiver._data.ToString();
        }

        private void GetTestCases(string module) {
            var testCases = EvaluateJavaScript(
                String.Format("var testCase = require('{0}'); for(var x in testCase) { console.log(x); }", module));
            foreach (var testCase in testCases.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
            }
        }

        class OutputReceiver {
            internal readonly StringBuilder _data = new StringBuilder();
            
            public void DataRead(object sender, DataReceivedEventArgs e) {
                if (e.Data != null) {
                    _data.Append(e.Data);
                }
            }
        }
#endif
    }
}