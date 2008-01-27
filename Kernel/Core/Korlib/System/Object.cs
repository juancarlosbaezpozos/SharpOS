//
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Mircea-Cristian Racasan <darx_kies@gmx.net>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

using SharpOS.AOT.Attributes;
using SharpOS.Korlib.Runtime;

namespace InternalSystem {
	[TargetNamespace ("System")]
	public unsafe class Object {
		internal VTable VTable;
		internal uint Synchronisation = 0;

		public Object ()
		{
		}
		
		public virtual string ToString()
		{
			return this.VTable.Type.Name;
		}
	}
}
