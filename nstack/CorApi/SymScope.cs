//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.Samples.Debugging.CorSymbolStore
{
    using System.Diagnostics.SymbolStore;

    // Interface does not need to be marked with the serializable attribute
    using System;
    using System.Runtime.InteropServices;

    [
        ComImport,
        Guid("68005D0F-B8E0-3B01-84D5-A11A94154942"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedScope
    {
        void GetMethod([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod pRetVal);

        void GetParent([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedScope pRetVal);

        void GetChildren(int cChildren,
                            out int pcChildren,
                            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

        void GetStartOffset(out int pRetVal);

        void GetEndOffset(out int pRetVal);

        void GetLocalCount(out int pRetVal);

        void GetLocals(int cLocals,
                          out int pcLocals,
                          [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

        void GetNamespaces(int cNameSpaces,
                              out int pcNameSpaces,
                              [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
    };


    internal class SymScope : ISymbolScope
    {
        ISymUnmanagedScope m_target;

        internal SymScope(ISymUnmanagedScope target)
        {
            // We should not wrap null instances
            if (target == null)
                throw new ArgumentNullException("target");

            m_target = target;
        }

        public ISymbolMethod Method
        {
            get
            {
                ISymUnmanagedMethod uMethod = null;
                m_target.GetMethod(out uMethod);
                if (uMethod == null)
                    return null;
                return new SymMethod(uMethod);
            }
        }

        public ISymbolScope Parent
        {
            get
            {
                ISymUnmanagedScope uScope = null;
                m_target.GetParent(out uScope);
                if (uScope == null)
                    return null;
                return new SymScope(uScope);
            }
        }

        public ISymbolScope[] GetChildren()
        {
            int count;
            m_target.GetChildren(0, out count, null);
            ISymUnmanagedScope[] uScopes = new ISymUnmanagedScope[count];
            m_target.GetChildren(count, out count, uScopes);

            int i;
            ISymbolScope[] scopes = new ISymbolScope[count];
            for (i = 0; i < count; i++)
            {
                scopes[i] = new SymScope(uScopes[i]);
            }
            return scopes;
        }

        public int StartOffset
        {
            get
            {
                int offset;
                m_target.GetStartOffset(out offset);
                return offset;
            }
        }


        public int EndOffset
        {
            get
            {
                int offset;
                m_target.GetEndOffset(out offset);
                return offset;
            }
        }

        public ISymbolVariable[] GetLocals()
        {
            int count;
            m_target.GetLocals(0, out count, null);
            ISymUnmanagedVariable[] uVariables = new ISymUnmanagedVariable[count];
            m_target.GetLocals(count, out count, uVariables);

            int i;
            ISymbolVariable[] variables = new ISymbolVariable[count];
            for (i = 0; i < count; i++)
            {
                variables[i] = new SymVariable(uVariables[i]);
            }
            return variables;
        }

        public ISymbolNamespace[] GetNamespaces()
        {
            int count;
            m_target.GetNamespaces(0, out count, null);
            ISymUnmanagedNamespace[] uNamespaces = new ISymUnmanagedNamespace[count];
            m_target.GetNamespaces(count, out count, uNamespaces);

            int i;
            ISymbolNamespace[] namespaces = new ISymbolNamespace[count];
            for (i = 0; i < count; i++)
            {
                namespaces[i] = new SymNamespace(uNamespaces[i]);
            }
            return namespaces;
        }

      
    }
}
