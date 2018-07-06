//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorMDA : WrapperBase
    {
        private ICorDebugMDA m_mda;
        internal CorMDA(ICorDebugMDA mda)
            :base(mda)
        {
            m_mda = mda;
        }

        public CorDebugMDAFlags Flags
        {
            get
            {
                CorDebugMDAFlags flags;
                m_mda.GetFlags(out flags);
                return flags;
            }
        }

        string m_cachedName = null;
        public string Name        
        {
            get 
            {
                // This is thread safe because even in a race, the loser will just do extra work.
                // but no harm done.
                if (m_cachedName == null)
                {
                    uint len = 0;
                    m_mda.GetName(0, out len, null);
                                    
                    char[] name = new char[len];
                    uint fetched = 0;

                    m_mda.GetName ((uint) name.Length, out fetched, name);
                    // ``fetched'' includes terminating null; String doesn't handle null, so we "forget" it.
                    m_cachedName = new String (name, 0, (int) (fetched-1));
                }
                return m_cachedName;               
            } // end get
        }

        public string XML
        {
            get 
            {
                uint len = 0;
                m_mda.GetXML(0, out len, null);
                                
                char[] name = new char[len];
                uint fetched = 0;

                m_mda.GetXML ((uint) name.Length, out fetched, name);
                // ``fetched'' includes terminating null; String doesn't handle null, so we "forget" it.
                return new String (name, 0, (int) (fetched-1));
            }            
        }

      
      
    } // end CorMDA

    public sealed class CorModule : WrapperBase
    {
        private ICorDebugModule m_module;

        internal CorModule (ICorDebugModule managedModule)
            :base(managedModule)
        {
            m_module = managedModule;
        }

       
        /** The assembly this module is in. */
        public CorAssembly Assembly
        {
            get
            {
                ICorDebugAssembly a = null;
                m_module.GetAssembly (out a);
                return new CorAssembly (a);
            }
        }

        /** The name of the module. */
        public String Name
        {
            get
            {
                char[] name = new Char[300];
                uint fetched = 0;
                m_module.GetName ((uint) name.Length, out fetched, name);
                // ``fetched'' includes terminating null; String doesn't handle null,
                // so we "forget" it.
                return new String (name, 0, (int) (fetched-1));
            }
        }

       
     
        /// <summary>
        /// Typesafe wrapper around GetMetaDataInterface. 
        /// </summary>
        /// <typeparam name="T">type of interface to query for</typeparam>
        /// <returns>interface to the metadata</returns>
        public T GetMetaDataInterface<T>()
        {
            // Ideally, this would be declared as Object to match the unmanaged
            // CorDebug.idl definition; but the managed wrappers we build
            // on import it as an IMetadataImport, so we need to start with
            // that. 
            IMetadataImport obj;
            Guid interfaceGuid = typeof(T).GUID;
            m_module.GetMetaDataInterface(ref interfaceGuid, out obj);
            return (T) obj;
        }
    } /* class Module */
} /* namespace */
