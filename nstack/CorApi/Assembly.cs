//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Information about an Assembly being debugged.
     */
    public sealed class CorAssembly : WrapperBase
    {
        private ICorDebugAssembly m_asm;

        internal CorAssembly (ICorDebugAssembly managedAssembly)
            :base(managedAssembly)
        { 
            m_asm = managedAssembly;
        }

        /** Get the AppDomain containing the assembly. */
        public CorAppDomain AppDomain
        {
            get 
            {
                ICorDebugAppDomain ad = null;
                m_asm.GetAppDomain (out ad);
                return new CorAppDomain (ad);
            }
        }

        
        /** The name of the assembly. */
        public String Name
        {
            get 
            {
                char[] name = new char[300];
                uint sz = 0;
                m_asm.GetName ((uint) name.Length, out sz, name);
                // ``sz'' includes terminating null; String doesn't handle null,
                // so we "forget" it.
                return new String (name, 0, (int) (sz-1));
            }
        }

    } /* class Assembly */
} /* namespace debugging */
