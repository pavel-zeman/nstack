//---------------------------------------------------------------------
//  This file is part of the Managed Stack Explorer (MSE).
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


//[assembly: SecurityPermission(SecurityAction.RequestMinimum)]
namespace Microsoft.Mse.Library
{

	/// <summary>
	/// Represents info about a specifc point in a function
	/// </summary>
	public class SourcePosition
	{

		//private members
		//internal string fixedFile = null; //<strip>@TODO ENC HACK diasymreader</strip> saves the ENC file name
		internal string path;
		private int startLine;

		//constructors
		/// <summary>
		/// Contructor of SourcePosition type.
		/// </summary>
		/// <param name="path">Path for the source file.</param>
		/// <param name="startLine">Start line of the location in the source file.</param>  
		public SourcePosition(string pa, int startL)
		{
			// special sequence points are handled elsewhere.

			path = pa;
			startLine = startL;
		}

		//properties

		/// <summary>
		/// Same as StartLine.
		/// </summary>
		/// <value>StartLine.</value>
		public int Line
		{
			get
			{
				return startLine;
			}
		}

		
		
		
		/// <summary>
		/// Gets the Path for the source file.
		/// </summary>
		/// <value>The Path.</value>
		public string Path
		{
			get
			{
				
				return path;
			}
		}
	}
}