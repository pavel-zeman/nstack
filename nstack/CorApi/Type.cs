//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorType : WrapperBase
    {
        internal ICorDebugType m_type;
        
        internal CorType (ICorDebugType type)
            : base(type)
        {
            m_type = type;
        }


        /** Element type of the type. */
        public CorElementType Type
        {
            get 
            {
                CorElementType type;
                m_type.GetType (out type);
                return type;
            }
        }

        /** Class of the type */
        public CorClass Class
        {
            get 
            {
                ICorDebugClass c = null;
                m_type.GetClass(out c);
                return c==null?null:new CorClass (c);
            }
        }

        public int Rank
        {
            get 
            {
                uint pRank= 0;
                m_type.GetRank (out pRank);
                return (int)pRank;
            }
        }

        // Provide the first CorType parameter in the TypeParameters collection.
        // This is a convenience operator.
        public CorType FirstTypeParameter
        {
            get
            {
                ICorDebugType dt = null;
                m_type.GetFirstTypeParameter(out dt);
                return dt==null?null:new CorType (dt);
            }
        }


    } /* class Type */
} /* namespace */
