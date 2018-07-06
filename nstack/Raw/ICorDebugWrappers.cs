//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Imports ICorDebug interface from CorDebug.idl into managed code.
//---------------------------------------------------------------------


using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using IStream = System.Runtime.InteropServices.ComTypes.IStream;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug.NativeApi
{

   
   
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct COR_IL_MAP
    {
        public uint oldOffset;
        public uint newOffset;
        public int fAccurate;
    }

    public enum CorDebugCreateProcessFlags
    {
        DEBUG_NO_SPECIAL_OPTIONS
    }

    public enum CorDebugExceptionCallbackType
    {
        // Fields
        DEBUG_EXCEPTION_CATCH_HANDLER_FOUND = 3,
        DEBUG_EXCEPTION_FIRST_CHANCE = 1,
        DEBUG_EXCEPTION_UNHANDLED = 4,
        DEBUG_EXCEPTION_USER_FIRST_CHANCE = 2
    }

    public enum CorDebugExceptionUnwindCallbackType
    {
        // Fields
        DEBUG_EXCEPTION_INTERCEPTED = 2,
        DEBUG_EXCEPTION_UNWIND_BEGIN = 1
    }





    [Flags]
    public enum CorDebugUserState
    {
        // Fields
        USER_NONE = 0x00,
        USER_STOP_REQUESTED = 0x01,
        USER_SUSPEND_REQUESTED = 0x02,
        USER_BACKGROUND = 0x04,
        USER_UNSTARTED = 0x08,
        USER_STOPPED = 0x10,
        USER_WAIT_SLEEP_JOIN = 0x20,
        USER_SUSPENDED = 0x40,
        USER_UNSAFE_POINT = 0x80
    }

    [CLSCompliant(true)]
    // Technically 0x40 is the only flag that could be set in combination with others, but we might
    // want to test for the presence of this value so we'll mark the enum as 'Flags'.
    // But in almost all cases we just use one of the individual values.
    // Reflection (Enum.ToString) appears to do a good job of picking the simplest combination to
    // represent a value as a set of flags, but the Visual Studio debugger does not - it just does
    // a linear search from the start looking for matches.  To make debugging these values easier,
    // their order is reversed so that VS always produces the simplest representation.
    [Flags]
    public enum CorElementType
    {
        ELEMENT_TYPE_PINNED = 0x45,
        ELEMENT_TYPE_SENTINEL = 0x41,
        ELEMENT_TYPE_MODIFIER = 0x40,

        ELEMENT_TYPE_MAX = 0x22,

        ELEMENT_TYPE_INTERNAL = 0x21,

        ELEMENT_TYPE_CMOD_OPT = 0x20,
        ELEMENT_TYPE_CMOD_REQD = 0x1f,

        ELEMENT_TYPE_MVAR = 0x1e,
        ELEMENT_TYPE_SZARRAY = 0x1d,
        ELEMENT_TYPE_OBJECT = 0x1c,
        ELEMENT_TYPE_FNPTR = 0x1b,
        ELEMENT_TYPE_U = 0x19,
        ELEMENT_TYPE_I = 0x18,

        ELEMENT_TYPE_TYPEDBYREF = 0x16,
        ELEMENT_TYPE_GENERICINST = 0x15,
        ELEMENT_TYPE_ARRAY = 0x14,
        ELEMENT_TYPE_VAR = 0x13,
        ELEMENT_TYPE_CLASS = 0x12,
        ELEMENT_TYPE_VALUETYPE = 0x11,

        ELEMENT_TYPE_BYREF = 0x10,
        ELEMENT_TYPE_PTR = 0xf,

        ELEMENT_TYPE_STRING = 0xe,
        ELEMENT_TYPE_R8 = 0xd,
        ELEMENT_TYPE_R4 = 0xc,
        ELEMENT_TYPE_U8 = 0xb,
        ELEMENT_TYPE_I8 = 0xa,
        ELEMENT_TYPE_U4 = 0x9,
        ELEMENT_TYPE_I4 = 0x8,
        ELEMENT_TYPE_U2 = 0x7,
        ELEMENT_TYPE_I2 = 0x6,
        ELEMENT_TYPE_U1 = 0x5,
        ELEMENT_TYPE_I1 = 0x4,
        ELEMENT_TYPE_CHAR = 0x3,
        ELEMENT_TYPE_BOOLEAN = 0x2,
        ELEMENT_TYPE_VOID = 0x1,
        ELEMENT_TYPE_END = 0x0
    }

    #region Top-level interfaces
    [ComImport, Guid("3D6F5F61-7538-11D3-8D5B-00104B35E7EF"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICorDebug
    {
        //
        void Initialize();
        //
        void Terminate();
        //
        void SetManagedHandler([In, MarshalAs(UnmanagedType.Interface)] ICorDebugManagedCallback pCallback);
        //
        void SetUnmanagedHandler([In, MarshalAs(UnmanagedType.Interface)] ICorDebugUnmanagedCallback pCallback);
        //
        void CreateProcess([In, MarshalAs(UnmanagedType.LPWStr)] string lpApplicationName, [In, MarshalAs(UnmanagedType.LPWStr)] string lpCommandLine, [In] SECURITY_ATTRIBUTES lpProcessAttributes, [In] SECURITY_ATTRIBUTES lpThreadAttributes, [In] int bInheritHandles, [In] uint dwCreationFlags, [In] IntPtr lpEnvironment, [In, MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory, [In] STARTUPINFO lpStartupInfo, [In] PROCESS_INFORMATION lpProcessInformation, [In] CorDebugCreateProcessFlags debuggingFlags, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
        //
        void DebugActiveProcess([In] uint id, [In] int win32Attach, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
        //
        void EnumerateProcesses([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcessEnum ppProcess);
        //
        void GetProcess([In] uint dwProcessId, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
        //
        void CanLaunchOrAttach([In] uint dwProcessId, [In] int win32DebuggingEnabled);
    }


   
    #endregion // Top-level interfaces

    #region AppDomain, Process
    public enum CorDebugThreadState
    {
        THREAD_RUN,
        THREAD_SUSPEND
    }

    [ComImport, Guid("3D6F5F62-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(1)]
    public interface ICorDebugController
    {

        void Stop([In] uint dwTimeout);

        void Continue([In] int fIsOutOfBand);

        void IsRunning([Out] out int pbRunning);

        void HasQueuedCallbacks([In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [Out] out int pbQueued);

        void EnumerateThreads([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);

        void SetAllThreadsDebugState([In] CorDebugThreadState state, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pExceptThisThread);

        void Detach();

        void Terminate([In] uint exitCode);

        void CanCommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

        void CommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
    }

    [ComImport, ComConversionLoss, InterfaceType(1), Guid("3D6F5F64-7538-11D3-8D5B-00104B35E7EF")]
    public interface ICorDebugProcess : ICorDebugController
    {

        new void Stop([In] uint dwTimeout);

        new void Continue([In] int fIsOutOfBand);

        new void IsRunning([Out] out int pbRunning);

        new void HasQueuedCallbacks([In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [Out] out int pbQueued);

        new void EnumerateThreads([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);

        new void SetAllThreadsDebugState([In] CorDebugThreadState state, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pExceptThisThread);

        new void Detach();

        new void Terminate([In] uint exitCode);

        new void CanCommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

        new void CommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);



        void GetID([Out] out uint pdwProcessId);

        void GetHandle([Out, ComAliasName("HPROCESS*")] out IntPtr phProcessHandle);

        void GetThread([In] uint dwThreadId, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

        void EnumerateObjects([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectEnum ppObjects);

        void IsTransitionStub([In] ulong address, [Out] out int pbTransitionStub);

        void IsOSSuspended([In] uint threadID, [Out] out int pbSuspended);

        void GetThreadContext([In] uint threadID, [In] uint contextSize, [In, ComAliasName("BYTE*")] IntPtr context);

        void SetThreadContext([In] uint threadID, [In] uint contextSize, [In, ComAliasName("BYTE*")] IntPtr context);

        void ReadMemory([In] ulong address, [In] uint size, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, [Out, ComAliasName("SIZE_T*")] out IntPtr read);

        void WriteMemory([In] ulong address, [In] uint size, [In, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, [Out, ComAliasName("SIZE_T*")] out IntPtr written);

        void ClearCurrentException([In] uint threadID);

        void EnableLogMessages([In] int fOnOff);

        void ModifyLogSwitch([In, MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In] int lLevel);

        void EnumerateAppDomains([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomainEnum ppAppDomains);

        void GetObject([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);

        void ThreadForFiberCookie([In] uint fiberCookie, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

        void GetHelperThreadID([Out] out uint pThreadID);
    }

    [ComImport, ComConversionLoss, InterfaceType(1), Guid("CC7BCB05-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugProcessEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugProcess[] processes, [Out] out uint pceltFetched);
    }




    [ComImport, ComConversionLoss, InterfaceType(1), Guid("3D6F5F63-7538-11D3-8D5B-00104B35E7EF")]
    public interface ICorDebugAppDomain : ICorDebugController
    {

        new void Stop([In] uint dwTimeout);

        new void Continue([In] int fIsOutOfBand);

        new void IsRunning([Out] out int pbRunning);

        new void HasQueuedCallbacks([In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [Out] out int pbQueued);

        new void EnumerateThreads([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);

        new void SetAllThreadsDebugState([In] CorDebugThreadState state, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pExceptThisThread);

        new void Detach();

        new void Terminate([In] uint exitCode);

        new void CanCommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);

        new void CommitChanges([In] uint cSnapshots, [In, MarshalAs(UnmanagedType.Interface)] ref ICorDebugEditAndContinueSnapshot pSnapshots, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);



        void GetProcess([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

        void EnumerateAssemblies([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugAssemblyEnum ppAssemblies);

        void GetModuleFromMetaDataInterface([In, MarshalAs(UnmanagedType.IUnknown)] object pIMetaData, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);

        void EnumerateBreakpoints([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugBreakpointEnum ppBreakpoints);

        void EnumerateSteppers([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugStepperEnum ppSteppers);

        void IsAttached([Out] out int pbAttached);

        void GetName([In] uint cchName, [Out] out uint pcchName, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

        void GetObject([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);

        void Attach();

        void GetID([Out] out uint pId);
    }

   
    [ComImport, InterfaceType(1), Guid("63CA1B24-4359-4883-BD57-13F815F58744"), ComConversionLoss]
    public interface ICorDebugAppDomainEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugAppDomain[] values, [Out] out uint pceltFetched);
    }

    #endregion // AppDomain, Process


    #region Assembly

    [ComImport, ComConversionLoss, Guid("DF59507C-D47A-459E-BCE2-6427EAC8FD06"), InterfaceType(1)]
    public interface ICorDebugAssembly
    {

        void GetProcess([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

        void GetAppDomain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);

        void EnumerateModules([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleEnum ppModules);

        void GetCodeBase([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);

        void GetName([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);
    }

  

    [ComImport, Guid("4A2A1EC9-85EC-4BFB-9F15-A89FDFE0FE83"), ComConversionLoss, InterfaceType(1)]
    public interface ICorDebugAssemblyEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugAssembly[] values, [Out] out uint pceltFetched);
    }
    #endregion Assembly


    #region Execution Control

    #region Breakpoints
    [ComImport, InterfaceType(1), Guid("CC7BCAE8-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugBreakpoint
    {

        void Activate([In] int bActive);

        void IsActive([Out] out int pbActive);
    }

    [ComImport, Guid("CC7BCB03-8A68-11D2-983C-0000F808342D"), ComConversionLoss, InterfaceType(1)]
    public interface ICorDebugBreakpointEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugBreakpoint[] breakpoints, [Out] out uint pceltFetched);
    }

    [ComImport, Guid("CC7BCAE9-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugFunctionBreakpoint : ICorDebugBreakpoint
    {

        new void Activate([In] int bActive);

        new void IsActive([Out] out int pbActive);


        void GetFunction([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetOffset([Out] out uint pnOffset);
    }

    #endregion Breakpoints

    #region Stepping
    [Flags]
    public enum CorDebugUnmappedStop
    {
        // Fields
        STOP_ALL = 0xffff,
        STOP_NONE = 0,
        STOP_PROLOG = 1,
        STOP_EPILOG = 2,
        STOP_NO_MAPPING_INFO = 4,
        STOP_OTHER_UNMAPPED = 8,
        STOP_UNMANAGED = 0x10
    }

    public enum CorDebugStepReason
    {
        STEP_NORMAL,
        STEP_RETURN,
        STEP_CALL,
        STEP_EXCEPTION_FILTER,
        STEP_EXCEPTION_HANDLER,
        STEP_INTERCEPT,
        STEP_EXIT
    }
    [Flags]
    public enum CorDebugIntercept
    {
        // Fields
        INTERCEPT_NONE = 0,
        INTERCEPT_ALL = 0xffff,
        INTERCEPT_CLASS_INIT = 1,
        INTERCEPT_EXCEPTION_FILTER = 2,
        INTERCEPT_SECURITY = 4,
        INTERCEPT_CONTEXT_POLICY = 8,
        INTERCEPT_INTERCEPTION = 0x10,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct COR_DEBUG_STEP_RANGE
    {
        public uint startOffset;
        public uint endOffset;
    }

    [ComImport, Guid("CC7BCAEC-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugStepper
    {

        void IsActive([Out] out int pbActive);

        void Deactivate();

        void SetInterceptMask([In] CorDebugIntercept mask);

        void SetUnmappedStopMask([In] CorDebugUnmappedStop mask);

        void Step([In] int bStepIn);

        void StepRange([In] int bStepIn, [In, MarshalAs(UnmanagedType.LPArray)] COR_DEBUG_STEP_RANGE[] ranges, [In] uint cRangeCount);

        void StepOut();

        void SetRangeIL([In] int bIL);
    }

   
    [ComImport, ComConversionLoss, Guid("CC7BCB04-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugStepperEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugStepper[] steppers, [Out] out uint pceltFetched);
    }

    #endregion

    #endregion Execution Control


    #region Class, Type

    [ComImport, Guid("CC7BCAF5-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugClass
    {

        void GetModule([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModule pModule);

        void GetToken([Out] out uint pTypeDef);

        void GetStaticFieldValue([In] uint fieldDef, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFrame pFrame, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
    }

    [ComImport, Guid("B008EA8D-7AB1-43F7-BB20-FBB5A04038AE"), InterfaceType(1)]
    public interface ICorDebugClass2
    {

        void GetParameterizedType([In] CorElementType elementType, [In] uint nTypeArgs, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ICorDebugType[] ppTypeArgs, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);

        void SetJMCStatus([In] int bIsJustMyCode);
    }

    [ComImport, Guid("D613F0BB-ACE1-4C19-BD72-E4C08D5DA7F5"), InterfaceType(1)]
    public interface ICorDebugType
    {

        void GetType([Out] out CorElementType ty);


        void GetClass([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);


        void EnumerateTypeParameters([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTyParEnum);

        void GetFirstTypeParameter([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugType value);

        void GetBase([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugType pBase);

        void GetStaticFieldValue([In] uint fieldDef, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFrame pFrame, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        void GetRank([Out] out uint pnRank);
    }

    [ComImport, Guid("10F27499-9DF2-43CE-8333-A321D7C99CB4"), InterfaceType(1), ComConversionLoss]
    public interface ICorDebugTypeEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugType[] values, [Out] out uint pceltFetched);
    }


    #endregion // Class, Type


    #region Code and Function
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct COR_DEBUG_IL_TO_NATIVE_MAP
    {
        public uint ilOffset;
        public uint nativeStartOffset;
        public uint nativeEndOffset;
    }

    [ComImport, InterfaceType(1), Guid("CC7BCAF4-8A68-11D2-983C-0000F808342D"), ComConversionLoss]
    public interface ICorDebugCode
    {

        void IsIL([Out] out int pbIL);

        void GetFunction([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetAddress([Out] out ulong pStart);

        void GetSize([Out] out uint pcBytes);

        void CreateBreakpoint([In] uint offset, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);


        void GetCode([In] uint startOffset, [In] uint endOffset, [In] uint cBufferAlloc, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] buffer, [Out] out uint pcBufferSize);

        void GetVersionNumber([Out] out uint nVersion);

        void GetILToNativeMapping([In] uint cMap, [Out] out uint pcMap, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] COR_DEBUG_IL_TO_NATIVE_MAP[] map);

        void GetEnCRemapSequencePoints([In] uint cMap, [Out] out uint pcMap, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] offsets);
    }


    [ComImport, Guid("55E96461-9645-45E4-A2FF-0367877ABCDE"), InterfaceType(1), ComConversionLoss]
    public interface ICorDebugCodeEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugCode[] values, [Out] out uint pceltFetched);
    }

    [ComImport, Guid("CC7BCAF3-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugFunction
    {

        void GetModule([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);

        void GetClass([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

        void GetToken([Out] out uint pMethodDef);

        void GetILCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

        void GetNativeCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

        void CreateBreakpoint([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);

        void GetLocalVarSigToken([Out] out uint pmdSig);

        void GetCurrentVersionNumber([Out] out uint pnCurrentVersion);
    }

    [ComImport, InterfaceType(1), Guid("EF0C490B-94C3-4E4D-B629-DDC134C532D8")]
    public interface ICorDebugFunction2
    {

        void SetJMCStatus([In] int bIsJustMyCode);

        void GetJMCStatus([Out] out int pbIsJustMyCode);

        void EnumerateNativeCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCodeEnum ppCodeEnum);

        void GetVersionNumber([Out] out uint pnVersion);
    }


    #endregion Code and Function


    #region Deprecated
    //
    // These interfaces are not used
    //

    [ComImport, InterfaceType(1), Guid("CC7BCB00-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugContext : ICorDebugObjectValue
    {

        new void GetType([Out] out CorElementType pType);

        new void GetSize([Out] out uint pSize);

        new void GetAddress([Out] out ulong pAddress);

        new void CreateBreakpoint([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);


        new void GetClass([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

        new void GetFieldValue([In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass, [In] uint fieldDef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        new void GetVirtualMethod([In] uint memberRef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        new void GetContext([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

        new void IsValueClass([Out] out int pbIsValueClass);

        new void GetManagedCopy([Out, MarshalAs(UnmanagedType.IUnknown)] out object ppObject);

        new void SetFromManagedCopy([In, MarshalAs(UnmanagedType.IUnknown)] object pObject);
    }

    [ComImport, InterfaceType(1), Guid("6DC3FA01-D7CB-11D2-8A95-0080C792E5D8")]
    public interface ICorDebugEditAndContinueSnapshot
    {

        void CopyMetaData([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [Out] out Guid pMvid);

        void GetMvid([Out] out Guid pMvid);

        void GetRoDataRVA([Out] out uint pRoDataRVA);

        void GetRwDataRVA([Out] out uint pRwDataRVA);

        void SetPEBytes([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);

        void SetILMap([In] uint mdFunction, [In] uint cMapSize, [In] ref COR_IL_MAP map);

        void SetPESymbolBytes([In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);
    }

    [ComImport, ComConversionLoss, InterfaceType(1), Guid("F0E18809-72B5-11D2-976F-00A0C9B4D50C")]
    public interface ICorDebugErrorInfoEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, ComAliasName("ICorDebugEditAndContinueErrorInfo**")] IntPtr errors, [Out] out uint pceltFetched);
    }
    #endregion Deprecated



    [ComImport, InterfaceType(1), Guid("CC7BCB01-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugEnum
    {

        void Skip([In] uint celt);

        void Reset();

        void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        void GetCount([Out] out uint pcelt);
    }



    #region Function Evaluation
    [ComImport, InterfaceType(1), Guid("CC7BCAF6-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugEval
    {

        void CallFunction([In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pFunction, [In] uint nArgs, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ICorDebugValue[] ppArgs);

        void NewObject([In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pConstructor, [In] uint nArgs, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ICorDebugValue[] ppArgs);

        void NewObjectNoConstructor([In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass);

        void NewString([In, MarshalAs(UnmanagedType.LPWStr)] string @string);

        void NewArray([In] CorElementType elementType, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass pElementClass, [In] uint rank, [In] ref uint dims, [In] ref uint lowBounds);

        void IsActive([Out] out int pbActive);

        void Abort();

        void GetResult([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppResult);

        void GetThread([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

        void CreateValue([In] CorElementType elementType, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass pElementClass, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
    }

   
    #endregion Function Evaluation



    #region ICorDebugValue

    [ComImport, Guid("CC7BCAF7-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugValue
    {

        void GetType([Out] out CorElementType pType);

        void GetSize([Out] out uint pSize);

        void GetAddress([Out] out ulong pAddress);

        void CreateBreakpoint([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
    }


   
 

    [ComImport, InterfaceType(1), Guid("CC7BCAEB-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugValueBreakpoint : ICorDebugBreakpoint
    {

        new void Activate([In] int bActive);

        new void IsActive([Out] out int pbActive);


        void GetValue([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
    }

    [ComImport, ComConversionLoss, Guid("CC7BCB0A-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugValueEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugValue[] values, [Out] out uint pceltFetched);
    }


   

    [ComImport, InterfaceType(1), Guid("18AD3D6E-B7D2-11D2-BD04-0000F80849BD")]
    public interface ICorDebugObjectValue : ICorDebugValue
    {

        new void GetType([Out] out CorElementType pType);

        new void GetSize([Out] out uint pSize);

        new void GetAddress([Out] out ulong pAddress);

        new void CreateBreakpoint([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);


        void GetClass([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

        void GetFieldValue([In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass, [In] uint fieldDef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        void GetVirtualMethod([In] uint memberRef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetContext([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

        void IsValueClass([Out] out int pbIsValueClass);

        void GetManagedCopy([Out, MarshalAs(UnmanagedType.IUnknown)] out object ppObject);

        void SetFromManagedCopy([In, MarshalAs(UnmanagedType.IUnknown)] object pObject);
    }

   
    [ComImport, ComConversionLoss, Guid("CC7BCB02-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugObjectEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ulong[] objects, [Out] out uint pceltFetched);
    }

    #endregion // ICorDebugValue

    #region Frames and Chains

    #region Chains
    public enum CorDebugChainReason
    {
        // Fields
        CHAIN_CLASS_INIT = 1,
        CHAIN_CONTEXT_POLICY = 8,
        CHAIN_CONTEXT_SWITCH = 0x400,
        CHAIN_DEBUGGER_EVAL = 0x200,
        CHAIN_ENTER_MANAGED = 0x80,
        CHAIN_ENTER_UNMANAGED = 0x100,
        CHAIN_EXCEPTION_FILTER = 2,
        CHAIN_FUNC_EVAL = 0x800,
        CHAIN_INTERCEPTION = 0x10,
        CHAIN_NONE = 0,
        CHAIN_PROCESS_START = 0x20,
        CHAIN_SECURITY = 4,
        CHAIN_THREAD_START = 0x40
    }

    [ComImport, Guid("CC7BCAEE-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugChain
    {

        void GetThread([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);

        void GetStackRange([Out] out ulong pStart, [Out] out ulong pEnd);

        void GetContext([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);

        void GetCaller([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void GetCallee([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void GetPrevious([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void GetNext([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void IsManaged([Out] out int pManaged);

        void EnumerateFrames([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrameEnum ppFrames);

        void GetActiveFrame([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        void GetRegisterSet([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);

        void GetReason([Out] out CorDebugChainReason pReason);
    }

    [ComImport, InterfaceType(1), ComConversionLoss, Guid("CC7BCB08-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugChainEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugChain[] chains, [Out] out uint pceltFetched);
    }
    #endregion // end Chains




    [ComImport, Guid("CC7BCAEF-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugFrame
    {

        void GetChain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void GetCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

        void GetFunction([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetFunctionToken([Out] out uint pToken);

        void GetStackRange([Out] out ulong pStart, [Out] out ulong pEnd);

        void GetCaller([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        void GetCallee([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        void CreateStepper([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
    }

    [ComImport, ComConversionLoss, Guid("CC7BCB07-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugFrameEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugFrame[] frames, [Out] out uint pceltFetched);
    }


    public enum CorDebugMappingResult
    {
        // Fields
        MAPPING_APPROXIMATE = 0x20,
        MAPPING_EPILOG = 2,
        MAPPING_EXACT = 0x10,
        MAPPING_NO_INFO = 4,
        MAPPING_PROLOG = 1,
        MAPPING_UNMAPPED_ADDRESS = 8
    }


    [ComImport, InterfaceType(1), Guid("03E26311-4F76-11D3-88C6-006097945418")]
    public interface ICorDebugILFrame : ICorDebugFrame
    {

        new void GetChain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        new void GetCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

        new void GetFunction([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        new void GetFunctionToken([Out] out uint pToken);

        new void GetStackRange([Out] out ulong pStart, [Out] out ulong pEnd);

        new void GetCaller([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        new void GetCallee([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);


        new void CreateStepper([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);


        void GetIP([Out] out uint pnOffset, [Out] out CorDebugMappingResult pMappingResult);

        void SetIP([In] uint nOffset);

        void EnumerateLocalVariables([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);

        void GetLocalVariable([In] uint dwIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        void EnumerateArguments([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);

        void GetArgument([In] uint dwIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        void GetStackDepth([Out] out uint pDepth);

        void GetStackValue([In] uint dwIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int CanSetIP([In] uint nOffset);
    }

    [ComImport, InterfaceType(1), Guid("5D88A994-6C30-479B-890F-BCEF88B129A5")]
    public interface ICorDebugILFrame2
    {

        void RemapFunction([In] uint newILOffset);

        void EnumerateTypeParameters([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTyParEnum);
    }

    public enum CorDebugInternalFrameType
    {
        STUBFRAME_NONE,
        STUBFRAME_M2U,
        STUBFRAME_U2M,
        STUBFRAME_APPDOMAIN_TRANSITION,
        STUBFRAME_LIGHTWEIGHT_FUNCTION,
        STUBFRAME_FUNC_EVAL,
        STUBFRAME_INTERNALCALL,
        STUBFRAME_CLASS_INIT,
        STUBFRAME_EXCEPTION,
        STUBFRAME_SECURITY,
        STUBFRAME_JIT_COMPILATION,
    }

    [ComImport, InterfaceType(1), Guid("B92CC7F7-9D2D-45C4-BC2B-621FCC9DFBF4")]
    public interface ICorDebugInternalFrame : ICorDebugFrame
    {

        new void GetChain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        new void GetCode([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);

        new void GetFunction([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        new void GetFunctionToken([Out] out uint pToken);

        new void GetStackRange([Out] out ulong pStart, [Out] out ulong pEnd);

        new void GetCaller([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        new void GetCallee([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        new void CreateStepper([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

        void GetFrameType([Out] out CorDebugInternalFrameType pType);
    }


    [ComImport, InterfaceType(1), Guid("C0815BDC-CFAB-447e-A779-C116B454EB5B")]
    public interface ICorDebugInternalFrame2
    {
        void GetAddress([Out] out ulong pAddress);

        void IsCloserToLeaf([In, MarshalAs(UnmanagedType.Interface)] ICorDebugFrame pFrameToCompare,
                            [Out] out int pIsCloser);
    }

    [ComImport, InterfaceType(1), Guid("879CAC0A-4A53-4668-B8E3-CB8473CB187F")]
    public interface ICorDebugRuntimeUnwindableFrame
    {
    }

    #endregion // Frames

    #region Callbacks

    // Unmanaged callback is only used for Interop-debugging to dispatch native debug events.
    [ComImport, Guid("5263E909-8CB5-11D3-BD2F-0000F80849BD"), InterfaceType(1)]
    public interface ICorDebugUnmanagedCallback
    {

        void DebugEvent(IntPtr debugEvent, [In] int fOutOfBand);
    }

    [ComImport, Guid("3D6F5F60-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(1)]
    public interface ICorDebugManagedCallback
    {

        void Breakpoint([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugBreakpoint pBreakpoint);


        void StepComplete([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugStepper pStepper, [In] CorDebugStepReason reason);


        void Break([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread thread);


        void Exception([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In] int unhandled);


        void EvalComplete([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugEval pEval);


        void EvalException([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugEval pEval);


        void CreateProcess([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess);


        void ExitProcess([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess);


        void CreateThread([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread thread);


        void ExitThread([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread thread);


        void LoadModule([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugModule pModule);


        void UnloadModule([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugModule pModule);


        void LoadClass([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass c);


        void UnloadClass([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugClass c);


        void DebuggerError([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In, MarshalAs(UnmanagedType.Error)] int errorHR, [In] uint errorCode);


        void LogMessage([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In] int lLevel, [In, MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In, MarshalAs(UnmanagedType.LPWStr)] string pMessage);


        void LogSwitch([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In] int lLevel, [In] uint ulReason, [In, MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In, MarshalAs(UnmanagedType.LPWStr)] string pParentName);


        void CreateAppDomain([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain);


        void ExitAppDomain([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain);


        void LoadAssembly([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugAssembly pAssembly);


        void UnloadAssembly([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugAssembly pAssembly);


        void ControlCTrap([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess);


        void NameChange([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread);


        void UpdateModuleSymbols([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugModule pModule, [In, MarshalAs(UnmanagedType.Interface)] IStream pSymbolStream);


        void EditAndContinueRemap([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pFunction, [In] int fAccurate);


        void BreakpointSetError([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugBreakpoint pBreakpoint, [In] uint dwError);
    }

    [ComImport, Guid("250E5EEA-DB5C-4C76-B6F3-8C46F12E3203"), InterfaceType(1)]
    public interface ICorDebugManagedCallback2
    {

        void FunctionRemapOpportunity([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pOldFunction, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pNewFunction, [In] uint oldILOffset);


        void CreateConnection([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In] uint dwConnectionId, [In] ref ushort pConnName);


        void ChangeConnection([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In] uint dwConnectionId);


        void DestroyConnection([In, MarshalAs(UnmanagedType.Interface)] ICorDebugProcess pProcess, [In] uint dwConnectionId);


        void Exception([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFrame pFrame, [In] uint nOffset, [In] CorDebugExceptionCallbackType dwEventType, [In] uint dwFlags);


        void ExceptionUnwind([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In] CorDebugExceptionUnwindCallbackType dwEventType, [In] uint dwFlags);


        void FunctionRemapComplete([In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugFunction pFunction);


        void MDANotification([In, MarshalAs(UnmanagedType.Interface)] ICorDebugController pController, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread, [In, MarshalAs(UnmanagedType.Interface)] ICorDebugMDA pMDA);
    }

    [ComImport, Guid("264EA0FC-2591-49AA-868E-835E6515323F"), InterfaceType(1)]
    public interface ICorDebugManagedCallback3
    {
        void CustomNotification([In, MarshalAs(UnmanagedType.Interface)] ICorDebugThread pThread,
                                [In, MarshalAs(UnmanagedType.Interface)] ICorDebugAppDomain pAppDomain);
    }


    #endregion // Callbacks

    #region Module
    [ComImport, ComConversionLoss, InterfaceType(1), Guid("DBA2D8C1-E5C5-4069-8C13-10A7C6ABF43D")]
    public interface ICorDebugModule
    {

        void GetProcess([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

        void GetBaseAddress([Out] out ulong pAddress);

        void GetAssembly([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugAssembly ppAssembly);

        void GetName([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);

        void EnableJITDebugging([In] int bTrackJITInfo, [In] int bAllowJitOpts);

        void EnableClassLoadCallbacks([In] int bClassLoadCallbacks);

        void GetFunctionFromToken([In] uint methodDef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetFunctionFromRVA([In] ulong rva, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

        void GetClassFromToken([In] uint typeDef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

        void CreateBreakpoint([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleBreakpoint ppBreakpoint);

        void GetEditAndContinueSnapshot([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEditAndContinueSnapshot ppEditAndContinueSnapshot);

        void GetMetaDataInterface([In, ComAliasName("REFIID")] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IMetadataImport ppObj);

        void GetToken([Out] out uint pToken);

        void IsDynamic([Out] out int pDynamic);

        void GetGlobalVariableValue([In] uint fieldDef, [Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

        void GetSize([Out] out uint pcBytes);

        void IsInMemory([Out] out int pInMemory);
    }

    
    
    [ComImport, Guid("CC7BCAEA-8A68-11D2-983C-0000F808342D"), InterfaceType(1)]
    public interface ICorDebugModuleBreakpoint : ICorDebugBreakpoint
    {

        new void Activate([In] int bActive);

        new void IsActive([Out] out int pbActive);


        void GetModule([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
    }

    [ComImport, InterfaceType(1), ComConversionLoss, Guid("CC7BCB09-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugModuleEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugModule[] modules, [Out] out uint pceltFetched);
    }


    #endregion Module




    #region MDA
    [CLSCompliant(true)]
    [Flags]
    public enum CorDebugMDAFlags
    {
        // Fields
        None = 0,
        MDA_FLAG_SLIP = 2
    }

    [ComImport, Guid("CC726F2F-1DB7-459B-B0EC-05F01D841B42"), InterfaceType(1)]
    public interface ICorDebugMDA
    {

        void GetName([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);


        void GetDescription([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);


        void GetXML([In] uint cchName, [Out] out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);


        void GetFlags([Out] out CorDebugMDAFlags pFlags);


        void GetOSThreadId([Out] out uint pOsTid);
    }
    #endregion // MDA

    [ComImport, Guid("CC7BCB0B-8A68-11D2-983C-0000F808342D"), ComConversionLoss, InterfaceType(1)]
    public interface ICorDebugRegisterSet
    {

        void GetRegistersAvailable([Out] out ulong pAvailable);


        void GetRegisters([In] ulong mask, [In] uint regCount, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ulong[] regBuffer);


        void SetRegisters([In] ulong mask, [In] uint regCount, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ulong[] regBuffer);


        void GetThreadContext([In] uint contextSize, [In, ComAliasName("BYTE*")] IntPtr context);


        void SetThreadContext([In] uint contextSize, [In, ComAliasName("BYTE*")] IntPtr context);
    }

    #region Threads
    [ComImport, Guid("938C6D66-7FB6-4F69-B389-425B8987329B"), InterfaceType(1)]
    public interface ICorDebugThread
    {

        void GetProcess([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

        void GetID([Out] out uint pdwThreadId);

        void GetHandle([Out] out IntPtr phThreadHandle);

        void GetAppDomain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);

        void SetDebugState([In] CorDebugThreadState state);

        void GetDebugState([Out] out CorDebugThreadState pState);

        void GetUserState([Out] out CorDebugUserState pState);

        void GetCurrentException([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppExceptionObject);

        void ClearCurrentException();

        void CreateStepper([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);

        void EnumerateChains([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChainEnum ppChains);

        void GetActiveChain([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);

        void GetActiveFrame([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);

        void GetRegisterSet([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);

        void CreateEval([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEval ppEval);

        void GetObject([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);
    }

   
    [ComImport, InterfaceType(1), ComConversionLoss, Guid("CC7BCB06-8A68-11D2-983C-0000F808342D")]
    public interface ICorDebugThreadEnum : ICorDebugEnum
    {

        new void Skip([In] uint celt);

        new void Reset();

        new void Clone([Out, MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);

        new void GetCount([Out] out uint pcelt);
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray)] ICorDebugThread[] threads, [Out] out uint pceltFetched);
    }

    #endregion Threads

    
}
