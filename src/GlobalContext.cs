#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections;

using log4net.Util;

namespace log4net
{
	/// <summary>
	/// The log4net Global Context.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The GlobalContext provides a global properties map. These
	/// properties can be included in the output of log messages.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public sealed class GlobalContext
	{
		#region Private Instance Constructors

		/// <summary>
		/// Private Constructor. 
		/// </summary>
		/// <remarks>
		/// Uses a private access modifier to prevent instantiation of this class.
		/// </remarks>
		private GlobalContext()
		{
		}

		#endregion Private Instance Constructors

		#region Public Static Properties

		/// <summary>
		/// Get the global properties map
		/// </summary>
		public static GlobalContextProperties Properties
		{
			get { return s_properties; }
		}

		#endregion Public Static Properties

		#region Private Static Fields

		/// <summary>
		/// The global context properties instance
		/// </summary>
		private readonly static GlobalContextProperties s_properties = new GlobalContextProperties();

		#endregion Private Static Fields
	}
}
