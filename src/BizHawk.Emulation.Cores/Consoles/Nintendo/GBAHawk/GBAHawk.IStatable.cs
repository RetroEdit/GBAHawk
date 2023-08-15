﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBAHawk
	{
		private void SyncState(Serializer ser)
		{
			if (cart_RAM != null)
			{
				ser.Sync(nameof(cart_RAM), ref cart_RAM, false);
			}

			_controllerDeck.SyncState(ser);

			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref Lag_Count);
			ser.Sync("IsLag", ref Is_Lag);

			if (ser.IsReader)
			{
				ser.Sync(nameof(GBA_core), ref GBA_core, false);
				LibGBAHawk.GBA_load_state(GBA_Pntr, GBA_core);
			}
			else
			{
				LibGBAHawk.GBA_save_state(GBA_Pntr, GBA_core);
				ser.Sync(nameof(GBA_core), ref GBA_core, false);
			}
		}
	}
}
