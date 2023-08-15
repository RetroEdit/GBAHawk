﻿using System;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;

using BizHawk.Common.ReflectionExtensions;
using static BizHawk.Emulation.Cores.Nintendo.GBA.GBAHawk;
using static BizHawk.Emulation.Cores.Nintendo.GBHawk.GBHawk;

/*
	GBA Emulator
	NOTES: 
	RAM disabling not implemented, check if used by any games

	Open bus behaviour needs to be done more carefully

	EEPROM accesses only emulated at 0xDxxxxxx, check if any games use lower range
*/

namespace BizHawk.Emulation.Cores.Nintendo.GBALink
{
	[Core(CoreNames.GBAHawkLink, "", isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GBAHawkLink : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IInputPollable, IRegionable,
								ISettable<GBAHawkLink.GBALinkSettings, GBAHawkLink.GBALinkSyncSettings>
	{
		public byte[] BIOS;

		public readonly byte[][] ROMS = new byte[2][];

		public uint[] ROMS_Length = new uint[2];

		public static readonly byte[] multi_boot_check = {0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x6E, 0x27, 0x74, 0x20, 0x61, 0x20, 0x52, 0x4F, 0x4D };

		public ushort controller_state_1, controller_state_2;
		public ushort Acc_X_state_1, Acc_X_state_2;
		public ushort Acc_Y_state_1, Acc_Y_state_2;
		public byte Solar_state_1, Solar_state_2;

		public byte[][] cart_RAMS = new byte[2][];
		public bool[] has_bats = new bool[2];
		public bool use_sram;

		int[] mappers = new int[2];

		[CoreConstructor(VSystemID.Raw.GBAL)]
		public GBAHawkLink(CoreLoadParameters<GBAHawkLink.GBALinkSettings, GBAHawkLink.GBALinkSyncSettings> lp)
		{
			if (lp.Roms.Count != 2)
				throw new InvalidOperationException("Wrong number of roms");
			
			// multi boot identified by special ROM
			bool[] is_multi_boot = new bool[2];

			is_multi_boot[0] = is_multi_boot[1] = true;

			ServiceProvider = new BasicServiceProvider(this);
			Settings = (GBALinkSettings)lp.Settings ?? new GBALinkSettings();
			SyncSettings = (GBALinkSyncSettings)lp.SyncSettings ?? new GBALinkSyncSettings();

			use_sram = SyncSettings.Use_SRAM;

			ROMS[0] = new byte[0x6000000];
			ROMS[1] = new byte[0x6000000];

			for (int i = 0; i < 2; i++)
			{
				var rom = lp.Roms[i].RomData;

				var romHashMD5 = MD5Checksum.ComputePrefixedHex(rom);
				Console.WriteLine(romHashMD5);
				var romHashSHA1 = SHA1Checksum.ComputePrefixedHex(rom);
				Console.WriteLine(romHashSHA1);

				for (int j = 0; j < 16; j++)
				{
					if (lp.Roms[i].RomData[j] != multi_boot_check[j]) { is_multi_boot[i] = false; }
				}

				if (is_multi_boot[i])
				{
					// replace with empty ROM
					for (int j = 0; j < 0x6000000; j += 2)
					{
						ROMS[i][j] = (byte)((j & 0xFF) >> 1);
						ROMS[i][j + 1] = (byte)(((j >> 8) & 0xFF) >> 1);
					}

					Console.WriteLine("No ROM inserted to console " + i);
				}
				else if (rom.Length > 0x6000000)
				{
					throw new Exception("Over size ROM?");
				}
				else
				{
					ROMS_Length[i] = (uint)rom.Length;
					Buffer.BlockCopy(rom, 0, ROMS[i], 0, rom.Length);

					if (romHashSHA1 != "SHA1:5F989B9A4017F16A431F76FD78A95E9799AA8FCA") // GBA Suite Memory test
					{
						// fill unused ROM area (assuming the ROM chip doesn't respond)
						// for now mirror across 2MB boundaries, but might need to be more precise for smaller ROMs (do they exist?)
						if (rom.Length < 0x6000000)
						{
							int ofst_base = rom.Length & 0xF000000;

							if (rom.Length > ofst_base)
							{
								ofst_base += 0x1000000;
							}

							if (ofst_base < 0x6000000)
							{
								for (int j = 0; j < (0x6000000 - ofst_base); j += 2)
								{
									ROMS[i][j + ofst_base] = (byte)((j & 0xFF) >> 1);
									ROMS[i][j + ofst_base + 1] = (byte)(((j >> 8) & 0xFF) >> 1);
								}
							}
						}
					}
					else
					{
						// mirror the rom accross the whole region (might need different increment sizes for different ROMs)
						for (int j = 0; j < rom.Length; j++)
						{
							ROMS[i][j + 0x2000000] = rom[j];
							ROMS[i][j + 0x4000000] = rom[j];
						}
					}
				}

				mappers[i] = Setup_Mapper(romHashMD5, romHashSHA1, i);

				if (cart_RAMS[i] != null)
				{
					// initialize SRAM to 0xFF;
					if ((mappers[i] == 2) || (mappers[i] == 3))
					{
						for (int j = 0; j < cart_RAMS[i].Length; j++)
						{
							cart_RAMS[i][j] = 0xFF;
						}
					}
					// initialize EEPROM to 0xFF;
					if ((mappers[i] == 4) || (mappers[i] == 5) || (mappers[i] == 6))
					{
						for (int j = 0; j < cart_RAMS[i].Length; j++)
						{
							cart_RAMS[i][j] = 0xFF;
						}
					}
					// initialize Flash to 0;
					if ((mappers[i] == 7) || (mappers[i] == 8))
					{
						for (int j = 0; j < cart_RAMS[i].Length; j++)
						{
							cart_RAMS[i][j] = 0;
						}
					}
				}
			}
			
			// Load up a BIOS and initialize the correct PPU
			BIOS = lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("GBA", "Bios"), "BIOS Not Found, Cannot Load");

			GBA_Pntr = LibGBAHawkLink.GBALink_create();

			LibGBAHawkLink.GBALink_load_bios(GBA_Pntr, BIOS);

			// load 0 RTC
			bool rtc_working_0 = true;

			byte temp_year_0 = 0;
			byte temp_month_0 = 1;
			byte temp_day_0 = 1;
			byte temp_week_0 = 0;
			byte temp_hour_0 = 0;
			byte temp_minute_0 = 0;
			byte temp_second_0 = 0;
			byte temp_ctrl_0 = 0;

			if (SyncSettings.RTCInitialState_L == GBALinkSyncSettings.InitRTCState.Reset_Bad_Batt)
			{
				rtc_working_0 = false;
			}
			else if (SyncSettings.RTCInitialState_L == GBALinkSyncSettings.InitRTCState.RTC_Set)
			{
				// all games seem to use 24 hour mode,, so use this to represent set time
				temp_ctrl_0 = 0x40;

				// parse the date and time into the regs
				DateTime temp_0 = SyncSettings.RTCInitialTime_L;

				// if year outside range of RTC, just leave the initial values
				if ((temp_0.Year < 2100) && (temp_0.Year >= 2000))
				{
					temp_year_0 = To_BCD((byte)(temp_0.Year - 2000));
					temp_month_0 = To_BCD((byte)temp_0.Month);
					temp_day_0 = To_BCD((byte)temp_0.Day);
					temp_week_0 = To_BCD((byte)temp_0.DayOfWeek);
					temp_minute_0 = To_BCD((byte)temp_0.Minute);
					temp_second_0 = To_BCD((byte)temp_0.Second);

					temp_hour_0 = To_BCD((byte)temp_0.Hour);

					if (temp_0.Hour >= 12)
					{
						temp_hour_0 |= 0x80;
					}
				}
			}

			ulong date_time_0 = 0;

			date_time_0 |= temp_second_0;
			date_time_0 |= ((ulong)temp_minute_0 << 8);
			date_time_0 |= ((ulong)temp_hour_0 << 16);
			date_time_0 |= ((ulong)temp_week_0 << 24);
			date_time_0 |= ((ulong)temp_day_0 << 32);
			date_time_0 |= ((ulong)temp_month_0 << 40);
			date_time_0 |= ((ulong)temp_year_0 << 48);
			date_time_0 |= ((ulong)temp_ctrl_0  << 56);

			// load 1 RTC
			bool rtc_working_1 = true;

			byte temp_year_1 = 0;
			byte temp_month_1 = 1;
			byte temp_day_1 = 1;
			byte temp_week_1 = 0;
			byte temp_hour_1 = 0;
			byte temp_minute_1 = 0;
			byte temp_second_1 = 0;
			byte temp_ctrl_1 = 0;

			if (SyncSettings.RTCInitialState_R == GBALinkSyncSettings.InitRTCState.Reset_Bad_Batt)
			{
				rtc_working_1 = false;
			}
			else if (SyncSettings.RTCInitialState_R == GBALinkSyncSettings.InitRTCState.RTC_Set)
			{
				// all games seem to use 24 hour mode,, so use this to represent set time
				temp_ctrl_1 = 0x40;

				// parse the date and time into the regs
				DateTime temp_1 = SyncSettings.RTCInitialTime_R;

				// if year outside range of RTC, just leave the initial values
				if ((temp_1.Year < 2100) && (temp_1.Year >= 2000))
				{
					temp_year_1 = To_BCD((byte)(temp_1.Year - 2000));
					temp_month_1 = To_BCD((byte)temp_1.Month);
					temp_day_1 = To_BCD((byte)temp_1.Day);
					temp_week_1 = To_BCD((byte)temp_1.DayOfWeek);
					temp_minute_1 = To_BCD((byte)temp_1.Minute);
					temp_second_1 = To_BCD((byte)temp_1.Second);

					temp_hour_1 = To_BCD((byte)temp_1.Hour);

					if (temp_1.Hour >= 12)
					{
						temp_hour_1 |= 0x80;
					}
				}
			}

			ulong date_time_1 = 0;

			date_time_1 |= temp_second_1;
			date_time_1 |= ((ulong)temp_minute_1 << 8);
			date_time_1 |= ((ulong)temp_hour_1 << 16);
			date_time_1 |= ((ulong)temp_week_1 << 24);
			date_time_1 |= ((ulong)temp_day_1 << 32);
			date_time_1 |= ((ulong)temp_month_1 << 40);
			date_time_1 |= ((ulong)temp_year_1 << 48);
			date_time_1 |= ((ulong)temp_ctrl_1 << 56);

			Console.WriteLine("Mapper: " + mappers[0] + " " + +mappers[1]);

			LibGBAHawkLink.GBALink_load(GBA_Pntr, ROMS[0], (uint)ROMS_Length[0], mappers[0],
												ROMS[1], (uint)ROMS_Length[1], mappers[1],
												date_time_0, rtc_working_0, date_time_1, rtc_working_1);

			if (cart_RAMS[0] != null) { LibGBAHawkLink.GBALink_create_SRAM(GBA_Pntr, cart_RAMS[0], (uint)cart_RAMS[0].Length, 0); }
			if (cart_RAMS[1] != null) { LibGBAHawkLink.GBALink_create_SRAM(GBA_Pntr, cart_RAMS[1], (uint)cart_RAMS[1].Length, 1); }

			blip_L.SetRates(4194304 * 4, 44100);
			blip_R.SetRates(4194304 * 4, 44100);

			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(this);

			SetupMemoryDomains();

			Header_Length = LibGBAHawkLink.GBALink_getheaderlength(GBA_Pntr);
			Disasm_Length = LibGBAHawkLink.GBALink_getdisasmlength(GBA_Pntr);
			Reg_String_Length = LibGBAHawkLink.GBALink_getregstringlength(GBA_Pntr);

			var newHeader = new StringBuilder(Header_Length);
			LibGBAHawkLink.GBALink_getheader(GBA_Pntr, newHeader, Header_Length);

			Console.WriteLine(Header_Length + " " + Disasm_Length + " " + Reg_String_Length);

			Tracer = new TraceBuffer(newHeader.ToString());

			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<ITraceable>(Tracer);
			serviceProvider.Register<IStatable>(new StateSerializer(SyncState));

			string cntrllr1 = GBAHawkLink_ControllerDeck.DefaultControllerName;
			string cntrllr2 = GBAHawkLink_ControllerDeck.DefaultControllerName;

			if (mappers[0] == 3)
			{
				cntrllr1 = typeof(StandardZGyro).DisplayName();
			}
			else if (mappers[0] == 5)
			{
				cntrllr1 = typeof(StandardTilt).DisplayName();
			}
			else if (mappers[0] == 6)
			{
				cntrllr1 = typeof(StandardSolar).DisplayName();
			}

			if (mappers[1] == 3)
			{
				cntrllr1 = typeof(StandardZGyro).DisplayName();
			}
			else if (mappers[1] == 5)
			{
				cntrllr2 = typeof(StandardTilt).DisplayName();
			}
			else if (mappers[1] == 6)
			{
				cntrllr2 = typeof(StandardSolar).DisplayName();
			}

			_controllerDeck = new(cntrllr1, cntrllr2, false);

			LibGBAHawkLink.GBALink_Hard_Reset(GBA_Pntr);
		}

		public byte To_BCD(byte in_byte)
		{
			byte tens_cnt = 0;

			while (in_byte >= 10)
			{
				tens_cnt += 1;
				in_byte -= 10;
			}

			return (byte)((tens_cnt << 4) | in_byte);
		}

		public int Setup_Mapper(string romHashMD5, string romHashSHA1, int i)
		{
			int size_f = 0;
			
			int mppr = 0;
			has_bats[i] = false;

			// check for SRAM
			for (int j = 0; j < ROMS[i].Length; j += 4)
			{
				if (ROMS[i][j] == 0x53)
				{
					if ((ROMS[i][j + 1] == 0x52) && (ROMS[i][j + 2] == 0x41))
					{
						if ((ROMS[i][j + 3] == 0x4D) && (ROMS[i][j + 4] == 0x5F))
						{
							Console.WriteLine("using SRAM mapper");
							mppr = 2;
							break;
						}
					}
				}
				if (ROMS[i][j] == 0x45)
				{
					if ((ROMS[i][j + 1] == 0x45) && (ROMS[i][j + 2] == 0x50))
					{
						if ((ROMS[i][j + 3] == 0x52) && (ROMS[i][j + 4] == 0x4F) && (ROMS[i][j + 5] == 0x4D))
						{
							Console.WriteLine("using EEPROM mapper");
							mppr = 4;
							break;
						}
					}
				}
				if (ROMS[i][j] == 0x46)
				{
					if ((ROMS[i][j + 1] == 0x4C) && (ROMS[i][j + 2] == 0x41))
					{
						if ((ROMS[i][j + 3] == 0x53) && (ROMS[i][j + 4] == 0x48))
						{
							if ((ROMS[i][j + 5] == 0x5F) && (ROMS[i][j + 6] == 0x56))
							{
								Console.WriteLine("using FLASH mapper");
								mppr = 7;
								size_f = 64;

								break;
							}
							if ((ROMS[i][j + 5] == 0x35) && (ROMS[i][j + 6] == 0x31) && (ROMS[i][j + 7] == 0x32))
							{
								Console.WriteLine("using FLASH mapper");
								mppr = 7;
								size_f = 64;

								break;
							}
							if ((ROMS[i][j + 5] == 0x31) && (ROMS[i][j + 6] == 0x4D))
							{
								Console.WriteLine("using FLASH mapper");
								mppr = 7;
								size_f = 128;

								break;
							}
						}
					}
				}
			}

			// hash checks for individual games / homebrew / test roms
			if ((romHashSHA1 == "SHA1:C67E0A5E26EA5EBA2BC11C99D003027A96E44060") || // Aging cart test
				(romHashSHA1 == "SHA1:AC6D8FD4A1FB5234A889EE092CBE7774DAC21F0E") || // VRAM access test
				(romHashSHA1 == "SHA1:41D39A0C34F72469DD3FBCC90190605B8ADA93E6") || // Another World
				(romHashSHA1 == "SHA1:270C426705DF767A4AD2DC69D039842442F779B2") || // Anguna
				(romHashSHA1 == "SHA1:9B02C4BFD99CCD913A5D7EE7CF269EBC689E1FDE"))   // Higurashi no Nakukoroni (fixed header)

			{
				Console.WriteLine("using SRAM mapper");
				mppr = 2;
			}

			if (romHashSHA1 == "SHA1:3714D1222E5C2B2734996ACE9F9BC49B35656171")
			{
				mppr = 1;
			}
			else if (mppr == 2)
			{
				has_bats[i] = true;
				cart_RAMS[i] = new byte[0x8000];

				if ((romHashSHA1 == "SHA1:A389FA50E2E842B264B980CBE30E980C69D93A5B") || // Mawaru - Made in Wario (JPN)
					(romHashSHA1 == "SHA1:F0102D0D6F7596FE853D5D0A94682718278E083A"))   // Warioware Twisted (USA)
				{
					mppr = 3;
				}
			}
			else if (mppr == 4)
			{
				has_bats[i] = true;

				// assume 8 KB saves, use hash check to pick out 512 bytes versions
				if ((romHashSHA1 == "SHA1:947498CB1DB918D305500257E8223DEEADDF561D") || // Yoshi USA
					(romHashSHA1 == "SHA1:A3F2035CA2BDC2BC59E9E46EFBB6187705EBE3D1") || // Yoshi Japan
					(romHashSHA1 == "SHA1:045BE1369964F141009F3701839EC0A8DCCB25C1") || // Yoshi EU
					(romHashSHA1 == "SHA1:40CB751D119A49BE0CD44CF0491C93EBC8795EF0"))   // koro koro puzzle
				{
					Console.WriteLine("Using Tilt Controls");

					cart_RAMS[i] = new byte[0x200];
					mppr = 5;
				}
				else if ((romHashSHA1 == "SHA1:F91126CD3A1BF7BF5F770D3A70229171D0D5A6EE") || // Boktai Beta
						 (romHashSHA1 == "SHA1:64F7BF0F0560F6E94DA33B549D3206678B29F557") || // Boktai EU
						 (romHashSHA1 == "SHA1:7164326283DF46A3941EC7B6CECA889CBC40E660") || // Boktai USA
						 (romHashSHA1 == "SHA1:CD10D8ED82F4DAF4072774F70D015E39A5D32D0B") || // Boktai 2 USA
						 (romHashSHA1 == "SHA1:EEACDF5A9D3D2173A4A96689B72DC6B7AD92153C") || // Boktai 2 EU
						 (romHashSHA1 == "SHA1:54A4DCDECA2EE9A22559EB104B88586386639097") || // Boktai 2 JPN
						 (romHashSHA1 == "SHA1:1A81843C3070DECEA4CBCA20C4563541400B2437") || // Boktai 2 JPN Rev 1
						 (romHashSHA1 == "SHA1:2651C5E6875AC60ABFF734510D152166D211C87C"))   // Boktai 3
				{
					Console.WriteLine("Using Solar Sensor");

					cart_RAMS[i] = new byte[0x2000];
					mppr = 6;
				}
				else
				{
					cart_RAMS[i] = new byte[0x2000];
				}
			}
			else if (mppr == 7)
			{
				has_bats[i] = true;

				if (pokemon_check(romHashSHA1) ||
					(romHashSHA1 == "SHA1:4DCD7CEE46D3A5E848A22EB371BEBBBC2FB8D488")) // Sennen Kozoku
				{
					cart_RAMS[i] = new byte[0x20000];

					mppr = 8;
				}
				else
				{
					if (size_f == 64)
					{
						cart_RAMS[i] = new byte[0x10000];
					}
					else
					{
						cart_RAMS[i] = new byte[0x20000];
					}
				}
			}

			return mppr;
		}

		public bool pokemon_check(string romHashSHA1)
		{
			if ((romHashSHA1 == "SHA1:424740BE1FC67A5DDB954794443646E6AEEE2C1B") || // Pokemon Ruby (Germany) (Rev 1)
				(romHashSHA1 == "SHA1:1C2A53332382E14DAB8815E3A6DD81AD89534050") || // "" (Germany)	
				(romHashSHA1 == "SHA1:F28B6FFC97847E94A6C21A63CACF633EE5C8DF1E") || // "" (USA)
				(romHashSHA1 == "SHA1:5B64EACF892920518DB4EC664E62A086DD5F5BC8") || // "" (USA, Europe) (Rev 2)
				(romHashSHA1 == "SHA1:610B96A9C9A7D03D2BAFB655E7560CCFF1A6D894") || // "" (Europe) (Rev 1)
				(romHashSHA1 == "SHA1:A6EE94202BEC0641C55D242757E84DC89336D4CB") || // "" (France)
				(romHashSHA1 == "SHA1:BA888DFBA231A231CBD60FE228E894B54FB1ED79") || // "" (France) (Rev 1)
				(romHashSHA1 == "SHA1:5C5E546720300B99AE45D2AA35C646C8B8FF5C56") || // "" (Japan)
				(romHashSHA1 == "SHA1:1F49F7289253DCBFECBC4C5BA3E67AA0652EC83C") || // "" (Spain)
				(romHashSHA1 == "SHA1:9AC73481D7F5D150A018309BBA91D185CE99FB7C") || // "" (Spain) (Rev 1)
				(romHashSHA1 == "SHA1:2B3134224392F58DA00F802FAA1BF4B5CF6270BE") || // "" (Italy)
				(romHashSHA1 == "SHA1:015A5D380AFE316A2A6FCC561798EBFF9DFB3009") || // "" (Italy) (Rev 1)

				(romHashSHA1 == "SHA1:1692DB322400C3141C5DE2DB38469913CEB1F4D4") || // Pokemon Emerald (Italy)
				(romHashSHA1 == "SHA1:F3AE088181BF583E55DAF962A92BB46F4F1D07B7") || // "" (USA, Europe)
				(romHashSHA1 == "SHA1:FE1558A3DCB0360AB558969E09B690888B846DD9") || // "" (Spain)
				(romHashSHA1 == "SHA1:D7CF8F156BA9C455D164E1EA780A6BF1945465C2") || // "" (Japan)
				(romHashSHA1 == "SHA1:61C2EB2B380B1A75F0C94B767A2D4C26CD7CE4E3") || // "" (Germany)
				(romHashSHA1 == "SHA1:CA666651374D89CA439007BED54D839EB7BD14D0") || // "" (France)

				(romHashSHA1 == "SHA1:5A087835009D552D4C5C1F96BE3BE3206E378153") || // Pokemon Sapphire (Germany)
				(romHashSHA1 == "SHA1:7E6E034F9CDCA6D2C4A270FDB50A94DEF5883D17") || // "" (Germany) (Rev 1)
				(romHashSHA1 == "SHA1:4722EFB8CD45772CA32555B98FD3B9719F8E60A9") || // "" (Europe) (Rev 1)
				(romHashSHA1 == "SHA1:89B45FB172E6B55D51FC0E61989775187F6FE63C") || // "" (USA, Europe) (Rev 2)
				(romHashSHA1 == "SHA1:3CCBBD45F8553C36463F13B938E833F652B793E4") || // "" (USA)
				(romHashSHA1 == "SHA1:3233342C2F3087E6FFE6C1791CD5867DB07DF842") || // "" (Japan)
				(romHashSHA1 == "SHA1:0FE9AD1E602E2FAFA090AEE25E43D6980625173C") || // "" (Rev 1)
				(romHashSHA1 == "SHA1:3A6489189E581C4B29914071B79207883B8C16D8") || // "" (Spain)
				(romHashSHA1 == "SHA1:C269B5692B2D0E5800BA1DDF117FDA95AC648634") || // "" (France)
				(romHashSHA1 == "SHA1:860E93F5EA44F4278132F6C1EE5650D07B852FD8") || // "" (France) (Rev 1)
				(romHashSHA1 == "SHA1:73EDF67B9B82FF12795622DCA412733755D2C0FE") || // "" (Italy) (Rev 1)
				(romHashSHA1 == "SHA1:F729DD571FB2C09E72C5C1D68FE0A21E72713D34"))   // "" (Italy))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public ulong TotalExecutedCycles => 0;

		public void HardReset()
		{
			LibGBAHawkLink.GBALink_Hard_Reset(GBA_Pntr);
		}

		private IntPtr GBA_Pntr { get; set; } = IntPtr.Zero;
		private byte[] GBA_core = new byte[0xA0000 * 2];

		private readonly GBAHawkLink_ControllerDeck _controllerDeck;

		private int _frame = 0;

		public DisplayType Region => DisplayType.NTSC;

		private readonly ITraceable Tracer;

		private LibGBAHawkLink.TraceCallback tracecb;

		// these will be constant values assigned during core construction
		private int Header_Length;
		private readonly int Disasm_Length;
		private readonly int Reg_String_Length;

		private void MakeTrace(int t)
		{
			StringBuilder new_d = new StringBuilder(Disasm_Length);
			StringBuilder new_r = new StringBuilder(Reg_String_Length);

			uint tracer_core = (uint)Settings.TraceSet;

			LibGBAHawkLink.GBALink_getdisassembly(GBA_Pntr, new_d, t, Disasm_Length, tracer_core);
			LibGBAHawkLink.GBALink_getregisterstate(GBA_Pntr, new_r, t, Reg_String_Length, tracer_core);

			Tracer.Put(new(disassembly: new_d.ToString().PadRight(80), registerInfo: new_r.ToString()));
		}

		// GBA PPU Viewer
		public Action _scanlineCallback;
		public int _scanlineCallbackLine = 0;

		public void SetScanlineCallback(Action callback, int line)
		{
			_scanlineCallback = callback;
			_scanlineCallbackLine = line;

			if (line == -2)
			{
				_scanlineCallback();
			}
		}
	}
}
