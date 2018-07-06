//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Security.Permissions;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;


namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Wraps the native CLR Debugger.
     * Note that we don't derive the class from WrapperBase, becuase this
     * class will never be returned in any callback.
     */
    public sealed class CorDebugger : MarshalByRefObject
    {
        private const int MaxVersionStringLength = 256; // == MAX_PATH

        public static string GetDebuggerVersionFromFile(string pathToExe)
        {
            Debug.Assert(!string.IsNullOrEmpty(pathToExe));
            if (string.IsNullOrEmpty(pathToExe))
                throw new ArgumentException("Value cannot be null or empty.", "pathToExe");
            int neededSize;
            StringBuilder sb = new StringBuilder(MaxVersionStringLength);
            NativeMethods.GetRequestedRuntimeVersion(pathToExe, sb, sb.Capacity, out neededSize);
            return sb.ToString();
        }

        public static string GetDebuggerVersionFromPid(int pid)
        {
            using (ProcessSafeHandle ph = NativeMethods.OpenProcess((int)(NativeMethods.ProcessAccessOptions.ProcessVMRead |
                                                                         NativeMethods.ProcessAccessOptions.ProcessQueryInformation |
                                                                         NativeMethods.ProcessAccessOptions.ProcessDupHandle |
                                                                         NativeMethods.ProcessAccessOptions.Synchronize),
                                                                   false, // inherit handle
                                                                   pid))
            {
                if (ph.IsInvalid)
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                int neededSize;
                StringBuilder sb = new StringBuilder(MaxVersionStringLength);
                NativeMethods.GetVersionFromProcess(ph, sb, sb.Capacity, out neededSize);
                return sb.ToString();
            }
        }


        /// <summary>Creates a debugger interface that is able debug requested version of CLR</summary>
        /// <param name="debuggerVerison">Version number of the debugging interface.</param>
        /// <remarks>The version number is usually retrieved either by calling one of following mscoree functions:
        /// GetCorVerison, GetRequestedRuntimeVersion or GetVersionFromProcess.</remarks>
        public CorDebugger(string debuggerVersion)
        {
            InitFromVersion(debuggerVersion);
        }

       
        /** 
         * Attach to an active process
         */
        public CorProcess DebugActiveProcess(int processId, bool win32Attach)
        {
            ICorDebugProcess proc;
            m_debugger.DebugActiveProcess((uint)processId, win32Attach ? 1 : 0, out proc);
            return CorProcess.GetCorProcess(proc);
        }


        ////////////////////////////////////////////////////////////////////////////////
        //
        // CorDebugger private implement part
        //
        ////////////////////////////////////////////////////////////////////////////////

        // called by constructors during initialization
        private void InitFromVersion(string debuggerVersion)
        {
            if (debuggerVersion.StartsWith("v1"))
            {
                // ICorDebug before V2 did not cooperate well with COM-intop. MDbg's managed
                // wrappers over ICorDebug only work on V2 and beyond.
                throw new ArgumentException("Can't debug a version 1 CLR process (\"" + debuggerVersion +
                    "\").  Run application in a version 2 CLR, or use a version 1 debugger instead.");
            }

            bool fUseV2 = false;
            ICorDebug rawDebuggingAPI = null;
            try
            {
                CLRMetaHost mh = new CLRMetaHost();
                CLRRuntimeInfo rti = mh.GetRuntime(debuggerVersion);
                rawDebuggingAPI = rti.GetLegacyICorDebugInterface();
            }
            catch (NotImplementedException)
            {
                fUseV2 = true;
            }
            catch (EntryPointNotFoundException)
            {
                fUseV2 = true;
            }

            if (fUseV2)
            {
                // fallback to v2 method

                try
                {
                    rawDebuggingAPI = NativeMethods.CreateDebuggingInterfaceFromVersion((int)CorDebuggerVersion.Whidbey, debuggerVersion);
                }
                catch (ArgumentException)
                {
                    // This can commonly happen if:
                    // 1) the debuggee is missing a config file 
                    // 2) the debuggee has a config file for a not-installed CLR.
                    // 
                    // Give a more descriptive error. 
                    // We explicitly don't pass the inner exception because:
                    // - it's uninteresting. It's really just from a pinvoke and so there are no
                    //    extra managed frames.
                    // - MDbg's error reporting will call Exception.GetBaseException() and so just
                    //    grab the inner exception.
                    throw new ArgumentException("Failed to create debugging services for version '" + debuggerVersion + "'");
                }
            }
            Debug.Assert(rawDebuggingAPI != null);
            InitFromICorDebug(rawDebuggingAPI);
        }

        private void InitFromICorDebug(ICorDebug rawDebuggingAPI)
        {
            Debug.Assert(rawDebuggingAPI != null);
            if (rawDebuggingAPI == null)
                throw new ArgumentException("Cannot be null.", "rawDebugggingAPI");

            m_debugger = rawDebuggingAPI;
            m_debugger.Initialize();
            m_debugger.SetManagedHandler(new ManagedCallback(this));
        }

        /**
         * Helper for invoking events.  Checks to make sure that handlers
         * are hooked up to a handler before the handler is invoked.
         *
         * We want to allow maximum flexibility by our callers.  As such,
         * we don't require that they call <code>e.Controller.Continue</code>,
         * nor do we require that this class call it.  <b>Someone</b> needs
         * to call it, however.
         *
         * Consequently, if an exception is thrown and the process is stopped,
         * the process is continued automatically.
         */

        void InternalFireEvent(ManagedCallbackType callbackType, CorEventArgs e)
        {
            CorProcess owner = e.Process;

            Debug.Assert(owner != null);
            try
            {
                owner.DispatchEvent(callbackType, e);
            }
            finally
            {
                // If the callback marked the event to be continued, then call Continue now.
                if (e.Continue)
                {
                    e.Controller.Continue(false);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // ManagedCallback
        //
        ////////////////////////////////////////////////////////////////////////////////

        /**
         * This is the object that gets passed to the debugger.  It's
         * the intermediate "source" of the events, which repackages
         * the event arguments into a more approprate form and forwards
         * the call to the appropriate function.
         */
        private class ManagedCallback : ManagedCallbackBase
        {
            public ManagedCallback(CorDebugger outer)
            {
                m_outer = outer;
            }
            protected override void HandleEvent(ManagedCallbackType eventId, CorEventArgs args)
            {
                m_outer.InternalFireEvent(eventId, args);
            }
            private CorDebugger m_outer;
        }

        private ICorDebug m_debugger = null;
    } /* class Debugger */


    public class ProcessSafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private ProcessSafeHandle()
            : base(true)
        {
        }

        private ProcessSafeHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(handle);
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode=true)]
        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

    public static class NativeMethods
    {
        private const string Kernel32LibraryName = "kernel32.dll";
        private const string Ole32LibraryName = "ole32.dll";
        private const string ShlwapiLibraryName = "shlwapi.dll";
        private const string ShimLibraryName = "mscoree.dll";

        public const int MAX_PATH = 260;

        [
         System.Runtime.ConstrainedExecution.ReliabilityContract(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success),
         DllImport(Kernel32LibraryName)
        ]
        public static extern bool CloseHandle(IntPtr handle);


        [
         DllImport(ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern ICorDebug CreateDebuggingInterfaceFromVersion(int iDebuggerVersion
                                                                           , string szDebuggeeVersion);

        [
         DllImport(ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern void GetVersionFromProcess(ProcessSafeHandle hProcess, StringBuilder versionString,
                                                        Int32 bufferSize, out Int32 dwLength);

        [
         DllImport(ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern void GetRequestedRuntimeVersion(string pExe, StringBuilder pVersion,
                                                             Int32 cchBuffer, out Int32 dwLength);

        [
         DllImport(ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern void CLRCreateInstance(ref Guid clsid, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)]out object metahostInterface);

        public enum ProcessAccessOptions : int
        {
            ProcessTerminate = 0x0001,
            ProcessCreateThread = 0x0002,
            ProcessSetSessionID = 0x0004,
            ProcessVMOperation = 0x0008,
            ProcessVMRead = 0x0010,
            ProcessVMWrite = 0x0020,
            ProcessDupHandle = 0x0040,
            ProcessCreateProcess = 0x0080,
            ProcessSetQuota = 0x0100,
            ProcessSetInformation = 0x0200,
            ProcessQueryInformation = 0x0400,
            ProcessSuspendResume = 0x0800,
            Synchronize = 0x100000,
        }

        [
         DllImport(Kernel32LibraryName, PreserveSig = true)
        ]
        public static extern ProcessSafeHandle OpenProcess(Int32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

        [
         DllImport(Kernel32LibraryName, CharSet = CharSet.Unicode, PreserveSig = true)
        ]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryFullProcessImageName(ProcessSafeHandle hProcess,
                                                            int dwFlags,
                                                            StringBuilder lpExeName,
                                                            ref int lpdwSize);

        public enum Stgm
        {
            StgmRead = 0x00000000,
            StgmWrite = 0x00000001,
            StgmReadWrite = 0x00000002,
            StgmShareDenyNone = 0x00000040,
            StgmShareDenyRead = 0x00000030,
            StgmShareDenyWrite = 0x00000020,
            StgmShareExclusive = 0x00000010,
            StgmPriority = 0x00040000,
            StgmCreate = 0x00001000,
            StgmConvert = 0x00020000,
            StgmFailIfThere = 0x00000000,
            StgmDirect = 0x00000000,
            StgmTransacted = 0x00010000,
            StgmNoScratch = 0x00100000,
            StgmNoSnapshot = 0x00200000,
            StgmSimple = 0x08000000,
            StgmDirectSwmr = 0x00400000,
            StgmDeleteOnRelease = 0x04000000
        }

        // SHCreateStreamOnFile* is used to create IStreams to pass to ICLRMetaHostPolicy::GetRequestedRuntime().
        // Since we can't count on the EX version being available, we have SHCreateStreamOnFile as a fallback.
        [
         DllImport(ShlwapiLibraryName, PreserveSig = false)
        ]
        // Only in version 6 and later
        public static extern void SHCreateStreamOnFileEx([MarshalAs(UnmanagedType.LPWStr)]string file,
                                                        Stgm dwMode,
                                                        Int32 dwAttributes, // Used if a file is created.  Identical to dwFlagsAndAttributes param of CreateFile.
                                                        bool create,
                                                        IntPtr pTemplate,   // Reserved, always pass null.
                                                        [MarshalAs(UnmanagedType.Interface)]out IStream openedStream);

        [
         DllImport(ShlwapiLibraryName, PreserveSig = false)
        ]
        public static extern void SHCreateStreamOnFile(string file,
                                                        Stgm dwMode,
                                                        [MarshalAs(UnmanagedType.Interface)]out IStream openedStream);

    }

    // Wrapper for ICLRMetaHost.  Used to find information about runtimes.
    public sealed class CLRMetaHost
    {
        private ICLRMetaHost m_metaHost;

        public const int MaxVersionStringLength = 26; // 24 + NULL and an extra
        private static readonly Guid clsidCLRMetaHost = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");

        public CLRMetaHost()
        {
            object o;
            Guid ifaceId = typeof(ICLRMetaHost).GUID;
            Guid clsid = clsidCLRMetaHost;
            NativeMethods.CLRCreateInstance(ref clsid, ref ifaceId, out o);
            m_metaHost = (ICLRMetaHost)o;
        }

       
        // Retrieve information about runtimes that are currently loaded into the target process.
        public IEnumerable<CLRRuntimeInfo> EnumerateLoadedRuntimes(Int32 processId)
        {
            List<CLRRuntimeInfo> runtimes = new List<CLRRuntimeInfo>();
            IEnumUnknown enumRuntimes;

            using (ProcessSafeHandle hProcess = NativeMethods.OpenProcess((int)(NativeMethods.ProcessAccessOptions.ProcessVMRead |
                                                                        NativeMethods.ProcessAccessOptions.ProcessQueryInformation |
                                                                        NativeMethods.ProcessAccessOptions.ProcessDupHandle |
                                                                        NativeMethods.ProcessAccessOptions.Synchronize),
                                                                        false, // inherit handle
                                                                        processId))
            {
                if (hProcess.IsInvalid)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                enumRuntimes = m_metaHost.EnumerateLoadedRuntimes(hProcess);
            }

            // Since we're only getting one at a time, we can pass NULL for count.
            // S_OK also means we got the single element we asked for.
            for (object oIUnknown; enumRuntimes.Next(1, out oIUnknown, IntPtr.Zero) == 0; /* empty */)
            {
                runtimes.Add(new CLRRuntimeInfo(oIUnknown));
            }

            return runtimes;
        }

        public CLRRuntimeInfo GetRuntime(string version)
        {
            Guid ifaceId = typeof(ICLRRuntimeInfo).GUID;
            return new CLRRuntimeInfo(m_metaHost.GetRuntime(version, ref ifaceId));
        }
    }


    // You're expected to get this interface from mscoree!GetCLRMetaHost.
    // Details for APIs are in metahost.idl.
    [ComImport, InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), Guid("D332DB9E-B9B3-4125-8207-A14884F53216")]
    internal interface ICLRMetaHost
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        System.Object GetRuntime(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzVersion,
            [In] ref Guid riid /*must use typeof(ICLRRuntimeInfo).GUID*/);

        void GetVersionFromFile(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
            [In, Out] ref uint pcchBuffer);

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumUnknown EnumerateInstalledRuntimes();

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumUnknown EnumerateLoadedRuntimes(
            [In] ProcessSafeHandle hndProcess);
    }


    // Wrapper for ICLRMetaHostPolicy.
    public sealed class CLRMetaHostPolicy
    {
        public enum MetaHostPolicyFlags
        {
            metaHostPolicyHighCompat = 0,
            metaHostPolicyLowFootprint = 1
        }

        private ICLRMetaHostPolicy m_MHPolicy;
        private int MaxVersionStringLength = 26; //24 for version, + 2 terminating NULLs
        private static readonly Guid clsidCLRMetaHostPolicy = new Guid("2EBCD49A-1B47-4a61-B13A-4A03701E594B");

        public CLRMetaHostPolicy()
        {
            object o;
            Guid ifaceId = typeof(ICLRMetaHostPolicy).GUID;
            Guid clsid = clsidCLRMetaHostPolicy;
            NativeMethods.CLRCreateInstance(ref clsid, ref ifaceId, out o);
            m_MHPolicy = (ICLRMetaHostPolicy)o;
        }

        // Returns a CLRRuntimeInfo for the runtime that the specified binary
        // will run against.
        public CLRRuntimeInfo GetRequestedRuntime(MetaHostPolicyFlags flags,
                                                    String binaryPath,
                                                    String configPath,
                                                    ref StringBuilder version,
                                                    ref StringBuilder imageVersion)
        {
            IStream configStream = null;

            if (configPath != null)
            {
                try
                {
                    NativeMethods.SHCreateStreamOnFileEx(configPath,
                                                        NativeMethods.Stgm.StgmRead,
                                                        0,      // We're not creating a file, so no flags needed.
                                                        false,  // Do NOT create a new file.
                                                        IntPtr.Zero,
                                                        out configStream);
                }
                catch (EntryPointNotFoundException)
                {
                    // Fall back on the older method.
                    NativeMethods.SHCreateStreamOnFile(configPath,
                                                        NativeMethods.Stgm.StgmRead,
                                                        out configStream);
                }
            }

            // In case they're empty.
            version.EnsureCapacity(MaxVersionStringLength);
            uint versionCapacity = System.Convert.ToUInt32(version.Capacity);
            imageVersion.EnsureCapacity(MaxVersionStringLength);
            uint imageVersionCapacity = System.Convert.ToUInt32(imageVersion.Capacity);


            Guid ifaceId = typeof(ICLRRuntimeInfo).GUID;
            uint configFlags;
            object o = m_MHPolicy.GetRequestedRuntime(flags,
                                                        binaryPath,
                                                        configStream,
                                                        version,
                                                        ref versionCapacity,
                                                        imageVersion,
                                                        ref imageVersionCapacity,
                                                        out configFlags,
                                                        ref ifaceId);

            return new CLRRuntimeInfo(o);
        }

    }

    // You're expected to get this interface from mscoree!CLRCreateInstance.
    // Details for APIs are in metahost.idl.
    [ComImport, InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), Guid("E2190695-77B2-492E-8E14-C4B3A7FDD593")]
    internal interface ICLRMetaHostPolicy
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        System.Object GetRequestedRuntime([In, ComAliasName("metahost.assembly.MetaHostPolicyFlags")] CLRMetaHostPolicy.MetaHostPolicyFlags dwPolicyFlags,
                                    [In, MarshalAs(UnmanagedType.LPWStr)] string pwzBinary,
                                    [In, MarshalAs(UnmanagedType.Interface)] IStream pCfgStream,
                                    [In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzVersion,
                                    [In, Out] ref uint pcchVersion,
                                    [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzImageVersion,
                                    [In, Out] ref uint pcchImageVersion,
                                    [Out] out uint pdwConfigFlags,
                                    [In] ref Guid riid /* must use typeof(ICLRRuntimeInfo).GUID */);
    }

    // Wrapper for ICLRRuntimeInfo.  Represents information about a CLR install instance.
    public sealed class CLRRuntimeInfo
    {

        private static Guid m_ClsIdClrDebuggingLegacy = new Guid("DF8395B5-A4BA-450b-A77C-A9A47762C520");
        private ICLRRuntimeInfo m_runtimeInfo;

        public CLRRuntimeInfo(System.Object clrRuntimeInfo)
        {
            m_runtimeInfo = (ICLRRuntimeInfo)clrRuntimeInfo;
        }

        public string GetVersionString()
        {
            StringBuilder sb = new StringBuilder(CLRMetaHost.MaxVersionStringLength);
            int verStrLength = sb.Capacity;
            m_runtimeInfo.GetVersionString(sb, ref verStrLength);
            return sb.ToString();
        }

        
        public ICorDebug GetLegacyICorDebugInterface()
        {
            Guid ifaceId = typeof(ICorDebug).GUID;
            Guid clsId = m_ClsIdClrDebuggingLegacy;
            return (ICorDebug)m_runtimeInfo.GetInterface(ref clsId, ref ifaceId);
        }

    }


    // Details about this interface are in metahost.idl.
    [ComImport, InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
    internal interface ICLRRuntimeInfo
    {
        // Marshalling pcchBuffer as int even though it's unsigned. Max version string is 24 characters, so we should not need to go over 2 billion soon.
        void GetVersionString([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
                              [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcchBuffer);

        // Marshalling pcchBuffer as int even though it's unsigned. MAX_PATH is 260, unicode paths are 65535, so we should not need to go over 2 billion soon.
        [PreserveSig]
        int GetRuntimeDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
                                [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcchBuffer);

        int IsLoaded([In] IntPtr hndProcess);

        // Marshal pcchBuffer as int even though it's unsigned. Error strings approaching 2 billion characters are currently unheard-of.
        [LCIDConversion(3)]
        void LoadErrorString([In, MarshalAs(UnmanagedType.U4)] int iResourceID,
                             [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzBuffer,
                             [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcchBuffer,
                             [In] int iLocaleID);

        IntPtr LoadLibrary([In, MarshalAs(UnmanagedType.LPWStr)] string pwzDllName);

        IntPtr GetProcAddress([In, MarshalAs(UnmanagedType.LPStr)] string pszProcName);

        [return: MarshalAs(UnmanagedType.IUnknown)]
        System.Object GetInterface([In] ref Guid rclsid, [In] ref Guid riid);

    }


  
    // Wrapper for standard COM IEnumUnknown, needed for ICLRMetaHost enumeration APIs.
    [ComImport, InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), Guid("00000100-0000-0000-C000-000000000046")]
    internal interface IEnumUnknown
    {

        [PreserveSig]
        int Next(
            [In, MarshalAs(UnmanagedType.U4)]
             int celt,
            [Out, MarshalAs(UnmanagedType.IUnknown)]
            out System.Object rgelt,
            IntPtr pceltFetched);

        [PreserveSig]
        int Skip(
        [In, MarshalAs(UnmanagedType.U4)]
            int celt);

        void Reset();

        void Clone(
            [Out] 
            out IEnumUnknown ppenum);
    }



    ////////////////////////////////////////////////////////////////////////////////
    //
    // CorEvent Classes & Corresponding delegates
    //
    ////////////////////////////////////////////////////////////////////////////////

    /**
     * All of the Debugger events make a Controller available (to specify
     * whether or not to continue the program, or to stop, etc.).
     *
     * This serves as the base class for all events used for debugging.
     *
     * NOTE: If you don't want <b>Controller.Continue(false)</b> to be
     * called after event processing has finished, you need to set the
     * <b>Continue</b> property to <b>false</b>.
     */

    public class CorEventArgs : EventArgs
    {
        private CorController m_controller;

        private bool m_continue;

        private ManagedCallbackType m_callbackType;

        private CorThread m_thread;

        public CorEventArgs(CorController controller)
        {
            m_controller = controller;
            m_continue = true;
        }

        public CorEventArgs(CorController controller, ManagedCallbackType callbackType)
        {
            m_controller = controller;
            m_continue = true;
            m_callbackType = callbackType;
        }

        /** The Controller of the current event. */
        public CorController Controller
        {
            get
            {
                return m_controller;
            }
        }

        /** 
         * The default behavior after an event is to Continue processing
         * after the event has been handled.  This can be changed by
         * setting this property to false.
         */
        public virtual bool Continue
        {
            get
            {
                return m_continue;
            }
            set
            {
                m_continue = value;
            }
        }

        /// <summary>
        /// The type of callback that returned this CorEventArgs object.
        /// </summary>
        public ManagedCallbackType CallbackType
        {
            get
            {
                return m_callbackType;
            }
        }

        /// <summary>
        /// The CorThread associated with the callback event that returned
        /// this CorEventArgs object. If here is no such thread, Thread is null.
        /// </summary>
        public CorThread Thread
        {
            get
            {
                return m_thread;
            }
            protected set
            {
                m_thread = value;
            }
        }

        /// <summary>
        /// The CorProcess associated with this event.
        /// </summary>
        public CorProcess Process
        {
            get
            {
                CorProcess process = m_controller as CorProcess;
                if (process != null)
                {
                    return process;
                }
                else
                {
                    Debug.Assert(m_controller is CorAppDomain);
                    return ((CorAppDomain)m_controller).Process;
                }
            }
        }

    }


    /**
     * This class is used for all events that only have access to the 
     * CorProcess that is generating the event.
     */
    public class CorProcessEventArgs : CorEventArgs
    {
        public CorProcessEventArgs(CorProcess process)
            : base(process)
        {
        }

        public CorProcessEventArgs(CorProcess process, ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnCreateProcess:
                    return "Process Created";
                case ManagedCallbackType.OnProcessExit:
                    return "Process Exited";
                case ManagedCallbackType.OnControlCTrap:
                    break;
            }
            return base.ToString();
        }
    }

    public delegate void CorProcessEventHandler(Object sender,
                                                 CorProcessEventArgs e);


    /**
     * The event arguments for events that contain both a CorProcess
     * and an CorAppDomain.
     */
    public class CorAppDomainEventArgs : CorProcessEventArgs
    {
        private CorAppDomain m_ad;

        public CorAppDomainEventArgs(CorProcess process, CorAppDomain ad)
            : base(process)
        {
            m_ad = ad;
        }

        public CorAppDomainEventArgs(CorProcess process, CorAppDomain ad,
                                      ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
            m_ad = ad;
        }

        /** The AppDomain that generated the event. */
        public CorAppDomain AppDomain
        {
            get
            {
                return m_ad;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnCreateAppDomain:
                    return "AppDomain Created: " + m_ad.Name;
                case ManagedCallbackType.OnAppDomainExit:
                    return "AppDomain Exited: " + m_ad.Name;
            }
            return base.ToString();
        }
    }

    public delegate void CorAppDomainEventHandler(Object sender,
                                                   CorAppDomainEventArgs e);


    /**
     * The base class for events which take an CorAppDomain as their
     * source, but not a CorProcess.
     */
    public class CorAppDomainBaseEventArgs : CorEventArgs
    {
        public CorAppDomainBaseEventArgs(CorAppDomain ad)
            : base(ad)
        {
        }

        public CorAppDomainBaseEventArgs(CorAppDomain ad, ManagedCallbackType callbackType)
            : base(ad, callbackType)
        {
        }

    }


    /**
     * Arguments for events dealing with threads.
     */
    public class CorThreadEventArgs : CorAppDomainBaseEventArgs
    {
        public CorThreadEventArgs(CorAppDomain appDomain, CorThread thread)
            : base(appDomain != null ? appDomain : thread.AppDomain)
        {
            Thread = thread;
        }

        public CorThreadEventArgs(CorAppDomain appDomain, CorThread thread,
            ManagedCallbackType callbackType)
            : base(appDomain != null ? appDomain : thread.AppDomain, callbackType)
        {
            Thread = thread;
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnBreak:
                    return "Break";
                case ManagedCallbackType.OnCreateThread:
                    return "Thread Created";
                case ManagedCallbackType.OnThreadExit:
                    return "Thread Exited";
                case ManagedCallbackType.OnNameChange:
                    return "Name Changed";
            }
            return base.ToString();
        }
    }

    public delegate void CorThreadEventHandler(Object sender,
                                                CorThreadEventArgs e);


    /**
     * Arguments for events involving breakpoints.
     */
    public class CorBreakpointEventArgs : CorThreadEventArgs
    {
        public CorBreakpointEventArgs(CorAppDomain appDomain,
                                       CorThread thread)
            : base(appDomain, thread)
        {
        }

        public CorBreakpointEventArgs(CorAppDomain appDomain,
                                       CorThread thread,
                                       ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnBreakpoint)
            {
                return "Breakpoint Hit";
            }
            return base.ToString();
        }
    }

    public delegate void BreakpointEventHandler(Object sender,
                                                 CorBreakpointEventArgs e);


    /**
     * Arguments for when a Step operation has completed.
     */
    public class CorStepCompleteEventArgs : CorThreadEventArgs
    {

        [CLSCompliant(false)]
        public CorStepCompleteEventArgs(CorAppDomain appDomain, CorThread thread)
            : base(appDomain, thread)
        {
        }

        [CLSCompliant(false)]
        public CorStepCompleteEventArgs(CorAppDomain appDomain, CorThread thread,
                                         ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }


        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnStepComplete)
            {
                return "Step Complete";
            }
            return base.ToString();
        }
    }

    public delegate void StepCompleteEventHandler(Object sender,
                                                   CorStepCompleteEventArgs e);


    /**
     * For events dealing with exceptions.
     */
    public class CorExceptionEventArgs : CorThreadEventArgs
    {
        public CorExceptionEventArgs(CorAppDomain appDomain,
                                      CorThread thread)
            : base(appDomain, thread)
        {
        }

        public CorExceptionEventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

        
    }

    public delegate void CorExceptionEventHandler(Object sender,
                                                   CorExceptionEventArgs e);


    /**
     * For events dealing the evaluation of something...
     */
    public class CorEvalEventArgs : CorThreadEventArgs
    {
        public CorEvalEventArgs(CorAppDomain appDomain, CorThread thread)
            : base(appDomain, thread)
        {
        }

        public CorEvalEventArgs(CorAppDomain appDomain, CorThread thread,
                                 ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnEvalComplete:
                    return "Eval Complete";
                case ManagedCallbackType.OnEvalException:
                    return "Eval Exception";
            }
            return base.ToString();
        }
    }

    public delegate void EvalEventHandler(Object sender, CorEvalEventArgs e);


    /**
     * For events dealing with module loading/unloading.
     */
    public class CorModuleEventArgs : CorAppDomainBaseEventArgs
    {
        CorModule m_managedModule;

        public CorModuleEventArgs(CorAppDomain appDomain, CorModule managedModule)
            : base(appDomain)
        {
            m_managedModule = managedModule;
        }

        public CorModuleEventArgs(CorAppDomain appDomain, CorModule managedModule,
            ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_managedModule = managedModule;
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnModuleLoad:
                    return "Module loaded: " + m_managedModule.Name;
                case ManagedCallbackType.OnModuleUnload:
                    return "Module unloaded: " + m_managedModule.Name;
            }
            return base.ToString();
        }
    }

    public delegate void CorModuleEventHandler(Object sender,
                                                CorModuleEventArgs e);


    /**
     * For events dealing with class loading/unloading.
     */
    public class CorClassEventArgs : CorAppDomainBaseEventArgs
    {
        CorClass m_class;

        public CorClassEventArgs(CorAppDomain appDomain, CorClass managedClass)
            : base(appDomain)
        {
            m_class = managedClass;
        }

        public CorClassEventArgs(CorAppDomain appDomain, CorClass managedClass,
            ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_class = managedClass;
        }

        public override string ToString()
        {
            // I'd like to get the actual class name here, but we don't have 
            // access to the metadata inside the corapi layer. 
            string className = string.Format("{0} typedef={1:X}",
                m_class.Module.Name,
                m_class.Token);

            switch (CallbackType)
            {
                case ManagedCallbackType.OnClassLoad:
                    return "Class loaded: " + className;
                case ManagedCallbackType.OnClassUnload:
                    return "Class unloaded: " + className;
            }
            return base.ToString();
        }
    }

    public delegate void CorClassEventHandler(Object sender,
                                               CorClassEventArgs e);


    /**
     * For events dealing with debugger errors.
     */
    public class CorDebuggerErrorEventArgs : CorProcessEventArgs
    {
        public CorDebuggerErrorEventArgs(CorProcess process)
            : base(process)
        {
        }

        public CorDebuggerErrorEventArgs(CorProcess process, ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
        }

       
        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnDebuggerError)
            {
                return "Debugger Error";
            }
            return base.ToString();
        }
    }

    public delegate void DebuggerErrorEventHandler(Object sender,
                                                    CorDebuggerErrorEventArgs e);


    /**
     * For events dealing with Assemblies.
     */
    public class CorAssemblyEventArgs : CorAppDomainBaseEventArgs
    {
        private CorAssembly m_assembly;
        public CorAssemblyEventArgs(CorAppDomain appDomain,
                                     CorAssembly assembly)
            : base(appDomain)
        {
            m_assembly = assembly;
        }

        public CorAssemblyEventArgs(CorAppDomain appDomain,
                                     CorAssembly assembly, ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_assembly = assembly;
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.OnAssemblyLoad:
                    return "Assembly loaded: " + m_assembly.Name;
                case ManagedCallbackType.OnAssemblyUnload:
                    return "Assembly unloaded: " + m_assembly.Name;
            }
            return base.ToString();
        }
    }

    public delegate void CorAssemblyEventHandler(Object sender,
                                                  CorAssemblyEventArgs e);


    /**
     * For events dealing with logged messages.
     */
    public class CorLogMessageEventArgs : CorThreadEventArgs
    {
        string m_logSwitchName;

        public CorLogMessageEventArgs(CorAppDomain appDomain, CorThread thread, string logSwitchName)
            : base(appDomain, thread)
        {
            m_logSwitchName = logSwitchName;
        }

        public CorLogMessageEventArgs(CorAppDomain appDomain, CorThread thread, string logSwitchName, ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_logSwitchName = logSwitchName;
        }

        

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnLogMessage)
            {
                return "Log message(" + m_logSwitchName + ")";
            }
            return base.ToString();
        }
    }

    public delegate void LogMessageEventHandler(Object sender,
                                                 CorLogMessageEventArgs e);


    /**
     * For events dealing with logged messages.
     */
    public class CorLogSwitchEventArgs : CorThreadEventArgs
    {
        int m_level;

        string m_logSwitchName;

        public CorLogSwitchEventArgs(CorAppDomain appDomain, CorThread thread,
                                      int level, string logSwitchName)
            : base(appDomain, thread)
        {
            m_level = level;
            m_logSwitchName = logSwitchName;
        }

        public CorLogSwitchEventArgs(CorAppDomain appDomain, CorThread thread,
                                      int level, string logSwitchName,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_level = level;
            m_logSwitchName = logSwitchName;
        }


        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnLogSwitch)
            {
                return "Log Switch" + "\n" +
                    "Level: " + m_level + "\n" +
                    "Log Switch Name: " + m_logSwitchName;
            }
            return base.ToString();
        }
    }

    public delegate void LogSwitchEventHandler(Object sender,
                                                CorLogSwitchEventArgs e);

    /**
      * For events dealing with custom notifications.
      */
    public class CorCustomNotificationEventArgs : CorThreadEventArgs
    {
        // constructor
        // Arguments: thread: thread on which the notification occurred
        //            appDomain: appdomain in which the notification occurred
        //            callbackType: the type of the callback for theis event
        public CorCustomNotificationEventArgs(CorThread thread, CorAppDomain appDomain,
                                              ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

        // we're not really doing anything with this (yet), so we don't need much in the
        // way of functionality
        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnCustomNotification)
            {
                return "Custom Notification";
            }
            return base.ToString();
        }
    }

    /**
     * Handler for custom notification events.
     */

    public delegate void CustomNotificationEventHandler(Object sender,
                                                        CorCustomNotificationEventArgs e);

    /**
     * For events dealing with MDA messages.
     */
    public class CorMDAEventArgs : CorProcessEventArgs
    {
        // Thread may be null.
        public CorMDAEventArgs(CorMDA mda, CorThread thread, CorProcess proc)
            : base(proc)
        {
            m_mda = mda;
            Thread = thread;
            //m_proc = proc;
        }

        public CorMDAEventArgs(CorMDA mda, CorThread thread, CorProcess proc,
            ManagedCallbackType callbackType)
            : base(proc, callbackType)
        {
            m_mda = mda;
            Thread = thread;
            //m_proc = proc;
        }

        CorMDA m_mda;
        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnMDANotification)
            {
                return "MDANotification" + "\n" +
                    "Name=" + m_mda.Name + "\n" +
                    "XML=" + m_mda.XML;
            }
            return base.ToString();
        }

        //CorProcess m_proc;
        //CorProcess Process { get { return m_proc; } }
    }

    public delegate void MDANotificationEventHandler(Object sender, CorMDAEventArgs e);



    /**
     * For events dealing module symbol updates.
     */
    public class CorUpdateModuleSymbolsEventArgs : CorModuleEventArgs
    {
        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs(CorAppDomain appDomain,
                                                CorModule managedModule)
            : base(appDomain, managedModule)
        {
        }

        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs(CorAppDomain appDomain,
                                                CorModule managedModule,
                                                ManagedCallbackType callbackType)
            : base(appDomain, managedModule, callbackType)
        {
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnUpdateModuleSymbols)
            {
                return "Module Symbols Updated";
            }
            return base.ToString();
        }
    }

    public delegate void UpdateModuleSymbolsEventHandler(Object sender,
                                                          CorUpdateModuleSymbolsEventArgs e);

    public sealed class CorExceptionInCallbackEventArgs : CorEventArgs
    {
        public CorExceptionInCallbackEventArgs(CorController controller, Exception exceptionThrown)
            : base(controller)
        {
            m_exceptionThrown = exceptionThrown;
        }

        public CorExceptionInCallbackEventArgs(CorController controller, Exception exceptionThrown,
            ManagedCallbackType callbackType)
            : base(controller, callbackType)
        {
            m_exceptionThrown = exceptionThrown;
        }


        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnExceptionInCallback)
            {
                return "Callback Exception: " + m_exceptionThrown.Message;
            }
            return base.ToString();
        }

        private Exception m_exceptionThrown;
    }

    public delegate void CorExceptionInCallbackEventHandler(Object sender,
                                             CorExceptionInCallbackEventArgs e);


    /**
     * Edit and Continue callbacks
     */

    public class CorBreakpointSetErrorEventArgs : CorThreadEventArgs
    {
        public CorBreakpointSetErrorEventArgs(CorAppDomain appDomain,
                                        CorThread thread,
                                        ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnBreakpointSetError)
            {
                return "Error Setting Breakpoint";
            }
            return base.ToString();
        }

    }
    public delegate void CorBreakpointSetErrorEventHandler(Object sender,
                                                           CorBreakpointSetErrorEventArgs e);


    public sealed class CorFunctionRemapOpportunityEventArgs : CorThreadEventArgs
    {
       
        public CorFunctionRemapOpportunityEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           ManagedCallbackType callbackType
                                           )
            : base(appDomain, thread, callbackType)
        {
        }

        

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnFunctionRemapOpportunity)
            {
                return "Function Remap Opportunity";
            }
            return base.ToString();
        }

    }

    public delegate void CorFunctionRemapOpportunityEventHandler(Object sender,
                                                       CorFunctionRemapOpportunityEventArgs e);

    public sealed class CorFunctionRemapCompleteEventArgs : CorThreadEventArgs
    {
        public CorFunctionRemapCompleteEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           ManagedCallbackType callbackType
                                           )
            : base(appDomain, thread, callbackType)
        {
        }

   }

    public delegate void CorFunctionRemapCompleteEventHandler(Object sender,
                                                              CorFunctionRemapCompleteEventArgs e);


    public class CorExceptionUnwind2EventArgs : CorThreadEventArgs
    {

        [CLSCompliant(false)]
        public CorExceptionUnwind2EventArgs(CorAppDomain appDomain, CorThread thread,
                                            CorDebugExceptionUnwindCallbackType eventType,
                                            ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_eventType = eventType;
        }

      
        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnExceptionUnwind2)
            {
                return "Exception unwind\n" +
                    "EventType: " + m_eventType;
            }
            return base.ToString();
        }

        CorDebugExceptionUnwindCallbackType m_eventType;
    }

    public delegate void CorExceptionUnwind2EventHandler(Object sender,
                                                   CorExceptionUnwind2EventArgs e);


    public class CorException2EventArgs : CorThreadEventArgs
    {

       

        [CLSCompliant(false)]
        public CorException2EventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
        }

       
        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.OnException2)
            {
                return "Exception Thrown";
            }
            return base.ToString();
        }

       
    }

    public delegate void CorException2EventHandler(Object sender,
                                                   CorException2EventArgs e);
    

    public enum ManagedCallbackType
    {
        OnBreakpoint,
        OnStepComplete,
        OnBreak,
        OnException,
        OnEvalComplete,
        OnEvalException,
        OnCreateProcess,
        OnProcessExit,
        OnCreateThread,
        OnThreadExit,
        OnModuleLoad,
        OnModuleUnload,
        OnClassLoad,
        OnClassUnload,
        OnDebuggerError,
        OnLogMessage,
        OnLogSwitch,
        OnCreateAppDomain,
        OnAppDomainExit,
        OnAssemblyLoad,
        OnAssemblyUnload,
        OnControlCTrap,
        OnNameChange,
        OnUpdateModuleSymbols,
        OnFunctionRemapOpportunity,
        OnFunctionRemapComplete,
        OnBreakpointSetError,
        OnException2,
        OnExceptionUnwind2,
        OnMDANotification,
        OnExceptionInCallback,
        OnCustomNotification
    }
    internal enum ManagedCallbackTypeCount
    {
        Last = ManagedCallbackType.OnCustomNotification,
    }

    // Helper class to convert from COM-classic callback interface into managed args.
    // Derived classes can overide the HandleEvent method to define the handling.
    abstract public class ManagedCallbackBase : ICorDebugManagedCallback, ICorDebugManagedCallback2, ICorDebugManagedCallback3
    {
        // Derived class overrides this methdos 
        protected abstract void HandleEvent(ManagedCallbackType eventId, CorEventArgs args);

        void ICorDebugManagedCallback.Breakpoint(ICorDebugAppDomain appDomain,
                                ICorDebugThread thread,
                                ICorDebugBreakpoint breakpoint)
        {
            HandleEvent(ManagedCallbackType.OnBreakpoint,
                               new CorBreakpointEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                           thread == null ? null : new CorThread(thread),
                                                           ManagedCallbackType.OnBreakpoint
                                                           ));
        }

        void ICorDebugManagedCallback.StepComplete(ICorDebugAppDomain appDomain,
                                   ICorDebugThread thread,
                                   ICorDebugStepper stepper,
                                   CorDebugStepReason stepReason)
        {
            HandleEvent(ManagedCallbackType.OnStepComplete,
                               new CorStepCompleteEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                            thread == null ? null : new CorThread(thread),
                                                            ManagedCallbackType.OnStepComplete));
        }

        void ICorDebugManagedCallback.Break(
                           ICorDebugAppDomain appDomain,
                           ICorDebugThread thread)
        {
            HandleEvent(ManagedCallbackType.OnBreak,
                               new CorThreadEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      thread == null ? null : new CorThread(thread),
                                                      ManagedCallbackType.OnBreak));
        }

        void ICorDebugManagedCallback.Exception(
                                                 ICorDebugAppDomain appDomain,
                                                 ICorDebugThread thread,
                                                 int unhandled)
        {
            HandleEvent(ManagedCallbackType.OnException,
                               new CorExceptionEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                         thread == null ? null : new CorThread(thread),
                                                         ManagedCallbackType.OnException));
        }
        /* pass false if ``unhandled'' is 0 -- mapping TRUE to true, etc. */

        void ICorDebugManagedCallback.EvalComplete(
                                  ICorDebugAppDomain appDomain,
                                  ICorDebugThread thread,
                                  ICorDebugEval eval)
        {
            HandleEvent(ManagedCallbackType.OnEvalComplete,
                              new CorEvalEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                    thread == null ? null : new CorThread(thread),
                                                    ManagedCallbackType.OnEvalComplete));
        }

        void ICorDebugManagedCallback.EvalException(
                                   ICorDebugAppDomain appDomain,
                                   ICorDebugThread thread,
                                   ICorDebugEval eval)
        {
            HandleEvent(ManagedCallbackType.OnEvalException,
                              new CorEvalEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                    thread == null ? null : new CorThread(thread),
                                                    ManagedCallbackType.OnEvalException));
        }

        void ICorDebugManagedCallback.CreateProcess(
                                   ICorDebugProcess process)
        {
            HandleEvent(ManagedCallbackType.OnCreateProcess,
                              new CorProcessEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                       ManagedCallbackType.OnCreateProcess));
        }

        void ICorDebugManagedCallback.ExitProcess(
                                 ICorDebugProcess process)
        {
            HandleEvent(ManagedCallbackType.OnProcessExit,
                               new CorProcessEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                        ManagedCallbackType.OnProcessExit));
        }

        void ICorDebugManagedCallback.CreateThread(
                                  ICorDebugAppDomain appDomain,
                                  ICorDebugThread thread)
        {
            HandleEvent(ManagedCallbackType.OnCreateThread,
                              new CorThreadEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      thread == null ? null : new CorThread(thread),
                                                      ManagedCallbackType.OnCreateThread));
        }

        void ICorDebugManagedCallback.ExitThread(
                                ICorDebugAppDomain appDomain,
                                ICorDebugThread thread)
        {
            HandleEvent(ManagedCallbackType.OnThreadExit,
                              new CorThreadEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      thread == null ? null : new CorThread(thread),
                                                      ManagedCallbackType.OnThreadExit));
        }

        void ICorDebugManagedCallback.LoadModule(
                                ICorDebugAppDomain appDomain,
                                ICorDebugModule managedModule)
        {
            HandleEvent(ManagedCallbackType.OnModuleLoad,
                              new CorModuleEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      managedModule == null ? null : new CorModule(managedModule),
                                                      ManagedCallbackType.OnModuleLoad));
        }

        void ICorDebugManagedCallback.UnloadModule(
                                  ICorDebugAppDomain appDomain,
                                  ICorDebugModule managedModule)
        {
            HandleEvent(ManagedCallbackType.OnModuleUnload,
                              new CorModuleEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      managedModule == null ? null : new CorModule(managedModule),
                                                      ManagedCallbackType.OnModuleUnload));
        }

        void ICorDebugManagedCallback.LoadClass(
                               ICorDebugAppDomain appDomain,
                               ICorDebugClass c)
        {
            HandleEvent(ManagedCallbackType.OnClassLoad,
                               new CorClassEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                     c == null ? null : new CorClass(c),
                                                     ManagedCallbackType.OnClassLoad));
        }

        void ICorDebugManagedCallback.UnloadClass(
                                 ICorDebugAppDomain appDomain,
                                 ICorDebugClass c)
        {
            HandleEvent(ManagedCallbackType.OnClassUnload,
                              new CorClassEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                     c == null ? null : new CorClass(c),
                                                     ManagedCallbackType.OnClassUnload));
        }

        void ICorDebugManagedCallback.DebuggerError(
                                   ICorDebugProcess process,
                                   int errorHR,
                                   uint errorCode)
        {
            HandleEvent(ManagedCallbackType.OnDebuggerError,
                              new CorDebuggerErrorEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                             ManagedCallbackType.OnDebuggerError));
        }

        void ICorDebugManagedCallback.LogMessage(
                                ICorDebugAppDomain appDomain,
                                ICorDebugThread thread,
                                int level,
                                string logSwitchName,
                                string message)
        {
            HandleEvent(ManagedCallbackType.OnLogMessage,
                               new CorLogMessageEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                          thread == null ? null : new CorThread(thread),
                                                          logSwitchName, 
                                                          ManagedCallbackType.OnLogMessage));
        }

        void ICorDebugManagedCallback.LogSwitch(
                               ICorDebugAppDomain appDomain,
                               ICorDebugThread thread,
                               int level,
                               uint reason,
                               string logSwitchName,
                               string parentName)
        {
            HandleEvent(ManagedCallbackType.OnLogSwitch,
                              new CorLogSwitchEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                         thread == null ? null : new CorThread(thread),
                                                         level, logSwitchName, ManagedCallbackType.OnLogSwitch));
        }

        void ICorDebugManagedCallback.CreateAppDomain(
                                     ICorDebugProcess process,
                                     ICorDebugAppDomain appDomain)
        {
            HandleEvent(ManagedCallbackType.OnCreateAppDomain,
                              new CorAppDomainEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                         appDomain == null ? null : new CorAppDomain(appDomain),
                                                         ManagedCallbackType.OnCreateAppDomain));
        }

        void ICorDebugManagedCallback.ExitAppDomain(
                                   ICorDebugProcess process,
                                   ICorDebugAppDomain appDomain)
        {
            HandleEvent(ManagedCallbackType.OnAppDomainExit,
                              new CorAppDomainEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                         appDomain == null ? null : new CorAppDomain(appDomain),
                                                         ManagedCallbackType.OnAppDomainExit));
        }

        void ICorDebugManagedCallback.LoadAssembly(
                                  ICorDebugAppDomain appDomain,
                                  ICorDebugAssembly assembly)
        {
            HandleEvent(ManagedCallbackType.OnAssemblyLoad,
                              new CorAssemblyEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                        assembly == null ? null : new CorAssembly(assembly),
                                                        ManagedCallbackType.OnAssemblyLoad));
        }

        void ICorDebugManagedCallback.UnloadAssembly(
                                    ICorDebugAppDomain appDomain,
                                    ICorDebugAssembly assembly)
        {
            HandleEvent(ManagedCallbackType.OnAssemblyUnload,
                              new CorAssemblyEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                        assembly == null ? null : new CorAssembly(assembly),
                                                        ManagedCallbackType.OnAssemblyUnload));
        }

        void ICorDebugManagedCallback.ControlCTrap(ICorDebugProcess process)
        {
            HandleEvent(ManagedCallbackType.OnControlCTrap,
                              new CorProcessEventArgs(process == null ? null : CorProcess.GetCorProcess(process),
                                                       ManagedCallbackType.OnControlCTrap));
        }

        void ICorDebugManagedCallback.NameChange(
                                ICorDebugAppDomain appDomain,
                                ICorDebugThread thread)
        {
            HandleEvent(ManagedCallbackType.OnNameChange,
                              new CorThreadEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      thread == null ? null : new CorThread(thread),
                                                      ManagedCallbackType.OnNameChange));
        }

        
        void ICorDebugManagedCallback.UpdateModuleSymbols(
                                         ICorDebugAppDomain appDomain,
                                         ICorDebugModule managedModule,
                                         IStream stream)
        {
            HandleEvent(ManagedCallbackType.OnUpdateModuleSymbols,
                              new CorUpdateModuleSymbolsEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                                  managedModule == null ? null : new CorModule(managedModule),
                                                                  ManagedCallbackType.OnUpdateModuleSymbols));
        }

        void ICorDebugManagedCallback.EditAndContinueRemap(
                                         ICorDebugAppDomain appDomain,
                                         ICorDebugThread thread,
                                         ICorDebugFunction managedFunction,
                                         int isAccurate)
        {
            Debug.Assert(false); //OBSOLETE callback
        }


        void ICorDebugManagedCallback.BreakpointSetError(
                                       ICorDebugAppDomain appDomain,
                                       ICorDebugThread thread,
                                       ICorDebugBreakpoint breakpoint,
                                       UInt32 errorCode)
        {
            HandleEvent(ManagedCallbackType.OnBreakpointSetError,
                              new CorBreakpointSetErrorEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                        thread == null ? null : new CorThread(thread),
                                                        ManagedCallbackType.OnBreakpointSetError));
        }

        void ICorDebugManagedCallback2.FunctionRemapOpportunity(ICorDebugAppDomain appDomain,
                                                                       ICorDebugThread thread,
                                                                       ICorDebugFunction oldFunction,
                                                                       ICorDebugFunction newFunction,
                                                                       uint oldILoffset)
        {
            HandleEvent(ManagedCallbackType.OnFunctionRemapOpportunity,
                                      new CorFunctionRemapOpportunityEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                                               thread == null ? null : new CorThread(thread),
                                                                               ManagedCallbackType.OnFunctionRemapOpportunity));
        }

        void ICorDebugManagedCallback2.FunctionRemapComplete(ICorDebugAppDomain appDomain,
                                                             ICorDebugThread thread,
                                                             ICorDebugFunction managedFunction)
        {
            HandleEvent(ManagedCallbackType.OnFunctionRemapComplete,
                               new CorFunctionRemapCompleteEventArgs(appDomain == null ? null : new CorAppDomain(appDomain),
                                                      thread == null ? null : new CorThread(thread),
                                                      ManagedCallbackType.OnFunctionRemapComplete));
        }

        void ICorDebugManagedCallback2.CreateConnection(ICorDebugProcess process, uint connectionId, ref ushort connectionName)
        {
            Debug.Assert(false);
        }

        void ICorDebugManagedCallback2.ChangeConnection(ICorDebugProcess process, uint connectionId)
        {
            Debug.Assert(false);
        }

        void ICorDebugManagedCallback2.DestroyConnection(ICorDebugProcess process, uint connectionId)
        {
            Debug.Assert(false);
        }

        void ICorDebugManagedCallback2.Exception(ICorDebugAppDomain ad, ICorDebugThread thread,
                                                 ICorDebugFrame frame, uint offset,
                                                 CorDebugExceptionCallbackType eventType, uint flags)
        {
            HandleEvent(ManagedCallbackType.OnException2,
                                      new CorException2EventArgs(ad == null ? null : new CorAppDomain(ad),
                                                        thread == null ? null : new CorThread(thread),
                                                        ManagedCallbackType.OnException2));
        }

        void ICorDebugManagedCallback2.ExceptionUnwind(ICorDebugAppDomain ad, ICorDebugThread thread,
                                                       CorDebugExceptionUnwindCallbackType eventType, uint flags)
        {
            HandleEvent(ManagedCallbackType.OnExceptionUnwind2,
                                      new CorExceptionUnwind2EventArgs(ad == null ? null : new CorAppDomain(ad),
                                                        thread == null ? null : new CorThread(thread),
                                                        eventType,
                                                        ManagedCallbackType.OnExceptionUnwind2));
        }

        // wrapper for CustomNotification event handler to convert argument types
        void ICorDebugManagedCallback3.CustomNotification(ICorDebugThread thread, ICorDebugAppDomain ad)
        {
            HandleEvent(ManagedCallbackType.OnCustomNotification,
                               new CorCustomNotificationEventArgs(thread == null ? null : new CorThread(thread),
                                                                  ad == null ? null : new CorAppDomain(ad),
                                                                  ManagedCallbackType.OnCustomNotification));
        }

        // Get process from controller 
        static private CorProcess GetProcessFromController(ICorDebugController pController)
        {
            CorProcess p;
            ICorDebugProcess p2 = pController as ICorDebugProcess;
            if (p2 != null)
            {
                p = CorProcess.GetCorProcess(p2);
            }
            else
            {
                ICorDebugAppDomain a2 = (ICorDebugAppDomain)pController;
                p = new CorAppDomain(a2).Process;
            }
            return p;
        }

        void ICorDebugManagedCallback2.MDANotification(ICorDebugController pController,
                                                       ICorDebugThread thread,
                                                       ICorDebugMDA pMDA)
        {
            CorMDA c = new CorMDA(pMDA);
            string szName = c.Name;
            CorDebugMDAFlags f = c.Flags;
            CorProcess p = GetProcessFromController(pController);


            HandleEvent(ManagedCallbackType.OnMDANotification,
                                      new CorMDAEventArgs(c,
                                                           thread == null ? null : new CorThread(thread),
                                                           p, ManagedCallbackType.OnMDANotification));
        }


    }

} /* namespace */