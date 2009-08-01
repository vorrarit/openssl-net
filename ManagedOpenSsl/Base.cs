﻿// Copyright (c) 2006-2009 Frank Laub
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. The name of the author may not be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace OpenSSL
{
	#region Base
	/// <summary>
	/// Base class for all openssl wrapped objects. 
	/// Contains the raw unmanaged pointer and has a Handle property to get access to it. 
	/// Also overloads the ToString() method with a BIO print.
	/// </summary>
	public abstract class Base : IStackable, IDisposable
	{
		/// <summary>
		/// Raw unmanaged pointer
		/// </summary>
		protected IntPtr ptr;

		/// <summary>
		/// If this object is the owner, then call the appropriate native free function.
		/// </summary>
		protected bool owner = false;

		/// <summary>
		/// This is to prevent double-deletion issues.
		/// </summary>
		protected bool isDisposed = false;

		/// <summary>
		/// This destructor just calls Dispose().
		/// </summary>
		~Base() {
			Dispose();
		}

		/// <summary>
		/// gets/sets whether the object owns the Native pointer
		/// </summary>
		public virtual bool IsOwner {
			get { return owner; }
			set { owner = value; }
		}

		/// <summary>
		/// Access to the raw unmanaged pointer. Implements the IStackable interface.
		/// </summary>
		public virtual IntPtr Handle {
			get { return this.ptr; }
			set {
				if (this.owner && this.ptr != IntPtr.Zero) {
					this.OnDispose();
					DoAfterDispose();
				}
				this.owner = false;
				this.ptr = value;
				if (this.ptr != IntPtr.Zero) {
					GC.ReRegisterForFinalize(this);
					this.OnNewHandle(this.ptr);
				}
			}
		}

		/// <summary>
		/// Do nothing in the base class.
		/// </summary>
		/// <param name="ptr"></param>
		protected virtual void OnNewHandle(IntPtr ptr) {
		}

		public virtual void Addref() {
		}

		/// <summary>
		/// Constructor which takes the raw unmanged pointer. 
		/// This is the only way to construct this object and all dervied types.
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="takeOwnership"></param>
		public Base(IntPtr ptr, bool takeOwnership) {
			this.ptr = ptr;
			this.owner = takeOwnership;
		}

		/// <summary>
		/// This method is used by the ToString() implementation. A great number of
		/// openssl objects support printing, so this is a conveinence method.
		/// Dervied types should override this method and not ToString().
		/// </summary>
		/// <param name="bio">The BIO stream object to print into</param>
		public virtual void Print(BIO bio) { }

		/// <summary>
		/// Override of ToString() which uses Print() into a BIO memory buffer.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			try {
				if (this.ptr == IntPtr.Zero)
					return "(null)";

				using (BIO bio = BIO.MemoryBuffer()) {
					this.Print(bio);
					return bio.ReadString();
				}
			}
			catch (Exception) {
				return "<exception>";
			}
		}

		/// <summary>
		/// Default base implementation does nothing.
		/// </summary>
		protected abstract void OnDispose();

		#region IDisposable Members

		/// <summary>
		/// Implementation of the IDisposable interface.
		/// If the native pointer is not null, we haven't been disposed, and we are the owner,
		/// then call the virtual OnDispose() method.
		/// </summary>
		public void Dispose() {
			if (!this.isDisposed && this.owner && this.ptr != IntPtr.Zero) {
				this.OnDispose();
				DoAfterDispose();
			}
			this.isDisposed = true;
		}

		#endregion

		private void DoAfterDispose() {
			this.ptr = IntPtr.Zero;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Calls CRYPTO_add_lock()
		/// </summary>
		/// <param name="type"></param>
		/// <param name="lockType"></param>
		/// <param name="file"></param>
		protected object DoAddRef(Type type, CryptoLockTypes lockType, string file) {
			IntPtr offset = Marshal.OffsetOf(type, "references");
			IntPtr offset_ptr = new IntPtr((long)this.ptr + (long)offset);
			Native.CRYPTO_add_lock(offset_ptr, 1, lockType, file, 0);
			return this;
		}
	}
	#endregion
}
