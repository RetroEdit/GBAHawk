﻿using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBAHawk_Debug
{
	// Cart with EEEPROM and solar sensor with RTC, used in Boktai games
	public class MapperEEPROM_Solar : MapperBase
	{
		public bool Ready_Flag;

		public byte Port_State;
		public byte Port_Dir;
		public byte Ports_RW;
		
		public byte Current_C4, Current_C6, Current_C8;

		public int Size_Mask = 0;
		public int Bit_Offset, Bit_Read;
		public int Access_Address;

		// 0 = ready for command
		// 2 = writing
		// 3 = reading
		// 5 = get address for write
		// 6 = get address for read
		public int Current_State;
		public int Next_State;

		public ulong Next_Ready_Cycle;

		public override void Reset()
		{
			Ready_Flag = true;

			Port_State = 0;
			Port_Dir = 0;
			Ports_RW = 0;

			Current_C4 = ROM_C4;
			Current_C6 = ROM_C6;
			Current_C8 = ROM_C8;

			Core.ROM[0xC4] = Current_C4;
			Core.ROM[0xC6] = Current_C6;
			Core.ROM[0xC8] = Current_C8;

			// set up initial variables
			Size_Mask = Core.cart_RAM.Length - 1;

			Bit_Offset = Bit_Read = 0;

			Access_Address = 0;

			Current_State = 0;

			Next_State = 0;

			Next_Ready_Cycle = 0;
		}

		// EEPROM is not mapped to SRAM region
		public override byte ReadMemory8(uint addr)
		{
			return 0xFF; // nothing mapped here
		}

		public override ushort ReadMemory16(uint addr)
		{
			return 0xFFFF; // nothing mapped here
		}

		public override uint ReadMemory32(uint addr)
		{
			return 0xFFFFFFFF; // nothing mapped here
		}

		public override byte PeekMemory(uint addr)
		{
			return ReadMemory8(addr);
		}

		public override void WriteMemory8(uint addr, byte value)
		{

		}

		public override void WriteMemory16(uint addr, ushort value)
		{
		
		}

		public override void WriteMemory32(uint addr, uint value)
		{

		}

		public override void PokeMemory(uint addr, byte value)
		{
			WriteMemory8(addr, value);
		}

		public override void WriteROM8(uint addr, byte value)
		{
			if ((addr & 1) == 0) { WriteROM16(addr, (ushort)value); }
		}

		public override void WriteROM16(uint addr, ushort value)
		{
			if (addr == 0x080000C4)
			{

			}
			else if (addr == 0x080000C6)
			{
				Port_Dir = (byte)(value & 0xF);

				if (Ports_RW == 1)
				{ 
					Core.ROM[0xC6] = Port_Dir;

					Port_State &= (byte)((~Port_Dir) & 0xF);

					Core.ROM[0xC4] = Port_State;
				}
			}
			else if (addr == 0x080000C8)
			{
				Ports_RW = (byte)(value & 1);

				if ((value & 1) == 1)
				{
					Core.ROM[0xC4] = Port_State;
					Core.ROM[0xC6] = Port_Dir;
					Core.ROM[0xC8] = Ports_RW;
				}
				else
				{
					Core.ROM[0xC4] = ROM_C4;
					Core.ROM[0xC6] = ROM_C6;
					Core.ROM[0xC8] = ROM_C8;
				}
			}

			Current_C4 = Core.ROM[0xC4];
			Current_C6 = Core.ROM[0xC6];
			Current_C8 = Core.ROM[0xC8];
		}

		public override void WriteROM32(uint addr, uint value)
		{
			WriteROM16(addr, (ushort)value);
			WriteROM16((addr + 2), (ushort)(value >> 8));
		}

		public override byte Mapper_EEPROM_Read()
		{
			int cur_read_bit = 0;
			int cur_read_byte = 0;

			byte ret = 0;
			
			if (Current_State == 0)
			{
				return (byte)(Ready_Flag ? 1 : 0);
			}
			else if (Current_State != 3)
			{
				// what to return in write state?
				return 0;
			}
			else
			{
				// what are the first 4 bits returned?
				if (Bit_Read < 4)
				{
					Bit_Read++;
					
					return 0;
				}
				else
				{
					cur_read_bit = Bit_Read - 4;
					cur_read_byte = cur_read_bit >> 3;

					cur_read_bit &= 7;

					cur_read_byte = 7 - cur_read_byte;
					cur_read_bit = 7 - cur_read_bit;

					ret = Core.cart_RAM[(Access_Address << 3) + cur_read_byte];

					ret >>= cur_read_bit;

					ret &= 1;

					Bit_Read++;

					//Console.WriteLine("Read get data: " + ret + " Bit_Read: " + Bit_Read);

					if (Bit_Read == 68)
					{
						Bit_Read = 0;
						Current_State = 0;
						Ready_Flag = true;
					}

					return ret;
				}
			}
		}

		public override void Mapper_EEPROM_Write(byte value)
		{
			int cur_write_bit = 0;
			int cur_write_byte = 0;

			if (Current_State == 0)
			{
				if (Core.CycleCount >= Next_Ready_Cycle)
				{
					Ready_Flag = true;
				}

				//Console.WriteLine("state 0: " + (value & 1) + " Bit_Offset: " + Bit_Offset);

				if (Ready_Flag)
				{
					Access_Address = 0;

					if (Bit_Offset == 0)
					{
						Next_State = value & 1;
						Bit_Offset = 1;
					}
					else
					{
						Next_State <<= 1;
						Next_State |= (value & 1);

						Bit_Offset = 0;

						if (Next_State != 0)
						{
							Current_State = 4 + Next_State;
						}
						else
						{
							Current_State = 0;
						}
					}
				}
			}
			else if (Current_State == 2)
			{
				if (Bit_Read < 64)
				{
					cur_write_bit = Bit_Read;
					cur_write_byte = cur_write_bit >> 3;

					cur_write_bit &= 7;

					cur_write_byte = 7 - cur_write_byte;
					cur_write_bit = 7 - cur_write_bit;

					Core.cart_RAM[(Access_Address << 3) + cur_write_byte] &= (byte)(~(1 << cur_write_bit));

					Core.cart_RAM[(Access_Address << 3) + cur_write_byte] |= (byte)((value & 1) << cur_write_bit);
				}
				
				Bit_Read++;

				if (Bit_Read == 65)
				{
					if ((value & 1) == 0)
					{
						Bit_Read = 0;
						Current_State = 0;
						Next_Ready_Cycle = Core.CycleCount + 0x1A750;
					}
					else
					{
						// error? GBA Tek says it should be zero
						Bit_Read = 0;
						Current_State = 0;
						Next_Ready_Cycle = Core.CycleCount + 0x1A750;
					}
				}
			}
			else if (Current_State == 3)
			{
				// Nothing occurs in read state?
				//Console.WriteLine("Bad write");
			}
			else if (Current_State == 6)
			{
				// Get Address
				if (Size_Mask == 0x1FF)
				{
					if (Bit_Offset < 6)
					{
						Access_Address |= (int)(value & 1);
						Access_Address <<= 1;

						Bit_Offset++;

						if (Bit_Offset == 6)
						{
							Access_Address >>= 1;
							Access_Address &= 0x3F;

							// now write the data to the EEPROM
							Bit_Offset = 0;
							Current_State = 2;
						}
					}
				}
				else
				{
					if (Bit_Offset < 14)
					{
						Access_Address |= (int)(value & 1);
						Access_Address <<= 1;

						Bit_Offset++;

						if (Bit_Offset == 14)
						{
							Access_Address >>= 1;
							Access_Address &= 0x3FF;

							// now write the data to the EEPROM
							Bit_Offset = 0;
							Current_State = 2;
						}
					}
				}
			}
			else if (Current_State == 7)
			{
				// Get Address for reading and wait for zero bit

				//Console.WriteLine("Read get addr: " + (value & 1) + " Bit_Offset: " + Bit_Offset);

				if (Size_Mask == 0x1FF)
				{
					if (Bit_Offset < 7)
					{
						if (Bit_Offset < 6)
						{
							Access_Address |= (int)(value & 1);
							Access_Address <<= 1;
						}

						Bit_Offset++;

						if (Bit_Offset == 7)
						{
							Access_Address >>= 1;
							Access_Address &= 0x3F;

							if ((value & 1) == 0)
							{
								Bit_Offset = 0;

								// now read the data out from the EEPROM
								Current_State = 3;
							}
							else
							{
								// error? seems to ignore this bit even though GBA tek says it should be zero
								Bit_Offset = 0;

								// now read the data out from the EEPROM
								Current_State = 3;
							}
						}
					}
				}
				else
				{
					if (Bit_Offset < 15)
					{
						if (Bit_Offset < 14)
						{
							Access_Address |= (int)(value & 1);
							Access_Address <<= 1;
						}

						Bit_Offset++;

						if (Bit_Offset == 15)
						{
							Access_Address >>= 1;
							Access_Address &= 0x3FF;

							if ((value & 1) == 0)
							{
								Bit_Offset = 0;

								// now read the data out from the EEPROM
								Current_State = 3;
							}
							else
							{
								// error? seems to ignore this bit even though GBA tek says it should be zero
								Bit_Offset = 0;

								// now read the data out from the EEPROM
								Current_State = 3;
							}
						}
					}
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(Ready_Flag), ref Ready_Flag);

			ser.Sync(nameof(Port_State), ref Port_State);
			ser.Sync(nameof(Port_Dir), ref Port_Dir);
			ser.Sync(nameof(Ports_RW), ref Ports_RW);

			ser.Sync(nameof(Current_C4), ref Current_C4);
			ser.Sync(nameof(Current_C6), ref Current_C6);
			ser.Sync(nameof(Current_C8), ref Current_C8);

			Core.ROM[0xC4] = Current_C4;
			Core.ROM[0xC6] = Current_C6;
			Core.ROM[0xC8] = Current_C8;

			ser.Sync(nameof(Bit_Offset), ref Bit_Offset);
			ser.Sync(nameof(Bit_Read), ref Bit_Read);

			ser.Sync(nameof(Current_State), ref Current_State);
			ser.Sync(nameof(Next_State), ref Next_State);

			ser.Sync(nameof(Access_Address), ref Access_Address);

			ser.Sync(nameof(Next_Ready_Cycle), ref Next_Ready_Cycle);
		}
	}
}