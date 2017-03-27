﻿#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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

#if (NET_4_5 && PARALLEL_APPENDERS)

namespace log4net.Util
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using log4net.Appender;
	using log4net.Core;

	/// <summary>
	/// This class allows AppenderAttachedImpl class to call appenders in "parallel".
	/// </summary>
	/// <remarks>
	/// <para>
	/// By implementing <see cref="IAppender"/> interface
	/// allows AppenderAttacedImpl to call appenders in "parallel".
	/// That allows to mitigate performance impedance between appenders and
	/// provides better overall performance.
	/// </para>
	/// </remarks>
	/// <author>Harry Martyrossian</author>
	public class ParallelIAppender : IAppender
	{
		private static readonly Type declaringType = typeof(ParallelIAppender);

		private IAppender appender;

		private object synchObject = new object();
		private AutoResetEvent synchEvent = new AutoResetEvent(false);
		private PulseCode pulseCode = PulseCode.NotSignaled;

		private Queue<LoggingEvent> events = new Queue<LoggingEvent>();
		private LoggingEvent[] arrayOfEvents;

		private Thread appenderThread;

		#region Public Instance Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Initializes a new instance of the <see cref="ParallelIAppender"/> class.
		/// </para>
		/// </remarks>

		public ParallelIAppender(IAppender appender)
		{
			this.appender = appender;

			this.appenderThread = new Thread(this.Append);
			this.appenderThread.IsBackground = false;
			this.appenderThread.Name = string.Format("{0}-{1}", appender.Name, Guid.NewGuid().ToString("N"));

			this.appenderThread.Start();
		}

		#endregion Public Instance Constructors

		[Flags]
		internal enum PulseCode : int
		{
			NotSignaled = 0,
			ExitThread
		}

		#region Override implementation of Object

		/// <summary>
		/// Determines whether two <see cref="ParallelIAppender" /> instances 
		/// are equal.
		/// </summary>
		/// <param name="obj">The <see cref="object" /> to compare with the current <see cref="ParallelIAppender" />.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="object" /> is equal to the current <see cref="ParallelIAppender" />; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Compares the implementations of <see cref="IAppender" /> interface.
		/// </para>
		/// </remarks>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ParallelIAppender);
		}

		/// <summary>
		/// Compares implementations of IAppender interface.
		/// </summary>
		/// <param name="obj">The object to compare against.</param>
		/// <returns><c>true</c> if the objects are equal.</returns>
		/// <remarks>
		/// <para>
		/// Compares implementations of <see cref="IAppender" /> interface, and
		/// defers to base class if the target object is not a <see cref="ParallelIAppender" />
		/// instance.
		/// </para>
		/// </remarks>
		public virtual bool Equals(ParallelIAppender obj)
		{
			if (obj == null)
			{
				return false;
			}

			return this.appender.Equals(obj);
		}

		/// <summary>
		/// Returns a hash code
		/// </summary>
		/// <returns>A hash code for the current implementation of the <see cref="IAppender" /> interface.</returns>
		/// <remarks>
		/// <para>
		/// Returns a hash code suitable for use in hashing algorithms and data 
		/// structures like a hash table.
		/// </para>
		/// <para>
		/// Returns the hash code of the <see cref="IAppender"/> interface implementation.
		/// </para>
		/// </remarks>
		public override int GetHashCode()
		{
			return this.appender.GetHashCode();
		}

		#endregion Override implementation of Object

		#region Implementation of IAppender

		string IAppender.Name
		{
			get
			{
				return this.appender.Name;
			}

			set
			{
				this.appender.Name = value;
			}
		}

		void IAppender.Close()
		{
			lock (this.synchObject)
			{
				this.pulseCode |= PulseCode.ExitThread;

				// This call will put Append() thread to the scheduling state:
				this.synchEvent.Set();
			}

			this.appenderThread.Join();

			this.appender.Close();
		}

		void IAppender.DoAppend(LoggingEvent loggingEvent)
		{
			lock (this.synchObject)
			{
				this.events.Enqueue(loggingEvent);

				// This call will put Append() thread to the scheduling state:
				this.synchEvent.Set();
			}
		}

		#endregion Implementation of IAppender

		private void Append()
		{
			// Have to keep this thread running till log4net shutdown:
			bool keepRunning = true;

			do
			{
				// Have to wait for the data to be queued by log4net:
				this.synchEvent.WaitOne();

				lock (this.synchObject)
				{
					this.CopyEvents();

					if ((this.pulseCode & PulseCode.ExitThread) == PulseCode.ExitThread)
					{
						// Setting keepRunning to <false> will exit the thread function:
						keepRunning = false;

						// Clean up:
						this.pulseCode ^= PulseCode.ExitThread;
					}
				}

				this.CallDoAppend();
			}
			while (keepRunning);
		}

		private void CopyEvents()
		{
			this.arrayOfEvents = this.events.ToArray();
			this.events.Clear();
		}

		private void CallDoAppend()
		{
			var length = this.arrayOfEvents.Length;
			for (int i = 0; i < length; ++i)
			{
				this.appender.DoAppend(this.arrayOfEvents[i]);
			}
		}
	}
}

#endif
