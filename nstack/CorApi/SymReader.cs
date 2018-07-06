//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.Samples.Debugging.CorSymbolStore
{
    using System.Diagnostics.SymbolStore;
    using Microsoft.Samples.Debugging.CorDebug;

    // Interface does not need to be marked with the serializable attribute
    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    [
        ComImport,
        Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedReader
    {
        void GetDocument([MarshalAs(UnmanagedType.LPWStr)] String url,
                              Guid language,
                              Guid languageVendor,
                              Guid documentType,
                              [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocument retVal);

        void GetDocuments(int cDocs,
                               out int pcDocs,
                               [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] pDocs);


        // These methods will often return error HRs in common cases.
        // Using PreserveSig and manually handling error cases provides a big performance win.
        // Far fewer exceptions will be thrown and caught.
        // Exceptions should be reserved for truely "exceptional" cases.
        [PreserveSig]
        int GetUserEntryPoint(out SymbolToken EntryPoint);

        [PreserveSig]
        int GetMethod(SymbolToken methodToken,
                          [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

        [PreserveSig]
        int GetMethodByVersion(SymbolToken methodToken,
                                      int version,
                                      [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

        void GetVariables(SymbolToken parent,
                            int cVars,
                            out int pcVars,
                            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ISymUnmanagedVariable[] vars);

        void GetGlobalVariables(int cVars,
                                    out int pcVars,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] vars);


        void GetMethodFromDocumentPosition(ISymUnmanagedDocument document,
                                              int line,
                                              int column,
                                              [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);

        void GetSymAttribute(SymbolToken parent,
                                [MarshalAs(UnmanagedType.LPWStr)] String name,
                                int sizeBuffer,
                                out int lengthBuffer,
                                [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);

        void GetNamespaces(int cNameSpaces,
                                out int pcNameSpaces,
                                [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

        void Initialize(IntPtr importer,
                       [MarshalAs(UnmanagedType.LPWStr)] String filename,
                       [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                       IStream stream);

        void UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                     IStream stream);

        void ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                      IStream stream);

        void GetSymbolStoreFileName(int cchName,
                                           out int pcchName,
                                           [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

        void GetMethodsFromDocumentPosition(ISymUnmanagedDocument document,
                                                      int line,
                                                      int column,
                                                      int cMethod,
                                                      out int pcMethod,
                                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ISymUnmanagedMethod[] pRetVal);

        void GetDocumentVersion(ISymUnmanagedDocument pDoc,
                                      out int version,
                                      out Boolean pbCurrent);

        void GetMethodVersion(ISymUnmanagedMethod pMethod,
                                   out int version);
    };

    [
        ComImport,
        Guid("969708D2-05E5-4861-A3B0-96E473CDF63F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedDispose
    {
        void Destroy();
    }


    internal class SymReader : ISymbolReader, IDisposable
    {

        private ISymUnmanagedReader m_reader; // Unmanaged Reader pointer

        internal SymReader(ISymUnmanagedReader reader)
        {
            // We should not wrap null instances
            if (reader == null)
                throw new ArgumentNullException("reader");

            m_reader = reader;
        }

        public void Dispose()
        {
            // Release our unmanaged resources
            // If the underlying symbol reader supports an explicit dispose interface to release it's resources,
            // then call it.
            ISymUnmanagedDispose disposer = m_reader as ISymUnmanagedDispose;
            if (disposer != null)
            {
                disposer.Destroy();
            }

            m_reader = null;
        }

        public ISymbolDocument GetDocument(String url,
                                        Guid language,
                                        Guid languageVendor,
                                        Guid documentType)
        {
            ISymUnmanagedDocument document = null;
            m_reader.GetDocument(url, language, languageVendor, documentType, out document);
            if (document == null)
            {
                return null;
            }
            return new SymbolDocument(document);
        }

        public ISymbolDocument[] GetDocuments()
        {
            int cDocs = 0;
            m_reader.GetDocuments(0, out cDocs, null);
            ISymUnmanagedDocument[] unmanagedDocuments = new ISymUnmanagedDocument[cDocs];
            m_reader.GetDocuments(cDocs, out cDocs, unmanagedDocuments);

            ISymbolDocument[] documents = new SymbolDocument[cDocs];
            uint i;
            for (i = 0; i < cDocs; i++)
            {
                documents[i] = new SymbolDocument(unmanagedDocuments[i]);
            }
            return documents;
        }

        public SymbolToken UserEntryPoint
        {
            get
            {
                SymbolToken entryPoint;
                int hr = m_reader.GetUserEntryPoint(out entryPoint);
                if (hr == (int)HResult.E_FAIL)
                {
                    // Not all assemblies have entry points
                    // dlls for example...
                    return new SymbolToken(0);
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                return entryPoint;
            }
        }

        public ISymbolMethod GetMethod(SymbolToken method)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            int hr = m_reader.GetMethod(method, out unmanagedMethod);
            if (hr == (int)HResult.E_FAIL)
            {
                // This means that the method has no symbol info because it's probably empty
                // This can happen for virtual methods with no IL
                return null;
            }
            else
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return new SymMethod(unmanagedMethod);
        }

        public ISymbolMethod GetMethod(SymbolToken method, int version)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            int hr = m_reader.GetMethodByVersion(method, version, out unmanagedMethod);
            if (hr == (int)HResult.E_FAIL)
            {
                // This means that the method has no symbol info because it's probably empty
                // This can happen for virtual methods with no IL
                return null;
            }
            else
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return new SymMethod(unmanagedMethod);
        }

        public ISymbolVariable[] GetVariables(SymbolToken parent)
        {
            int cVars = 0;
            uint i;
            m_reader.GetVariables(parent, 0, out cVars, null);
            ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
            m_reader.GetVariables(parent, cVars, out cVars, unmanagedVariables);
            SymVariable[] variables = new SymVariable[cVars];

            for (i = 0; i < cVars; i++)
            {
                variables[i] = new SymVariable(unmanagedVariables[i]);
            }
            return variables;
        }

        public ISymbolVariable[] GetGlobalVariables()
        {
            int cVars = 0;
            uint i;
            m_reader.GetGlobalVariables(0, out cVars, null);
            ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
            m_reader.GetGlobalVariables(cVars, out cVars, unmanagedVariables);
            SymVariable[] variables = new SymVariable[cVars];

            for (i = 0; i < cVars; i++)
            {
                variables[i] = new SymVariable(unmanagedVariables[i]);
            }
            return variables;
        }

        public ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document,
                                                        int line,
                                                        int column)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            m_reader.GetMethodFromDocumentPosition(((SymbolDocument)document).InternalDocument, line, column, out unmanagedMethod);
            return new SymMethod(unmanagedMethod);
        }

        public byte[] GetSymAttribute(SymbolToken parent, String name)
        {
            byte[] Data;
            int cData = 0;
            m_reader.GetSymAttribute(parent, name, 0, out cData, null);
            if (cData == 0)
            {
                // no such attribute (can't distinguish from empty attribute value)
                return null;
            }
            Data = new byte[cData];
            m_reader.GetSymAttribute(parent, name, cData, out cData, Data);
            return Data;
        }

        public ISymbolNamespace[] GetNamespaces()
        {
            int count = 0;
            uint i;
            m_reader.GetNamespaces(0, out count, null);
            ISymUnmanagedNamespace[] unmanagedNamespaces = new ISymUnmanagedNamespace[count];
            m_reader.GetNamespaces(count, out count, unmanagedNamespaces);
            ISymbolNamespace[] namespaces = new SymNamespace[count];

            for (i = 0; i < count; i++)
            {
                namespaces[i] = new SymNamespace(unmanagedNamespaces[i]);
            }
            return namespaces;
        }
    }
}
