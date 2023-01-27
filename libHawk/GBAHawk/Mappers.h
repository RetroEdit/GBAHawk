#ifndef MAPPERS_H
#define MAPPERS_H

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBAHawk
{
	class Mappers
	{
	public:
	#pragma region mapper base

		bool Ready_Flag;
		
		uint32_t Size_Mask;

		uint32_t Bit_Offset, Bit_Read;

		uint32_t Access_Address;

		// 0 = ready for command
		// 2 = writing
		// 3 = reading
		// 5 = get address for write
		// 6 = get address for read
		uint32_t Current_State;

		uint32_t Next_State;
		
		uint64_t Next_Ready_Cycle;

		uint64_t* Core_Cycle_Count = nullptr;
		
		Mappers()
		{
			Reset();
		}

		uint8_t* Cart_RAM = nullptr;

		virtual uint8_t Read_Memory_8(uint32_t addr)
		{
			return 0;
		}

		virtual uint16_t Read_Memory_16(uint32_t addr)
		{
			return 0;
		}

		virtual uint32_t Read_Memory_32(uint32_t addr)
		{
			return 0;
		}

		virtual uint8_t Peek_Memory(uint32_t addr)
		{
			return 0;
		}

		virtual void Write_Memory_8(uint32_t addr, uint8_t value)
		{
		}

		virtual void Write_Memory_16(uint32_t addr, uint16_t value)
		{
		}

		virtual void Write_Memory_32(uint32_t addr, uint32_t value)
		{
		}

		virtual void Poke_Memory(uint32_t addr, uint8_t value)
		{
		}

		virtual void Dispose()
		{
		}

		virtual void Reset()
		{
		}

		virtual void Mapper_Tick()
		{
		}

		virtual uint8_t Mapper_EEPROM_Read()
		{
			return 0xFF;
		}

		virtual void Mapper_EEPROM_Write(uint8_t value)
		{

		}

		virtual void RTC_Get(int value, int index)
		{
		}

	#pragma endregion

	#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			saver = bool_saver(Ready_Flag, saver);

			saver = int_saver(Size_Mask, saver);

			saver = int_saver(Bit_Offset, saver);

			saver = int_saver(Bit_Read, saver);

			saver = int_saver(Access_Address, saver);

			saver = int_saver(Current_State, saver);

			saver = int_saver(Next_State, saver);

			saver = long_saver(Next_Ready_Cycle, saver);

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			loader = bool_loader(&Ready_Flag, loader);

			loader = int_loader(&Size_Mask, loader);

			loader = int_loader(&Bit_Offset, loader);

			loader = int_loader(&Bit_Read, loader);

			loader = int_loader(&Access_Address, loader);

			loader = int_loader(&Current_State, loader);

			loader = int_loader(&Next_State, loader);

			loader = long_loader(&Next_Ready_Cycle, loader);

			return loader;
		}

		uint8_t* bool_saver(bool to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save ? 1 : 0); saver++;

			return saver;
		}

		uint8_t* byte_saver(uint8_t to_save, uint8_t* saver)
		{
			*saver = to_save; saver++;

			return saver;
		}

		uint8_t* int_saver(uint32_t to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save & 0xFF); saver++; *saver = (uint8_t)((to_save >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 16) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* long_saver(uint64_t to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save & 0xFF); saver++; *saver = (uint8_t)((to_save >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 16) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 24) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 32) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 40) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 48) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 56) & 0xFF); saver++;

			return saver;
		}

		uint8_t* bool_loader(bool* to_load, uint8_t* loader)
		{
			to_load[0] = *to_load == 1; loader++;

			return loader;
		}

		uint8_t* byte_loader(uint8_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++;

			return loader;
		}

		uint8_t* int_loader(uint32_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (*loader << 8); loader++;
			to_load[0] |= (*loader << 16); loader++; to_load[0] |= (*loader << 24); loader++;

			return loader;
		}

		uint8_t* long_loader(uint64_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (uint64_t)(*loader) << 8; loader++;
			to_load[0] |= (uint64_t)(*loader) << 16; loader++; to_load[0] |= (uint64_t)(*loader) << 24; loader++;
			to_load[0] |= (uint64_t)(*loader) << 32; loader++; to_load[0] |= (uint64_t)(*loader) << 40; loader++;
			to_load[0] |= (uint64_t)(*loader) << 48; loader++; to_load[0] |= (uint64_t)(*loader) << 56; loader++;

			return loader;
		}

	#pragma endregion

	};

	#pragma region Default

	class Mapper_Default : public Mappers
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t Read_Memory_8(uint32_t addr)
		{
			return 0xFF; // nothing mapped here
		}

		uint16_t Read_Memory_16(uint32_t addr)
		{
			return 0xFFFF; // nothing mapped here
		}

		uint32_t Read_Memory_32(uint32_t addr)
		{
			return 0xFFFFFFFF; // nothing mapped here
		}

		uint8_t PeekMemory(uint32_t addr)
		{
			return Read_Memory_8(addr);
		}
	};

	#pragma endregion

	#pragma region SRAM

	class Mapper_SRAM : public Mappers
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t Read_Memory_8(uint32_t addr)
		{
			return Cart_RAM[addr & 0x7FFF];
		}

		uint16_t Read_Memory_16(uint32_t addr)
		{
			// 8 bit bus only
			uint16_t ret = Cart_RAM[addr & 0x7FFF];
			ret = (uint16_t)(ret | (ret << 8));
			return ret;
		}

		uint32_t Read_Memory_32(uint32_t addr)
		{
			// 8 bit bus only
			uint32_t ret = Cart_RAM[addr & 0x7FFF];
			ret = (uint32_t)(ret | (ret << 8) | (ret << 16) | (ret << 24));
			return ret;
		}

		uint8_t Peek_Memory(uint32_t addr)
		{
			return Read_Memory_8(addr);
		}

		void Write_Memory_8(uint32_t addr, uint8_t value)
		{
			Cart_RAM[addr & 0x7FFF] = value;
		}

		void Write_Memory_16(uint32_t addr, uint16_t value)
		{
			// stores the correct byte in the correct position, but only 1
			if ((addr & 1) == 0)
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)(value & 0xFF);
			}
			else
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)((value >> 8) & 0xFF);
			}
		}

		void Write_Memory_32(uint32_t addr, uint32_t value)
		{
			// stores the correct byte in the correct position, but only 1
			if ((addr & 3) == 0)
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)(value & 0xFF);
			}
			else if ((addr & 3) == 1)
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)((value >> 8) & 0xFF);
			}
			else if ((addr & 3) == 2)
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)((value >> 16) & 0xFF);
			}
			else
			{
				Cart_RAM[addr & 0x7FFF] = (uint8_t)((value >> 24) & 0xFF);
			}
		}

	};
	#pragma endregion

	#pragma region EEPROM

	class Mapper_EEPROM : public Mappers
	{
	public:

		void Reset()
		{
			// set up initial variables
			Ready_Flag = true;

			Bit_Offset = Bit_Read = 0;

			Access_Address = 0;

			Current_State = 0;

			Next_State = 0;

			Next_Ready_Cycle = 0;
		}

		uint8_t Read_Memory_8(uint32_t addr)
		{
			return 0xFF; // nothing mapped here
		}

		uint16_t Read_Memory_16(uint32_t addr)
		{
			return 0xFFFF; // nothing mapped here
		}

		uint32_t Read_Memory_32(uint32_t addr)
		{
			return 0xFFFFFFFF; // nothing mapped here
		}

		uint8_t PeekMemory(uint32_t addr)
		{
			return Read_Memory_8(addr);
		}

		uint8_t Mapper_EEPROM_Read()
		{
			uint32_t cur_read_bit = 0;
			uint32_t cur_read_byte = 0;

			uint8_t ret = 0;

			if (Current_State == 0)
			{
				return (uint8_t)(Ready_Flag ? 1 : 0);
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

					ret = Cart_RAM[(Access_Address << 3) + cur_read_byte];

					ret >>= cur_read_bit;

					ret &= 1;

					Bit_Read++;

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

		void Mapper_EEPROM_Write(uint8_t value)
		{
			uint32_t cur_write_bit = 0;
			uint32_t cur_write_byte = 0;

			if (Current_State == 0)
			{
				if (Core_Cycle_Count[0] >= Next_Ready_Cycle)
				{
					Ready_Flag = true;
				}

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

					Cart_RAM[(Access_Address << 3) + cur_write_byte] &= (uint8_t)(~(1 << cur_write_bit));

					Cart_RAM[(Access_Address << 3) + cur_write_byte] |= (uint8_t)((value & 1) << cur_write_bit);
				}

				Bit_Read++;

				if (Bit_Read == 65)
				{
					if ((value & 1) == 0)
					{
						Bit_Read = 0;
						Current_State = 0;
						Next_Ready_Cycle = Core_Cycle_Count[0] + 0x1A750;
					}
					else
					{
						// error? GBA Tek says it should be zero
						Bit_Read = 0;
						Current_State = 0;
						Next_Ready_Cycle = Core_Cycle_Count[0] + 0x1A750;
					}
				}
			}
			else if (Current_State == 3)
			{
				// Nothing occurs in read state?
			}
			else if (Current_State == 6)
			{
				// Get Address
				if (Size_Mask == 0x1FF)
				{
					if (Bit_Offset < 6)
					{
						Access_Address |= (uint32_t)(value & 1);
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
						Access_Address |= (uint32_t)(value & 1);
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

				if (Size_Mask == 0x1FF)
				{
					if (Bit_Offset < 7)
					{
						if (Bit_Offset < 6)
						{
							Access_Address |= (uint32_t)(value & 1);
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
							Access_Address |= (uint32_t)(value & 1);
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
	};

	#pragma endregion
}

#endif
