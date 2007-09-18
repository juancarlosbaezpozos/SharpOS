// 
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Sander van Rossen <sander.vanrossen@gmail.com>
//	William Lahti <xfurious@gmail.com>
//
// Licensed under the terms of the GNU GPL License version 2.
//

using System;
using SharpOS;
using SharpOS.ADC;
using SharpOS.Foundation;

namespace SharpOS 
{
	public unsafe class KeyMap
	{
		#region Global fields
		
		static PString8 *userKeyMap = PString8.Wrap (Kernel.StaticAlloc (24), 24);
		static byte *getBuiltinKeyMapBuffer = Kernel.StaticAlloc (Kernel.MaxKeyMapNameLength);
		static byte *stringConvBuffer = Kernel.StaticAlloc (Kernel.MaxKeyMapNameLength);
		static void *keymapArchive;
		static int keymapEntries;
		static void *keymapAddr;

		#endregion
		#region Setup
		
		/// <summary>
		/// Locates the archive of built-in keymaps, parses the
		/// user-specified keymap from the kernel command line,
		/// and installs a default keymap.
		/// </summary>
		public static void Setup ()
		{
			// look for the -keymap option, find a
			// matching keymap from the archive, and
			// use the Keyboard class to set it as
			// the installed keymap.



			if (!CommandLine.GetArgument ("-keymap", userKeyMap)) {
				// pick a default
				TextMode.WriteLine ("No keymap selected, choosing default (US)");

				userKeyMap->Clear ();
				userKeyMap->Concat ("US");
			}

			keymapArchive = (void*)Kernel.GetFunctionPointer
				("SharpOS.Kernel/Resources/BuiltinKeyMaps.ska");

			Kernel.Assert (keymapArchive != null, "KeyMap.Setup(): keymap archive is null");
			
			keymapEntries = *(int*)keymapArchive;
			keymapAddr = GetBuiltinKeyMap (userKeyMap);

			// print some info
			
			TextMode.WriteLine ("KeyMap archive: installed at 0x", (int)keymapArchive, true);
			TextMode.WriteLine ("                ", keymapEntries, " entries");
			TextMode.WriteLine ("");

			if (keymapAddr == null) {
				Kernel.Warning ("Failed to install an initial keymap");
				return;
			}
			
			SetDirectKeyMap (keymapAddr);
		}

		#endregion
		#region Internal

		static void *GetBuiltinKeyMap (byte *name, int nameLen)
		{
			TextMode.Write ("Key Map Name: ");
			TextMode.Write (name);
			TextMode.WriteLine ();

			TextMode.Write ("Key Map Name Length: ");
			TextMode.Write (nameLen);
			TextMode.WriteLine ();

			byte *table = (byte*)keymapArchive + 4;
			byte *buf = getBuiltinKeyMapBuffer;

			Kernel.Assert (nameLen <= Kernel.MaxKeyMapNameLength,
				"KeyMap.GetBuiltinKeyMap(): key map name is too large");
			
			for (int x = 0; x < keymapEntries; ++x) {
				int nSize = 0;
				int tSize = 0;
				int error = 0;
				int strSize = 0;

				strSize = BinaryTool.ReadPrefixedString (table, buf,
					Kernel.MaxKeyMapNameLength, &error);

				table += strSize;
				nSize = ByteString.Length (buf);
				
				TextMode.Write ("nsize: ");
				TextMode.Write (nSize);
				TextMode.WriteLine ();

				TextMode.Write ("found keymap: ");
				TextMode.WriteLine (buf);
				
				if (nSize == nameLen && ByteString.Compare (name, buf, nameLen) == 0)
					return table;

				table += 2; // keymask/statebit

				// default table
				
				tSize = *(int*)table;
				table += 4;
				table += tSize;

				// shifted table
				
				tSize = *(int*)table;
				table += 4;
				table += tSize;
			}

			return null;
		}

		#endregion
		#region GetKeyMap() family
		
		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void *GetBuiltinKeyMap (byte *name)
		{
			return GetBuiltinKeyMap (name, ByteString.Length (name));
		}
		
		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void *GetBuiltinKeyMap (CString8 *name)
		{
			return GetBuiltinKeyMap (name->Pointer, name->Length);
		}

		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void *GetBuiltinKeyMap (PString8 *name)
		{
			return GetBuiltinKeyMap (name->Pointer, name->Length);
		}
		
		/// <summary>
		/// Gets the address of a builtin keymap included in the kernel
		/// via the keymap archive resource in SharpOS.Kernel.dll. The
		/// archive is generated by the SharpOS keymap compiler.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="nameLen">The name len.</param>
		/// <returns></returns>
		public static void *GetBuiltinKeyMap (string name)
		{
			ByteString.GetBytes (name, stringConvBuffer, Kernel.MaxKeyMapNameLength);
			
			return GetBuiltinKeyMap (stringConvBuffer, name.Length);
		}

		/// <summary>
		/// Gets the count of all builtin key maps.
		/// </summary>
		public static int GetBuiltinKeyMapsCount ()
		{
			return keymapEntries;
		}
		
		/// <summary>
		/// Gets the address of a builtin key map, by it's numeric ID. Good
		/// for iterating through the list of builtin key maps.
		/// </summary>
		public static void *GetBuiltinKeyMap (int id)
		{
			byte *table = (byte*)keymapArchive + 4;
			
			for (int x = 0; x < keymapEntries; ++x) {
				
				if (x == id)
					return (void*) table;

				// name-size (4), name string (x), keymask and statebit (2)
							
				table += 6 + *(int*)table;

				// table size (4), default table (x)
				
				table += 4 + *(int*)table;

				// table size (4), shifted table (x)
				
				table += 4 + *(int*)table;
			}

			return null;
		}
		
		/// <summary>
		/// Gets the keymap currently in use.
		/// </summary>
		public static void *GetCurrentKeyMap ()
		{
			return keymapAddr;
		}

		#endregion
		#region SetKeyMap() family
		
		/// <summary>
		/// Installs the default and shifted key tables of the given
		/// keymap, so that all further keyboard scancodes are
		/// converted using the new mapping.
		/// </summary>
		public static void SetDirectKeyMap (void *keymap)
		{
			byte *defmap = null, shiftmap = null;
			int defmapLen = 0, shiftmapLen = 0;

			defmap = GetDefaultTable (keymapAddr, &defmapLen);
			shiftmap = GetShiftedTable (keymapAddr, &shiftmapLen);

			Keyboard.SetKeyMap (defmap, defmapLen, shiftmap, shiftmapLen);
		}

		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (byte *name)
		{
			SetDirectKeyMap (GetBuiltinKeyMap (name, ByteString.Length (name)));
		}
		
		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (CString8 *name)
		{
			SetDirectKeyMap (GetBuiltinKeyMap (name->Pointer, name->Length));
		}
		
		/// <summary>
		/// Sets the current keymap to a built-in one specified by
		/// <paramref name="name" />.
		/// </summary>
		public static void SetKeyMap (PString8 *name)
		{
			SetDirectKeyMap (GetBuiltinKeyMap (name->Pointer, name->Length));
		}

		#endregion
		#region [Get/Set][Default/Shifted]Table() family
		
		/// <summary>
		/// Gets the `default' table of the given keymap.
		/// </summary>
		public static byte *GetDefaultTable (void *keymap, int *ret_len)
		{
			int nlen = *(int*)keymap;
			*ret_len = *(int*)((byte*)keymap + 6 + nlen);
			
			return (byte*)keymap + 10;
		}

		/// <summary>
		/// Gets the `shifted' table of the given keymap.
		/// </summary>
		public static byte *GetShiftedTable (void *keymap, int *ret_len)
		{
			int dLen = 0;
			byte *ptr = GetDefaultTable (keymap, &dLen);

			ptr += dLen;
			*ret_len = *(int*)ptr;

			return ptr + 4;
		}

		/// <summary>
		/// Gets the `default' table of the installed keymap.
		/// </summary>
		public static byte *GetDefaultTable (int *ret_len)
		{
			Kernel.Assert (keymapAddr != null, "No keymap is installed!");
			
			return GetDefaultTable (keymapAddr, ret_len);
		}

		/// <summary>
		/// Gets the `shifted' table of the installed keymap.
		/// </summary>
		public static byte *GetShiftedTable (int *ret_len)
		{
			Kernel.Assert (keymapAddr != null, "No keymap is installed!");
			
			return GetShiftedTable (keymapAddr, ret_len);
		}

		#endregion
		#region GetKeyMask/StateBit() family

		/// <summary>
		/// Gets the keymask specified in the given keymap.
		/// </summary>
		public static byte GetKeyMask (void *keymap)
		{
			int nlen = *(int*)keymap;
			
			return *((byte*)keymap + 4 + nlen);
		}
		
		/// <summary>
		/// Gets the state bit specified in the given keymap.
		/// </summary>
		public static byte GetStateBit (void *keymap)
		{
			int nlen = *(int*)keymap;
			
			return *((byte*)keymap + 5 + nlen);
		}
		
		/// <summary>
		/// Gets the keymask of the installed keymap.
		/// </summary>
		public static byte GetKeyMask ()
		{
			Kernel.Assert (keymapAddr != null, "No keymap is installed!");
			
			return GetKeyMask (keymapAddr);
		}
		
		/// <summary>
		/// Gets the state bit of the installed keymap.
		/// </summary>
		public static byte GetStateBit ()
		{
			Kernel.Assert (keymapAddr != null, "No keymap is installed!");
			
			return GetStateBit (keymapAddr);
		}

		#endregion
	}
}

