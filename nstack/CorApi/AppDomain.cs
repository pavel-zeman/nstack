//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Text;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorAppDomain : CorController
    {
        /** Create an CorAppDomain object. */
        internal CorAppDomain (ICorDebugAppDomain appDomain)
            : base (appDomain)
        {
        }

        /** Get the ICorDebugAppDomain interface back from the Controller. */
        private ICorDebugAppDomain _ad ()
        {
            return (ICorDebugAppDomain) GetController();
        }

        /** Get the process containing the CorAppDomain. */
        public CorProcess Process
        {
            get
            {
                ICorDebugProcess proc = null;
                _ad().GetProcess (out proc);
                return  CorProcess.GetCorProcess (proc);
            }
        }

        /** The name of the CorAppDomain */
        public String Name
        {
            get 
            {
                uint size = 0;
                _ad().GetName (0, out size,  null);
                StringBuilder szName = new StringBuilder((int)size);
                _ad().GetName ((uint)szName.Capacity, out size,  szName);
                return szName.ToString();
            }
        }

        /** 
         * Attach the AppDomain to receive all CorAppDomain related events (e.g.
         * load assembly, load module, etc.) in order to debug the AppDomain.
         */
        public void Attach ()
        {
            _ad().Attach ();
        }

        /** Get the ID of this CorAppDomain */
        public int Id
        {
            get
            {
                uint id = 0;
                _ad().GetID (out id);
                return (int) id;
            }
        }
    }
}
