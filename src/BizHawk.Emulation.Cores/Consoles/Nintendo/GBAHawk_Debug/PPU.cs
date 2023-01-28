﻿using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBAHawk_Debug
{
/*
	PPU Emulation
	NOTES: 

	Does turning on interrupts in the STAT register trigger interrupts if conditions are met? For now assume no.

	For mosaic sprites, does the check for Y range take into account mosaic effects? (for now assue no)

	When accessing OAM (7000000h) or OBJ VRAM (6010000h) by HBlank Timing, then the "H-Blank Interval Free" bit in DISPCNT register must be set.

	TODO: odd vertical windowing, mosaic, lock VRAM when rendering
*/

#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand

	public partial class GBAHawk_Debug
	{		
		public static readonly int[] ppu_OBJ_Sizes_X = {8, 16, 8, 8, 
														16, 32, 8, 8,
														32, 32, 16, 8,
														64, 64, 32, 8};

		public static readonly int[] ppu_OBJ_Sizes_Y = {8, 8, 16, 8,
														16, 8, 32, 8,
														32, 16, 32, 8,
														64, 32, 64, 8};

		public ushort[] ppu_BG_CTRL = new ushort[4];
		public ushort[] ppu_BG_X = new ushort[4];
		public ushort[] ppu_BG_Y = new ushort[4];

		public uint[] ppu_BG_Ref_X = new uint[4];
		public uint[] ppu_BG_Ref_Y = new uint[4];

		public int[] ppu_BG_On_Update_Time = new int[4];

		public ushort[] ppu_BG_Rot_A = new ushort[4];
		public ushort[] ppu_BG_Rot_B = new ushort[4];
		public ushort[] ppu_BG_Rot_C = new ushort[4];
		public ushort[] ppu_BG_Rot_D = new ushort[4];

		public ushort[] ppu_ROT_REF_LY = new ushort[4];

		public bool[] ppu_BG_On = new bool[4];
		public bool[] ppu_BG_On_New = new bool[4];

		public uint ppu_Transparent_Color;

		public int ppu_BG_Mode, ppu_Display_Frame;
		public int ppu_X_RS, ppu_Y_RS;

		public int ppu_VBL_IRQ_cd, ppu_HBL_IRQ_cd, ppu_LYC_IRQ_cd;

		public int ppu_HBL_Check_cd, ppu_LYC_Check_cd;

		public ushort ppu_CTRL, ppu_Green_Swap, ppu_Cycle, ppu_Display_Cycle, ppu_Sprite_Eval_Time;
		public ushort ppu_WIN_Hor_0, ppu_WIN_Hor_1, ppu_WIN_Vert_0, ppu_WIN_Vert_1;
		public ushort ppu_WIN_In, ppu_WIN_Out, ppu_Mosaic, ppu_Special_FX, ppu_Alpha, ppu_Bright;

		public byte ppu_STAT, ppu_LY, ppu_LYC, ppu_Sprite_Line;

		public bool ppu_HBL_Free, ppu_OBJ_Dim, ppu_Forced_Blank, ppu_Any_Window_On;
		public bool ppu_OBJ_On, ppu_WIN0_On, ppu_WIN1_On, ppu_OBJ_WIN;

		public bool ppu_In_VBlank;
		public bool ppu_Delays;

		// Sprite Evaluation
		public uint[] ppu_Sprite_Pixels = new uint[240 * 2];
		public int[] ppu_Sprite_Priority = new int[240 * 2];

		public bool[] ppu_Sprite_Pixel_Occupied = new bool[240 * 2];
		public bool[] ppu_Sprite_Semi_Transparent = new bool[240 * 2];
		public bool[] ppu_Sprite_Object_Window = new bool[240 * 2];

		public int ppu_Current_Sprite;
		public int ppu_Sprite_ofst_eval;
		public int ppu_Sprite_ofst_draw;
		public int ppu_Sprite_proc_time;
		public int ppu_Sprite_X_Pos, ppu_Sprite_Y_Pos;
		public int ppu_Sprite_X_Size, ppu_Sprite_Y_Size;

		public ushort ppu_Sprite_Attr_0, ppu_Sprite_Attr_1, ppu_Sprite_Attr_2;

		public bool ppu_New_Sprite, ppu_Sprite_Eval_Finished;

		// Derived values, not stated, reloaded with savestate
		public ushort[] ppu_ROT_OBJ_X = new ushort[128 * 128 * 128];
		public ushort[] ppu_ROT_OBJ_Y = new ushort[128 * 128 * 128];

		public ushort[] ppu_ROT_BG_X = new ushort[2 * 240];
		public ushort[] ppu_ROT_BG_Y = new ushort[2 * 240];

		public ushort[] ppu_MOS_OBJ_X = new ushort[0x200];
		public ushort[] ppu_MOS_OBJ_Y = new ushort[0x100];

		public ushort[] ppu_MOS_BG_X = new ushort[0x200];
		public ushort[] ppu_MOS_BG_Y = new ushort[0x200];

		public int[] BG_Scale_X = new int[4];
		public int[] BG_Scale_Y = new int[4];
		public int[] BG_Num_Tiles = new int[4];
		public int[] ppu_BG_Char_Base = new int[4];
		public int[] ppu_BG_Screen_Base = new int[4];

		public byte[] ppu_BG_Priority = new byte[4];
		public byte[] ppu_BG_Screen_Size = new byte[4];

		public bool[] ppu_BG_Mosaic = new bool[4];
		public bool[] ppu_BG_Pal_Size = new bool[4];
		public bool[] ppu_BG_Overflow = new bool[4];
		public bool[] ppu_WIN0_BG_En = new bool[4];
		public bool[] ppu_WIN1_BG_En = new bool[4];
		public bool[] ppu_OBJ_BG_En = new bool[4];
		public bool[] ppu_OUT_BG_En = new bool[4];

		public int ppu_SFX_mode, ppu_SFX_BRT_Num;
		public int ppu_SFX_Alpha_Num_1, ppu_SFX_Alpha_Num_2;

		public ushort ppu_OBJ_Mosaic_X, ppu_OBJ_Mosaic_Y;
		public ushort ppu_BG_Mosaic_X, ppu_BG_Mosaic_Y;

		public byte ppu_WIN0_Left, ppu_WIN1_Left, ppu_WIN0_Right, ppu_WIN1_Right;
		public byte ppu_WIN0_Top, ppu_WIN1_Top, ppu_WIN0_Bot, ppu_WIN1_Bot;

		public bool ppu_WIN0_OBJ_En, ppu_WIN1_OBJ_En, ppu_OUT_OBJ_En, ppu_OBJ_OBJ_En;
		public bool ppu_WIN0_Color_En, ppu_WIN1_Color_En, ppu_OUT_Color_En, ppu_OBJ_Color_En;

		public bool ppu_SFX_OBJ_Target_1, ppu_SFX_OBJ_Target_2;

		// volatile variables used every cycle in rendering, not stated
		public uint[] bg_pixel = new uint[4];

		public bool[] BG_Go = new bool[4];
		public bool[] BG_Has_Pixel = new bool[4];
		public bool[] BG_Is_Transparent = new bool[4];

		public ushort ppu_col_dat;

		public byte ppu_Read_Reg_8(uint addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0x00: ret = (byte)(ppu_CTRL & 0xFF); break;
				case 0x01: ret = (byte)((ppu_CTRL & 0xFF00) >> 8); break;
				case 0x02: ret = (byte)(ppu_Green_Swap & 0xFF); break;
				case 0x03: ret = (byte)((ppu_Green_Swap & 0xFF00) >> 8); break;
				case 0x04: ret = ppu_STAT; break;
				case 0x05: ret = ppu_LYC; break;
				case 0x06: ret = ppu_LY; break;
				case 0x07: ret = 0; break;

				case 0x08: ret = (byte)(ppu_BG_CTRL[0] & 0xFF); break;
				case 0x09: ret = (byte)((ppu_BG_CTRL[0] & 0xFF00) >> 8); break;
				case 0x0A: ret = (byte)(ppu_BG_CTRL[1] & 0xFF); break;
				case 0x0B: ret = (byte)((ppu_BG_CTRL[1] & 0xFF00) >> 8); break;
				case 0x0C: ret = (byte)(ppu_BG_CTRL[2] & 0xFF); break;
				case 0x0D: ret = (byte)((ppu_BG_CTRL[2] & 0xFF00) >> 8); break;
				case 0x0E: ret = (byte)(ppu_BG_CTRL[3] & 0xFF); break;
				case 0x0F: ret = (byte)((ppu_BG_CTRL[3] & 0xFF00) >> 8); break;

				case 0x48: ret = (byte)(ppu_WIN_In & 0xFF); break;
				case 0x49: ret = (byte)((ppu_WIN_In & 0xFF00) >> 8); break;
				case 0x4A: ret = (byte)(ppu_WIN_Out & 0xFF); break;
				case 0x4B: ret = (byte)((ppu_WIN_Out & 0xFF00) >> 8); break;

				case 0x50: ret = (byte)(ppu_Special_FX & 0xFF); break;
				case 0x51: ret = (byte)((ppu_Special_FX & 0xFF00) >> 8); break;
				case 0x52: ret = (byte)(ppu_Alpha & 0xFF); break;
				case 0x53: ret = (byte)((ppu_Alpha & 0xFF00) >> 8); break;

				default: ret = (byte)((cpu_Last_Bus_Value >> (8 * (int)(addr & 3))) & 0xFF); break; // open bus;
			}

			return ret;
		}

		public ushort ppu_Read_Reg_16(uint addr)
		{
			ushort ret = 0;

			switch (addr)
			{
				case 0x00: ret = ppu_CTRL; break;
				case 0x02: ret = ppu_Green_Swap; break;
				case 0x04: ret = (ushort)((ppu_LYC << 8) | ppu_STAT); break;
				case 0x06: ret = ppu_LY; break;

				case 0x08: ret = ppu_BG_CTRL[0]; break;
				case 0x0A: ret = ppu_BG_CTRL[1]; break;
				case 0x0C: ret = ppu_BG_CTRL[2]; break;
				case 0x0E: ret = ppu_BG_CTRL[3]; break;

				case 0x48: ret = ppu_WIN_In; break;
				case 0x4A: ret = ppu_WIN_Out; break;

				case 0x50: ret = ppu_Special_FX; break;
				case 0x52: ret = ppu_Alpha; break;

				default: ret = (ushort)(cpu_Last_Bus_Value & 0xFFFF); break; // open bus
			}

			return ret;
		}

		public uint ppu_Read_Reg_32(uint addr)
		{
			uint ret = 0;

			switch (addr)
			{
				case 0x00: ret = (uint)((ppu_Green_Swap << 16) | ppu_CTRL); break;
				case 0x04: ret = (uint)((ppu_LY << 16) | (ppu_LYC << 8) | ppu_STAT); break;

				case 0x08: ret = (uint)((ppu_BG_CTRL[1] << 16) | ppu_BG_CTRL[0]); break;
				case 0x0C: ret = (uint)((ppu_BG_CTRL[3] << 16) | ppu_BG_CTRL[2]); break;

				case 0x48: ret = (uint)((ppu_WIN_Out << 16) | ppu_WIN_In); break;

				case 0x50: ret = (uint)((ppu_Alpha << 16) | ppu_Special_FX); break;

				default: ret = cpu_Last_Bus_Value; break;
			}

			return ret;
		}

		public void ppu_Write_Reg_8(uint addr, byte value)
		{
			switch (addr)
			{
				case 0x00: ppu_CTRL_Write((ushort)((ppu_CTRL & 0xFF00) | value)); break;
				case 0x01: ppu_CTRL_Write((ushort)((ppu_CTRL & 0x00FF) | (value << 8))); break;
				case 0x02: ppu_Green_Swap = (ushort)((ppu_Green_Swap & 0xFF00) | value); break;
				case 0x03: ppu_Green_Swap = (ushort)((ppu_Green_Swap & 0x00FF) | (value << 8)); break;
				case 0x04: ppu_STAT_Write(value); break;
				case 0x05: ppu_LYC = value; break;
				case 0x06: // No Effect on LY
				case 0x07: // No Effect on LY
				case 0x08: ppu_BG_CTRL[0] = (ushort)((ppu_BG_CTRL[0] & 0xDF00) | value); ppu_BG_CTRL_Write(0); break;
				case 0x09: ppu_BG_CTRL[0] = (ushort)((ppu_BG_CTRL[0] & 0x00FF) | (value << 8)); ppu_BG_CTRL_Write(0); break;
				case 0x0A: ppu_BG_CTRL[1] = (ushort)((ppu_BG_CTRL[1] & 0xDF00) | value); ppu_BG_CTRL_Write(1); break;
				case 0x0B: ppu_BG_CTRL[1] = (ushort)((ppu_BG_CTRL[1] & 0x00FF) | (value << 8)); ppu_BG_CTRL_Write(1); break;
				case 0x0C: ppu_BG_CTRL[2] = (ushort)((ppu_BG_CTRL[2] & 0xFF00) | value); ppu_BG_CTRL_Write(2); break;
				case 0x0D: ppu_BG_CTRL[2] = (ushort)((ppu_BG_CTRL[2] & 0x00FF) | (value << 8)); ppu_BG_CTRL_Write(2); break;
				case 0x0E: ppu_BG_CTRL[3] = (ushort)((ppu_BG_CTRL[3] & 0xFF00) | value); ppu_BG_CTRL_Write(3); break;
				case 0x0F: ppu_BG_CTRL[3] = (ushort)((ppu_BG_CTRL[3] & 0x00FF) | (value << 8)); ppu_BG_CTRL_Write(3); break;

				case 0x10: ppu_BG_X[0] = (ushort)((ppu_BG_X[0] & 0xFF00) | value); break;
				case 0x11: ppu_BG_X[0] = (ushort)((ppu_BG_X[0] & 0x00FF) | (value << 8)); break;
				case 0x12: ppu_BG_Y[0] = (ushort)((ppu_BG_Y[0] & 0xFF00) | value); break;
				case 0x13: ppu_BG_Y[0] = (ushort)((ppu_BG_Y[0] & 0x00FF) | (value << 8)); break;
				case 0x14: ppu_BG_X[1] = (ushort)((ppu_BG_X[1] & 0xFF00) | value); break;
				case 0x15: ppu_BG_X[1] = (ushort)((ppu_BG_X[1] & 0x00FF) | (value << 8)); break;
				case 0x16: ppu_BG_Y[1] = (ushort)((ppu_BG_Y[1] & 0xFF00) | value); break;
				case 0x17: ppu_BG_Y[1] = (ushort)((ppu_BG_Y[1] & 0x00FF) | (value << 8)); break;
				case 0x18: ppu_BG_X[2] = (ushort)((ppu_BG_X[2] & 0xFF00) | value); break;
				case 0x19: ppu_BG_X[2] = (ushort)((ppu_BG_X[2] & 0x00FF) | (value << 8)); break;
				case 0x1A: ppu_BG_Y[2] = (ushort)((ppu_BG_Y[2] & 0xFF00) | value); break;
				case 0x1B: ppu_BG_Y[2] = (ushort)((ppu_BG_Y[2] & 0x00FF) | (value << 8)); break;
				case 0x1C: ppu_BG_X[3] = (ushort)((ppu_BG_X[3] & 0xFF00) | value); break;
				case 0x1D: ppu_BG_X[3] = (ushort)((ppu_BG_X[3] & 0x00FF) | (value << 8)); break;
				case 0x1E: ppu_BG_Y[3] = (ushort)((ppu_BG_Y[3] & 0xFF00) | value); break;
				case 0x1F: ppu_BG_Y[3] = (ushort)((ppu_BG_Y[3] & 0x00FF) | (value << 8)); break;

				case 0x20: ppu_BG_Rot_A[2] = (ushort)((ppu_BG_Rot_A[2] & 0xFF00) | value); break;
				case 0x21: ppu_BG_Rot_A[2] = (ushort)((ppu_BG_Rot_A[2] & 0x00FF) | (value << 8)); break;
				case 0x22: ppu_BG_Rot_B[2] = (ushort)((ppu_BG_Rot_B[2] & 0xFF00) | value); break;
				case 0x23: ppu_BG_Rot_B[2] = (ushort)((ppu_BG_Rot_B[2] & 0x00FF) | (value << 8)); break;
				case 0x24: ppu_BG_Rot_C[2] = (ushort)((ppu_BG_Rot_C[2] & 0xFF00) | value); break;
				case 0x25: ppu_BG_Rot_C[2] = (ushort)((ppu_BG_Rot_C[2] & 0x00FF) | (value << 8)); break;
				case 0x26: ppu_BG_Rot_D[2] = (ushort)((ppu_BG_Rot_D[2] & 0xFF00) | value); break;
				case 0x27: ppu_BG_Rot_D[2] = (ushort)((ppu_BG_Rot_D[2] & 0x00FF) | (value << 8)); break;
				case 0x28: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0xFFFFFF00) | value); break;
				case 0x29: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0xFFFF00FF) | (value << 8)); break;
				case 0x2A: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0xFF00FFFF) | (value << 16)); break;
				case 0x2B: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0x00FFFFFF) | (value << 24)); break;
				case 0x2C: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0xFFFFFF00) | value); ppu_ROT_REF_LY_Update(2); break;
				case 0x2D: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0xFFFF00FF) | (value << 8)); ppu_ROT_REF_LY_Update(2); break;
				case 0x2E: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0xFF00FFFF) | (value << 16)); ppu_ROT_REF_LY_Update(2); break;
				case 0x2F: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0x00FFFFFF) | (value << 24)); ppu_ROT_REF_LY_Update(2); break;

				case 0x30: ppu_BG_Rot_A[3] = (ushort)((ppu_BG_Rot_A[3] & 0xFF00) | value); break;
				case 0x31: ppu_BG_Rot_A[3] = (ushort)((ppu_BG_Rot_A[3] & 0x00FF) | (value << 8)); break;
				case 0x32: ppu_BG_Rot_B[3] = (ushort)((ppu_BG_Rot_B[3] & 0xFF00) | value); break;
				case 0x33: ppu_BG_Rot_B[3] = (ushort)((ppu_BG_Rot_B[3] & 0x00FF) | (value << 8)); break;
				case 0x34: ppu_BG_Rot_C[3] = (ushort)((ppu_BG_Rot_C[3] & 0xFF00) | value); break;
				case 0x35: ppu_BG_Rot_C[3] = (ushort)((ppu_BG_Rot_C[3] & 0x00FF) | (value << 8)); break;
				case 0x36: ppu_BG_Rot_D[3] = (ushort)((ppu_BG_Rot_D[3] & 0xFF00) | value); break;
				case 0x37: ppu_BG_Rot_D[3] = (ushort)((ppu_BG_Rot_D[3] & 0x00FF) | (value << 8)); break;
				case 0x38: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0xFFFFFF00) | value); break;
				case 0x39: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0xFFFF00FF) | (value << 8)); break;
				case 0x3A: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0xFF00FFFF) | (value << 16)); break;
				case 0x3B: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0x00FFFFFF) | (value << 24)); break;
				case 0x3C: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0xFFFFFF00) | value); ppu_ROT_REF_LY_Update(3); break;
				case 0x3D: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0xFFFF00FF) | (value << 8)); ppu_ROT_REF_LY_Update(3); break;
				case 0x3E: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0xFF00FFFF) | (value << 16)); ppu_ROT_REF_LY_Update(3); break;
				case 0x3F: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0x00FFFFFF) | (value << 24)); ppu_ROT_REF_LY_Update(3); break;

				case 0x40: ppu_WIN_Hor_0 = (ushort)((ppu_WIN_Hor_0 & 0xFF00) | value); ppu_Calc_Win0(); break;
				case 0x41: ppu_WIN_Hor_0 = (ushort)((ppu_WIN_Hor_0 & 0x00FF) | (value << 8)); ppu_Calc_Win0(); break;
				case 0x42: ppu_WIN_Hor_1 = (ushort)((ppu_WIN_Hor_1 & 0xFF00) | value); ppu_Calc_Win1(); break;
				case 0x43: ppu_WIN_Hor_1 = (ushort)((ppu_WIN_Hor_1 & 0x00FF) | (value << 8)); ppu_Calc_Win1(); break;
				case 0x44: ppu_WIN_Vert_0 = (ushort)((ppu_WIN_Vert_0 & 0xFF00) | value); ppu_Calc_Win0(); break;
				case 0x45: ppu_WIN_Vert_0 = (ushort)((ppu_WIN_Vert_0 & 0x00FF) | (value << 8)); ppu_Calc_Win0(); break;
				case 0x46: ppu_WIN_Vert_1 = (ushort)((ppu_WIN_Vert_1 & 0xFF00) | value); ppu_Calc_Win1(); break;
				case 0x47: ppu_WIN_Vert_1 = (ushort)((ppu_WIN_Vert_1 & 0x00FF) | (value << 8)); ppu_Calc_Win1(); break;
				case 0x48: ppu_Update_Win_In((ushort)((ppu_WIN_In & 0xFF00) | value)); break;
				case 0x49: ppu_Update_Win_In((ushort)((ppu_WIN_In & 0x00FF) | (value << 8))); break;
				case 0x4A: ppu_Update_Win_Out((ushort)((ppu_WIN_In & 0xFF00) | value)); break;
				case 0x4B: ppu_Update_Win_Out((ushort)((ppu_WIN_In & 0x00FF) | (value << 8))); break;
				case 0x4C: ppu_Update_Mosaic((ushort)((ppu_Mosaic & 0xFF00) | value)); break;
				case 0x4D: ppu_Update_Mosaic((ushort)((ppu_Mosaic & 0x00FF) | (value << 8))); break;

				case 0x50: ppu_Update_Special_FX((ushort)((ppu_WIN_In & 0xFF00) | value)); break;
				case 0x51: ppu_Update_Special_FX((ushort)((ppu_WIN_In & 0x00FF) | (value << 8))); break;
				case 0x52: ppu_Update_Alpha((ushort)((ppu_WIN_In & 0xFF00) | value)); break;
				case 0x53: ppu_Update_Alpha((ushort)((ppu_WIN_In & 0x00FF) | (value << 8))); break;
				case 0x54: ppu_Update_Bright((ushort)((ppu_Mosaic & 0xFF00) | value)); break;
				case 0x55: ppu_Update_Bright((ushort)((ppu_Mosaic & 0x00FF) | (value << 8))); break;
			}
		}

		public void ppu_Write_Reg_16(uint addr, ushort value)
		{
			switch (addr)
			{
				case 0x00: ppu_CTRL_Write(value); break;
				case 0x02: ppu_Green_Swap = value; break;
				case 0x04: ppu_STAT_Write((byte)value); ppu_LYC = (byte)(value >> 8); break;
				case 0x06: // No Effect on LY
				case 0x08: ppu_BG_CTRL[0] = (ushort)(value & 0xDFFF); ppu_BG_CTRL_Write(0); break;
				case 0x0A: ppu_BG_CTRL[1] = (ushort)(value & 0xDFFF); ppu_BG_CTRL_Write(1); break;
				case 0x0C: ppu_BG_CTRL[2] = value; ppu_BG_CTRL_Write(2); break;
				case 0x0E: ppu_BG_CTRL[3] = value; ppu_BG_CTRL_Write(3); break;

				case 0x10: ppu_BG_X[0] = value; break;
				case 0x12: ppu_BG_Y[0] = value; break;
				case 0x14: ppu_BG_X[1] = value; break;
				case 0x16: ppu_BG_Y[1] = value; break;
				case 0x18: ppu_BG_X[2] = value; break;
				case 0x1A: ppu_BG_Y[2] = value; break;
				case 0x1C: ppu_BG_X[3] = value; break;
				case 0x1E: ppu_BG_Y[3] = value; break;

				case 0x20: ppu_BG_Rot_A[2] = value; break;
				case 0x22: ppu_BG_Rot_B[2] = value; break;
				case 0x24: ppu_BG_Rot_C[2] = value; break;
				case 0x26: ppu_BG_Rot_D[2] = value; break;
				case 0x28: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0xFFFF0000) | value); break;
				case 0x2A: ppu_BG_Ref_X[2] = (uint)((ppu_BG_Ref_X[2] & 0x0000FFFF) | (value << 16)); break;
				case 0x2C: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0xFFFF0000) | value); ppu_ROT_REF_LY_Update(2); break;
				case 0x2E: ppu_BG_Ref_Y[2] = (uint)((ppu_BG_Ref_Y[2] & 0x0000FFFF) | (value << 16)); ppu_ROT_REF_LY_Update(2); break;

				case 0x30: ppu_BG_Rot_A[3] = value; break;
				case 0x32: ppu_BG_Rot_B[3] = value; break;
				case 0x34: ppu_BG_Rot_C[3] = value; break;
				case 0x36: ppu_BG_Rot_D[3] = value; break;
				case 0x38: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0xFFFF0000) | value); break;
				case 0x3A: ppu_BG_Ref_X[3] = (uint)((ppu_BG_Ref_X[3] & 0x0000FFFF) | (value << 16)); break;
				case 0x3C: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0xFFFF0000) | value); ppu_ROT_REF_LY_Update(3); break;
				case 0x3E: ppu_BG_Ref_Y[3] = (uint)((ppu_BG_Ref_Y[3] & 0x0000FFFF) | (value << 16)); ppu_ROT_REF_LY_Update(3); break;

				case 0x40: ppu_WIN_Hor_0 = value; ppu_Calc_Win0(); break;
				case 0x42: ppu_WIN_Hor_1 = value; ppu_Calc_Win1(); break;
				case 0x44: ppu_WIN_Vert_0 = value; ppu_Calc_Win0(); break;
				case 0x46: ppu_WIN_Vert_1 = value; ppu_Calc_Win1(); break;
				case 0x48: ppu_Update_Win_In(value); break;
				case 0x4A: ppu_Update_Win_Out(value); break;
				case 0x4C: ppu_Update_Mosaic(value); break;

				case 0x50: ppu_Update_Special_FX(value); break;
				case 0x52: ppu_Update_Alpha(value); break;
				case 0x54: ppu_Update_Bright(value); break;
			}
		}

		public void ppu_Write_Reg_32(uint addr, uint value)
		{
			switch (addr)
			{
				case 0x00: ppu_CTRL_Write((ushort)(value & 0xFFFF));
						   ppu_Green_Swap = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x04: ppu_STAT_Write((byte)value); ppu_LYC = (byte)(value >> 8); break;
						   /* no effect on LY*/
				case 0x08: ppu_BG_CTRL[0] = (ushort)(value & 0xDFFF); ppu_BG_CTRL_Write(0);
						   ppu_BG_CTRL[1] = (ushort)((value >> 16) & 0xDFFF); ppu_BG_CTRL_Write(1); break;
				case 0x0C: ppu_BG_CTRL[2] = (ushort)(value & 0xFFFF); ppu_BG_CTRL_Write(2);
						   ppu_BG_CTRL[3] = (ushort)((value >> 16) & 0xFFFF); ppu_BG_CTRL_Write(3); break;

				case 0x10: ppu_BG_X[0] = (ushort)(value & 0xFFFF);
						   ppu_BG_Y[0] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x14: ppu_BG_X[1] = (ushort)(value & 0xFFFF);
						   ppu_BG_Y[1] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x18: ppu_BG_X[2] = (ushort)(value & 0xFFFF);
						   ppu_BG_Y[2] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x1C: ppu_BG_X[3] = (ushort)(value & 0xFFFF);
						   ppu_BG_Y[3] = (ushort)((value >> 16) & 0xFFFF); break;

				case 0x20: ppu_BG_Rot_A[2] = (ushort)(value & 0xFFFF);
						   ppu_BG_Rot_B[2] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x24: ppu_BG_Rot_C[2] = (ushort)(value & 0xFFFF);
						   ppu_BG_Rot_D[2] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x28: ppu_BG_Ref_X[2] = value; break;
				case 0x2C: ppu_BG_Ref_Y[2] = value; ppu_ROT_REF_LY_Update(2); break;

				case 0x30: ppu_BG_Rot_A[3] = (ushort)(value & 0xFFFF);
						   ppu_BG_Rot_B[3] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x34: ppu_BG_Rot_C[3] = (ushort)(value & 0xFFFF);
						   ppu_BG_Rot_D[3] = (ushort)((value >> 16) & 0xFFFF); break;
				case 0x38: ppu_BG_Ref_X[3] = value; break;
				case 0x3C: ppu_BG_Ref_Y[3] = value; ppu_ROT_REF_LY_Update(3); break;

				case 0x40: ppu_WIN_Hor_0 = (ushort)(value & 0xFFFF);
						   ppu_WIN_Hor_1 = (ushort)((value >> 16) & 0xFFFF); ppu_Calc_Win0(); ppu_Calc_Win1(); break;
				case 0x44: ppu_WIN_Vert_0 = (ushort)(value & 0xFFFF);
						   ppu_WIN_Vert_1 = (ushort)((value >> 16) & 0xFFFF); ppu_Calc_Win0(); ppu_Calc_Win1(); break;
				case 0x48: ppu_Update_Win_In((ushort)(value & 0xFFFF));
						   ppu_Update_Win_Out((ushort)((value >> 16) & 0xFFFF)); break;
				case 0x4C: ppu_Update_Mosaic((ushort)(value & 0xFFFF)); 
						   /* no effect*/ break;

				case 0x50: ppu_Update_Special_FX((ushort)(value & 0xFFFF));
						   ppu_Update_Alpha((ushort)((value >> 16) & 0xFFFF)); break;
				case 0x54: ppu_Update_Bright((ushort)(value & 0xFFFF));
						   /* no effect*/ break;
			}
		}

		public void ppu_STAT_Write(byte value)
		{
			ppu_STAT &= 0xC7;
			ppu_STAT |= (byte)(value & 0x38);
		}

		public void ppu_CTRL_Write(ushort value)
		{
			ppu_BG_Mode = value & 7;
			ppu_Display_Frame = (value >> 4) & 1;

			ppu_HBL_Free = (value & 0x20) == 0x20;
			ppu_OBJ_Dim = (value & 0x40) == 0x40;
			ppu_Forced_Blank = (value & 0x80) == 0x80;

			ppu_OBJ_On = (value & 0x1000) == 0x1000;
			ppu_WIN0_On = (value & 0x2000) == 0x2000;
			ppu_WIN1_On = (value & 0x4000) == 0x4000;
			ppu_OBJ_WIN = (value & 0x8000) == 0x8000;

			// BG's don't turn on immediately, but do turn off immediately
			for (int i = 0; i < 4; i++)
			{
				if ((value & (0x100 << i)) == 0)
				{
					ppu_BG_On[i] = false;
					ppu_BG_On_Update_Time[i] = 0;
				}
				else if (ppu_BG_On_Update_Time[i] == 0)
				{
					// if the change happens before around cycle 40, then the delay is only 2 scanlines not 3
					// see spyro season of ice, talking to the first fairy
					if (ppu_Cycle < 40)
					{
						ppu_BG_On_Update_Time[i] = 2;
					}
					else
					{
						ppu_BG_On_Update_Time[i] = 3;
					}
				}
			}
			
			ppu_CTRL = value;

			if (ppu_HBL_Free) { ppu_Sprite_Eval_Time = 960; }
			else { ppu_Sprite_Eval_Time = 1216; }

			ppu_Any_Window_On = ppu_WIN0_On | ppu_WIN1_On | ppu_OBJ_WIN;

			// ensure BG controls match new mode
			ppu_BG_CTRL_Write(0);
			ppu_BG_CTRL_Write(1);
			ppu_BG_CTRL_Write(2);
			ppu_BG_CTRL_Write(3);

			//Console.WriteLine("free: " + ppu_HBL_Free);
			//Console.WriteLine("Mode: " + ppu_BG_Mode);
		}

		public void ppu_Calc_Win0()
		{
			ppu_WIN0_Right = (byte)(ppu_WIN_Hor_0 & 0xFF);
			ppu_WIN0_Left = (byte)((ppu_WIN_Hor_0 >> 8) & 0xFF);

			ppu_WIN0_Bot = (byte)(ppu_WIN_Vert_0 & 0xFF);
			ppu_WIN0_Top = (byte)((ppu_WIN_Vert_0 >> 8) & 0xFF);
		}

		public void ppu_Calc_Win1()
		{
			ppu_WIN1_Right = (byte)(ppu_WIN_Hor_1 & 0xFF);
			ppu_WIN1_Left = (byte)((ppu_WIN_Hor_1 >> 8) & 0xFF);

			ppu_WIN1_Bot = (byte)(ppu_WIN_Vert_1 & 0xFF);
			ppu_WIN1_Top = (byte)((ppu_WIN_Vert_1 >> 8) & 0xFF);
		}

		public void ppu_Update_Win_In(ushort value)
		{
			ppu_WIN_In = (ushort)(value & 0x3F3F);
			
			ppu_WIN0_BG_En[0] = (ppu_WIN_In & 0x1) == 0x1;
			ppu_WIN0_BG_En[1] = (ppu_WIN_In & 0x2) == 0x2;
			ppu_WIN0_BG_En[2] = (ppu_WIN_In & 0x4) == 0x4;
			ppu_WIN0_BG_En[3] = (ppu_WIN_In & 0x8) == 0x8;
			ppu_WIN0_OBJ_En = (ppu_WIN_In & 0x10) == 0x10;
			ppu_WIN0_Color_En = (ppu_WIN_In & 0x20) == 0x20;

			ppu_WIN1_BG_En[0] = (ppu_WIN_In & 0x100) == 0x100;
			ppu_WIN1_BG_En[1] = (ppu_WIN_In & 0x200) == 0x200;
			ppu_WIN1_BG_En[2] = (ppu_WIN_In & 0x400) == 0x400;
			ppu_WIN1_BG_En[3] = (ppu_WIN_In & 0x800) == 0x800;
			ppu_WIN1_OBJ_En = (ppu_WIN_In & 0x1000) == 0x1000;
			ppu_WIN1_Color_En = (ppu_WIN_In & 0x2000) == 0x2000;
		}

		public void ppu_Update_Win_Out(ushort value)
		{
			ppu_WIN_Out = (ushort)(value & 0x3F3F);

			ppu_OUT_BG_En[0] = (ppu_WIN_Out & 0x1) == 0x1;
			ppu_OUT_BG_En[1] = (ppu_WIN_Out & 0x2) == 0x2;
			ppu_OUT_BG_En[2] = (ppu_WIN_Out & 0x4) == 0x4;
			ppu_OUT_BG_En[3] = (ppu_WIN_Out & 0x8) == 0x8;
			ppu_OUT_OBJ_En = (ppu_WIN_Out & 0x10) == 0x10;
			ppu_OUT_Color_En = (ppu_WIN_Out & 0x20) == 0x20;

			ppu_OBJ_BG_En[0] = (ppu_WIN_Out & 0x100) == 0x100;
			ppu_OBJ_BG_En[1] = (ppu_WIN_Out & 0x200) == 0x200;
			ppu_OBJ_BG_En[2] = (ppu_WIN_Out & 0x400) == 0x400;
			ppu_OBJ_BG_En[3] = (ppu_WIN_Out & 0x800) == 0x800;
			ppu_OBJ_OBJ_En = (ppu_WIN_Out & 0x1000) == 0x1000;
			ppu_OBJ_Color_En = (ppu_WIN_Out & 0x2000) == 0x2000;
		}

		public void ppu_ROT_REF_LY_Update(int layer)
		{
			if (ppu_BG_On[layer])
			{
				if (ppu_Cycle < 40)
				{
					ppu_ROT_REF_LY[layer] = ppu_LY;
				}
				else
				{
					ppu_ROT_REF_LY[layer] = (ushort)(ppu_LY + 1);
				}			
			}
		}

		public void ppu_Update_Special_FX(ushort value)
		{
			ppu_Special_FX = (ushort)(value & 0x3FFF);

			ppu_SFX_mode = ((ppu_Special_FX >> 6) & 3);

			ppu_SFX_OBJ_Target_1 = ((ppu_Special_FX >> 4) & 0x1) != 0;

			ppu_SFX_OBJ_Target_2 = ((ppu_Special_FX >> 12) & 0x1) != 0;
		}

		public void ppu_Update_Alpha(ushort value)
		{
			ppu_Alpha = (ushort)(value & 0x1F1F);

			if ((ppu_Alpha & 0x10) == 0x10)
			{
				ppu_SFX_Alpha_Num_1 = 16;
			}
			else
			{
				ppu_SFX_Alpha_Num_1 = ppu_Alpha & 0xF;
			}

			if ((ppu_Alpha & 0x1000) == 0x1000)
			{
				ppu_SFX_Alpha_Num_2 = 16;
			}
			else
			{
				ppu_SFX_Alpha_Num_2 = (ppu_Alpha >> 8) & 0xF;
			}
		}

		public void ppu_Update_Bright(ushort value)
		{
			ppu_Bright = value;

			if ((ppu_Bright & 0x10) == 0x10)
			{
				ppu_SFX_BRT_Num = 16;
			}
			else
			{
				ppu_SFX_BRT_Num = ppu_Bright & 0xF;
			}
		}

		public void ppu_Update_Mosaic(ushort value)
		{
			ppu_Mosaic = value;

			int j;

			ushort mosaic_x, mosaic_y;

			ppu_BG_Mosaic_X = (ushort)((ppu_Mosaic & 0xF) + 1);
			ppu_BG_Mosaic_Y = (ushort)(((ppu_Mosaic >> 4) & 0xF) + 1);
			ppu_OBJ_Mosaic_X = (ushort)(((ppu_Mosaic >> 8) & 0xF) + 1);
			ppu_OBJ_Mosaic_Y = (ushort)(((ppu_Mosaic >> 12) & 0xF) + 1);

			mosaic_x = 0;
			j = 0;

			for (int i = 0; i < 0x200; i++) 
			{
				ppu_MOS_BG_X[i] = mosaic_x;

				j++;

				if (j == ppu_BG_Mosaic_X)
				{
					mosaic_x += ppu_BG_Mosaic_X;
					j = 0;
				}				
			}

			mosaic_y = 0;
			j = 0;

			for (int i = 0; i < 0x200; i++)
			{
				ppu_MOS_BG_Y[i] = mosaic_y;

				j++;

				if (j == ppu_BG_Mosaic_Y)
				{
					mosaic_y += ppu_BG_Mosaic_Y;
					j = 0;
				}
			}

			mosaic_x = 0;
			j = 0;

			for (int i = 0; i < 0x200; i++)
			{
				ppu_MOS_OBJ_X[i] = mosaic_x;

				j++;

				if (j == ppu_OBJ_Mosaic_X)
				{
					mosaic_x += ppu_OBJ_Mosaic_X;
					j = 0;
				}
			}

			mosaic_y = 0;
			j = 0;

			for (int i = 0; i < 0x100; i++)
			{
				ppu_MOS_OBJ_Y[i] = mosaic_y;

				j++;

				if (j == ppu_OBJ_Mosaic_Y)
				{
					mosaic_y += ppu_OBJ_Mosaic_Y;
					j = 0;
				}
			}	
		}

		public void ppu_BG_CTRL_Write(int lyr)
		{
			ppu_BG_Priority[lyr] = (byte)(ppu_BG_CTRL[lyr] & 3);
			ppu_BG_Char_Base[lyr] = ((ppu_BG_CTRL[lyr] >> 2) & 3) * 16 * 1024;
			ppu_BG_Mosaic[lyr] = (ppu_BG_CTRL[lyr] & 0x40) == 0x40;
			ppu_BG_Pal_Size[lyr] = (ppu_BG_CTRL[lyr] & 0x80) == 0x80;
			ppu_BG_Screen_Base[lyr] = ((ppu_BG_CTRL[lyr] >> 8) & 0x1F) * 2 * 1024;
			ppu_BG_Overflow[lyr] = (ppu_BG_CTRL[lyr] & 0x2000) == 0x2000;
			ppu_BG_Screen_Size[lyr] = (byte)((ppu_BG_CTRL[lyr] >> 14) & 3);

			if (ppu_BG_Mode == 0)
			{
				switch (ppu_BG_Screen_Size[lyr])
				{
					case 0:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 256;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 1:
						BG_Scale_X[lyr] = 512;
						BG_Scale_Y[lyr] = 256;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 2:
						BG_Scale_X[lyr] = 256;
						BG_Scale_Y[lyr] = 512;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 3:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 512;
						BG_Num_Tiles[lyr] = 32;
						break;
				}
			}
			else if ((lyr < 2) && (ppu_BG_Mode == 1))
			{
				switch (ppu_BG_Screen_Size[lyr])
				{
					case 0:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 256;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 1:
						BG_Scale_X[lyr] = 512;
						BG_Scale_Y[lyr] = 256;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 2:
						BG_Scale_X[lyr] = 256;
						BG_Scale_Y[lyr] = 512;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 3:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 512;
						BG_Num_Tiles[lyr] = 32;
						break;
				}
			}
			else
			{
				switch (ppu_BG_Screen_Size[lyr])
				{
					case 0:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 128;
						BG_Num_Tiles[lyr] = 16;
						break;

					case 1:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 256;
						BG_Num_Tiles[lyr] = 32;
						break;

					case 2:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 512;
						BG_Num_Tiles[lyr] = 64;
						break;

					case 3:
						BG_Scale_X[lyr] = BG_Scale_Y[lyr] = 1024;
						BG_Num_Tiles[lyr] = 128;
						break;
				}
			}	
		}

		public void ppu_Tick()
		{
			// start the next scanline
			if (ppu_Cycle == 1232)
			{
				// scanline callback
				if ((ppu_LY + 1) == _scanlineCallbackLine)
				{
					if (_scanlineCallback != null)
					{
						_scanlineCallback();
					}
				}

				ppu_Cycle = 0;
				ppu_Display_Cycle = 0;
				ppu_LY += 1;

				if (ppu_LY == 228)
				{
					// display starts occuring at scanline 0
					ppu_In_VBlank = false;
					ppu_LY = 0;

					// reset rotation and scaling offsetreference Y value
					ppu_ROT_REF_LY[2] = 0;
					ppu_ROT_REF_LY[3] = 0;
				}

				ppu_Sprite_Line= (byte)(ppu_LY + 1);

				// video capture DMA, check timing
				if (dma_Go[3] && dma_Start_Snd_Vid[3])
				{
					if ((ppu_LY >= 2) && (ppu_LY < 162))
					{
						dma_Run[3] = true;
					}
				}

				// exit HBlank
				ppu_STAT &= 0xFD;

				// clear the LYC flag bit
				ppu_STAT &= 0xFB;

				// Check LY = LYC in 2 cycles
				ppu_LYC_Check_cd = 2;
				ppu_Delays = true;
				delays_to_process = true;

				if (ppu_LY == 160)
				{
					// vblank flag turns on on scanline 160
					VBlank_Rise = true;
					ppu_In_VBlank = true;
					ppu_STAT |= 1;

					// trigger VBL IRQ
					if ((ppu_STAT & 0x8) == 0x8)
					{
						ppu_VBL_IRQ_cd = 3;
						ppu_Delays = true;
						delays_to_process = true;
					}

					// trigger any DMAs with VBlank as a start condition
					if (dma_Go[0] && dma_Start_VBL[0]) { dma_Run[0] = true; }
					if (dma_Go[1] && dma_Start_VBL[1]) { dma_Run[1] = true; }
					if (dma_Go[2] && dma_Start_VBL[2]) { dma_Run[2] = true; }
					if (dma_Go[3] && dma_Start_VBL[3]) { dma_Run[3] = true; }

					// do core actions that happen on VBlank
					On_VBlank();
				}
				else if (ppu_LY == 227)
				{
					// vblank flag turns off on scanline 227
					ppu_STAT &= 0xFE;

					ppu_Sprite_Line = 0;

					// calculate parameters for rotated / scaled sprites at the end of vblank
					ppu_Calculate_Sprites_Pixels(0, true);
				}

				// reset sprite evaluation variables
				ppu_Current_Sprite = 0;
				ppu_New_Sprite = true;
				ppu_Sprite_Eval_Finished = false;
				ppu_Sprite_proc_time = 6;

				if (ppu_Sprite_ofst_eval == 0)
				{
					ppu_Sprite_ofst_eval = 240;
					ppu_Sprite_ofst_draw = 0;
				}
				else
				{
					ppu_Sprite_ofst_eval = 0;
					ppu_Sprite_ofst_draw = 240;
				}

				// reset obj window detection for the scanline
				for (int i = ppu_Sprite_ofst_eval; i < (240 + ppu_Sprite_ofst_eval); i++)
				{
					ppu_Sprite_Pixels[i] = 0;
					ppu_Sprite_Priority[i] = 0;
					ppu_Sprite_Pixel_Occupied[i] = false;
					ppu_Sprite_Semi_Transparent[i] = false;
					ppu_Sprite_Object_Window[i] = false;
				}

				// update BG toggles
				for (int i = 0; i < 4; i++)
				{
					if (ppu_BG_On_Update_Time[i] > 0)
					{
						ppu_BG_On_Update_Time[i]--;

						if (ppu_BG_On_Update_Time[i] == 0)
						{
							ppu_BG_On[i] = true;
						}
					}
				}
			}
			else if (ppu_Cycle == 1007)
			{
				// trigger any DMAs with HBlank as a start condition
				// but not if in vblank
				if (ppu_LY < 160)
				{
					if (dma_Go[0] && dma_Start_HBL[0]) { dma_Run[0] = true; }
					if (dma_Go[1] && dma_Start_HBL[1]) { dma_Run[1] = true; }
					if (dma_Go[2] && dma_Start_HBL[2]) { dma_Run[2] = true; }
					if (dma_Go[3] && dma_Start_HBL[3]) { dma_Run[3] = true; }
				}

				// Hblank starts on next cycle
				ppu_HBL_Check_cd = 1;
				ppu_Delays = true;
				delays_to_process = true;
			}

			if (!ppu_Forced_Blank)
			{
				if (ppu_In_VBlank)
				{
					// Any glitchy stuff happens in VBlank? Maybe prefetch on scanline 227?
					if ((ppu_LY == 227) && !ppu_Sprite_Eval_Finished && (ppu_Cycle < ppu_Sprite_Eval_Time))
					{
						ppu_Render_Sprites();
					}
				}
				else
				{
					if (!ppu_Sprite_Eval_Finished && (ppu_Cycle < ppu_Sprite_Eval_Time))
					{
						ppu_Render_Sprites();
					}

					// for now only do things every 4 cycles for the BG
					if ((ppu_Cycle & 3) == 0)
					{
						// calculate scanline rotation and scaling
						if (ppu_Cycle == 40)
						{
							if (ppu_BG_Mode > 0)
							{
								ppu_Calculate_BG_Rotations(2);

								if (ppu_BG_Mode == 2)
								{
									ppu_Calculate_BG_Rotations(3);
								}
							}
						}
						
						if ((ppu_Cycle >= 40) && (ppu_Cycle < 1000))
						{
							ppu_Render();

							ppu_Display_Cycle += 1;
						}
					}
				}
			}

			ppu_Cycle += 1;
		}

		public void ppu_Render()
		{
			uint bg_pixel_f = ppu_Transparent_Color;
			uint bg_pixel_s = ppu_Transparent_Color;
			uint spr_pixel = 0xFFFFFFFF;
			uint final_pixel = 0xFFFFFFFF;

			uint bld_pixel_1 = 0xFFFFFFFF;
			uint bld_pixel_2 = 0xFFFFFFFF;

			int cur_layer_priority = 4;
			int second_layer_priority = 4;

			int cur_BG_layer = 5;  // convenient to set to 5 for special effects
			int second_BG_layer = 5;

			int Pixel_Color;

			uint Pixel_Color_data;

			uint R,G,B;
			uint R2, G2, B2;

			int VRAM_ofst_X, VRAM_ofst_Y;

			int Tile_Addr;

			int Screen_Offset;

			byte BG_Effect_Byte;

			bool Is_Outside = true;
			bool OBJ_Has_Pixel = false;
			bool OBJ_Go = false;
			bool Color_FX_Go = false;

			bool Brighten_Final_Pixel = false;
			bool Blend_Final_Pixel = false;

			for (int a = 0; a < 4; a++)
			{
				bg_pixel[a] = 0xFFFFFFFF;
				BG_Go[a] = true;
				BG_Has_Pixel[a] = false;
				BG_Is_Transparent[a] = true;
			}

			OBJ_Has_Pixel = ppu_Sprite_Pixel_Occupied[ppu_Sprite_ofst_draw + ppu_Display_Cycle] & ppu_OBJ_On;

			// Check enabled pixels
			if (ppu_Any_Window_On)
			{
				if (ppu_WIN0_On && (((ppu_Display_Cycle - ppu_WIN0_Left) & 0xFF) < ((ppu_WIN0_Right - ppu_WIN0_Left) & 0xFF)) &&
					(((ppu_LY - ppu_WIN0_Top) & 0xFF) < ((ppu_WIN0_Bot - ppu_WIN0_Top) & 0xFF)))
				{
					Is_Outside = false;

					for (int w0 = 0; w0 < 4; w0++) { BG_Go[w0] &= ppu_WIN0_BG_En[w0] & ppu_BG_On[w0]; }

					OBJ_Go = ppu_WIN0_OBJ_En & OBJ_Has_Pixel;
					Color_FX_Go = ppu_WIN0_Color_En;
				}
				else if (ppu_WIN1_On && (((ppu_Display_Cycle - ppu_WIN1_Left) & 0xFF) < ((ppu_WIN1_Right - ppu_WIN1_Left) & 0xFF)) &&
						(((ppu_LY - ppu_WIN1_Top) & 0xFF) < ((ppu_WIN1_Bot - ppu_WIN1_Top) & 0xFF)))
				{
					Is_Outside = false;

					for (int w1 = 0; w1 < 4; w1++) { BG_Go[w1] &= ppu_WIN1_BG_En[w1] & ppu_BG_On[w1]; }

					OBJ_Go = ppu_WIN1_OBJ_En & OBJ_Has_Pixel;
					Color_FX_Go = ppu_WIN1_Color_En;
				}
				else if(ppu_OBJ_WIN && ppu_Sprite_Object_Window[ppu_Sprite_ofst_draw + ppu_Display_Cycle])
				{
					Is_Outside = false;

					for (int ob = 0; ob < 4; ob++) { BG_Go[ob] &= ppu_OBJ_BG_En[ob] & ppu_BG_On[ob]; }

					OBJ_Go = ppu_OBJ_OBJ_En & OBJ_Has_Pixel;
					Color_FX_Go = ppu_OBJ_Color_En;
				}

				if (Is_Outside)
				{
					for (int outs = 0; outs < 4; outs++) { BG_Go[outs] &= ppu_OUT_BG_En[outs] & ppu_BG_On[outs]; }

					OBJ_Go = ppu_OUT_OBJ_En & OBJ_Has_Pixel;
					Color_FX_Go = ppu_OUT_Color_En;
				}
			}
			else
			{
				for (int n = 0; n < 4; n++) { BG_Go[n] = ppu_BG_On[n]; }

				OBJ_Go = OBJ_Has_Pixel;
				Color_FX_Go = true;
			}

			switch (ppu_BG_Mode)
			{
				case 0:
					for (int c0 = 0; c0 < 4; c0++)
					{
						if (BG_Go[c0])
						{
							// calculate scrolling
							if (ppu_BG_Mosaic[c0])
							{
								ppu_X_RS = (int)((ppu_MOS_BG_X[ppu_Display_Cycle] + ppu_BG_X[c0]) & 0x1FF);
								ppu_Y_RS = (int)((ppu_MOS_BG_Y[ppu_LY] + ppu_BG_Y[c0]) & 0x1FF);
							}
							else
							{
								ppu_X_RS = (int)((ppu_Display_Cycle + ppu_BG_X[c0]) & 0x1FF);
								ppu_Y_RS = (int)((ppu_LY + ppu_BG_Y[c0]) & 0x1FF);
							}
							
							// always wrap around
							ppu_X_RS &= (BG_Scale_X[c0] - 1);
							ppu_Y_RS &= (BG_Scale_Y[c0] - 1);

							// determine if pixel is in valid range, and pic out color if so
							if ((ppu_X_RS >= 0) && (ppu_Y_RS >= 0) && (ppu_X_RS < BG_Scale_X[c0]) && (ppu_Y_RS < BG_Scale_Y[c0]))
							{
								VRAM_ofst_X = ppu_X_RS >> 3;
								VRAM_ofst_Y = ppu_Y_RS >> 3;

								Screen_Offset = 0;

								if (VRAM_ofst_X > 31)
								{
									if (VRAM_ofst_Y > 31)
									{
										Screen_Offset = 6 * 1024;
									}
									else
									{
										Screen_Offset = 2 * 1024;
									}
								}
								else if (VRAM_ofst_Y > 31)
								{
									if (ppu_BG_Screen_Size[c0] == 2)
									{
										Screen_Offset = 2 * 1024;
									}
									else
									{
										Screen_Offset = 4 * 1024;
									}
								}

								VRAM_ofst_X &= 31;
								VRAM_ofst_Y &= 31;

								Tile_Addr = ppu_BG_Screen_Base[c0] + Screen_Offset + VRAM_ofst_Y * BG_Num_Tiles[c0] * 2 + VRAM_ofst_X * 2;

								BG_Effect_Byte = VRAM[Tile_Addr + 1];

								Tile_Addr = VRAM[Tile_Addr] | ((VRAM[Tile_Addr + 1] & 3) << 8);

								if (ppu_BG_Pal_Size[c0])
								{
									Tile_Addr = Tile_Addr * 64 + ppu_BG_Char_Base[c0];

									if ((BG_Effect_Byte & 0x4) == 0x0)
									{
										Tile_Addr += (ppu_X_RS & 7);
									}
									else
									{
										Tile_Addr += 7 - (ppu_X_RS & 7);
									}

									if ((BG_Effect_Byte & 0x8) == 0x0)
									{
										Tile_Addr += (ppu_Y_RS & 7) * 8;
									}
									else
									{
										Tile_Addr += (7 - (ppu_Y_RS & 7)) * 8;
									}

									Pixel_Color = VRAM[Tile_Addr] << 1;

									BG_Is_Transparent[c0] = Pixel_Color == 0;
								}
								else
								{
									Tile_Addr = Tile_Addr * 32 + ppu_BG_Char_Base[c0];

									if ((BG_Effect_Byte & 0x8) == 0x0)
									{
										Tile_Addr += (ppu_Y_RS & 7) * 4;
									}
									else
									{
										Tile_Addr += (7 - (ppu_Y_RS & 7)) * 4;
									}

									if ((BG_Effect_Byte & 0x4) == 0x0)
									{
										Tile_Addr += ((ppu_X_RS & 7) >> 1);

										if ((ppu_X_RS & 1) == 0)
										{
											Pixel_Color = VRAM[Tile_Addr] & 0xF;
										}
										else
										{
											Pixel_Color = (VRAM[Tile_Addr] >> 4) & 0xF;
										}
									}
									else
									{
										Tile_Addr += ((7 - (ppu_X_RS & 7)) >> 1);

										if ((ppu_X_RS & 1) == 1)
										{
											Pixel_Color = VRAM[Tile_Addr] & 0xF;
										}
										else
										{
											Pixel_Color = (VRAM[Tile_Addr] >> 4) & 0xF;
										}
									}

									Pixel_Color <<= 1;

									Pixel_Color += 32 * (BG_Effect_Byte >> 4);

									BG_Is_Transparent[c0] = (Pixel_Color & 0x1F) == 0;
								}

								Pixel_Color_data = (ushort)(PALRAM[Pixel_Color] + (PALRAM[Pixel_Color + 1] << 8));

								bg_pixel[c0] = (uint)(0xFF000000 |
											   ((Pixel_Color_data & 0x1F) << 19) |
											   ((Pixel_Color_data & 0x3E0) << 6) |
											   ((Pixel_Color_data & 0x7C00) >> 7));

								BG_Has_Pixel[c0] = true;
							}
							else
							{
								BG_Is_Transparent[c0] = true;
							}
						}

						if (!BG_Is_Transparent[c0])
						{
							if (ppu_BG_Priority[c0] < cur_layer_priority)
							{							
								bg_pixel_s = bg_pixel_f;
								second_BG_layer = cur_BG_layer;
								second_layer_priority = cur_layer_priority;

								bg_pixel_f = bg_pixel[c0];
								cur_BG_layer = c0;
								cur_layer_priority = ppu_BG_Priority[c0];
							}
							else if (ppu_BG_Priority[c0] < second_layer_priority)
							{
								bg_pixel_s = bg_pixel[c0];
								second_BG_layer = c0;
								second_layer_priority = ppu_BG_Priority[c0];
							}
						}
					}
					break;

				case 1:
					for (int c1 = 0; c1 < 2; c1++)
					{
						if (BG_Go[c1])
						{
							// calculate scrolling
							if (ppu_BG_Mosaic[c1])
							{
								ppu_X_RS = (int)((ppu_MOS_BG_X[ppu_Display_Cycle] + ppu_BG_X[c1]) & 0x1FF);
								ppu_Y_RS = (int)((ppu_MOS_BG_Y[ppu_LY] + ppu_BG_Y[c1]) & 0x1FF);
							}
							else
							{
								ppu_X_RS = (int)((ppu_Display_Cycle + ppu_BG_X[c1]) & 0x1FF);
								ppu_Y_RS = (int)((ppu_LY + ppu_BG_Y[c1]) & 0x1FF);
							}

							// always wrap around
							ppu_X_RS &= (BG_Scale_X[c1] - 1);
							ppu_Y_RS &= (BG_Scale_Y[c1] - 1);

							// determine if pixel is in valid range, and pick out color if so
							if ((ppu_X_RS >= 0) && (ppu_Y_RS >= 0) && (ppu_X_RS < BG_Scale_X[c1]) && (ppu_Y_RS < BG_Scale_Y[c1]))
							{
								VRAM_ofst_X = ppu_X_RS >> 3;
								VRAM_ofst_Y = ppu_Y_RS >> 3;

								Screen_Offset = 0;

								if (VRAM_ofst_X > 31)
								{
									if (VRAM_ofst_Y > 31)
									{
										Screen_Offset = 6 * 1024;
									}
									else
									{
										Screen_Offset = 2 * 1024;
									}
								}
								else if (VRAM_ofst_Y > 31)
								{
									if (ppu_BG_Screen_Size[c1] == 2)
									{
										Screen_Offset = 2 * 1024;
									}
									else
									{
										Screen_Offset = 4 * 1024;
									}
								}

								VRAM_ofst_X &= 31;
								VRAM_ofst_Y &= 31;

								Tile_Addr = ppu_BG_Screen_Base[c1] + Screen_Offset + VRAM_ofst_Y * BG_Num_Tiles[c1] * 2 + VRAM_ofst_X * 2;

								BG_Effect_Byte = VRAM[Tile_Addr + 1];

								Tile_Addr = VRAM[Tile_Addr] | ((VRAM[Tile_Addr + 1] & 3) << 8);

								if (ppu_BG_Pal_Size[c1])
								{
									Tile_Addr = Tile_Addr * 64 + ppu_BG_Char_Base[c1];

									if ((BG_Effect_Byte & 0x4) == 0x0)
									{
										Tile_Addr += (ppu_X_RS & 7);
									}
									else
									{
										Tile_Addr += 7 - (ppu_X_RS & 7);
									}

									if ((BG_Effect_Byte & 0x8) == 0x0)
									{
										Tile_Addr += (ppu_Y_RS & 7) * 8;
									}
									else
									{
										Tile_Addr += (7 - (ppu_Y_RS & 7)) * 8;
									}

									Pixel_Color = VRAM[Tile_Addr] << 1;

									BG_Is_Transparent[c1] = Pixel_Color == 0;
								}
								else
								{
									Tile_Addr = Tile_Addr * 32 + ppu_BG_Char_Base[c1];

									if ((BG_Effect_Byte & 0x8) == 0x0)
									{
										Tile_Addr += (ppu_Y_RS & 7) * 4;
									}
									else
									{
										Tile_Addr += (7 - (ppu_Y_RS & 7)) * 4;
									}

									if ((BG_Effect_Byte & 0x4) == 0x0)
									{
										Tile_Addr += ((ppu_X_RS & 7) >> 1);

										if ((ppu_X_RS & 1) == 0)
										{
											Pixel_Color = VRAM[Tile_Addr] & 0xF;
										}
										else
										{
											Pixel_Color = (VRAM[Tile_Addr] >> 4) & 0xF;
										}
									}
									else
									{
										Tile_Addr += ((7 - (ppu_X_RS & 7)) >> 1);

										if ((ppu_X_RS & 1) == 1)
										{
											Pixel_Color = VRAM[Tile_Addr] & 0xF;
										}
										else
										{
											Pixel_Color = (VRAM[Tile_Addr] >> 4) & 0xF;
										}
									}

									Pixel_Color <<= 1;

									Pixel_Color += 32 * (BG_Effect_Byte >> 4);

									BG_Is_Transparent[c1] = (Pixel_Color & 0x1F) == 0;
								}

								Pixel_Color_data = (ushort)(PALRAM[Pixel_Color] + (PALRAM[Pixel_Color + 1] << 8));

								bg_pixel[c1] = (uint)(0xFF000000 |
												((Pixel_Color_data & 0x1F) << 19) |
												((Pixel_Color_data & 0x3E0) << 6) |
												((Pixel_Color_data & 0x7C00) >> 7));

								BG_Has_Pixel[c1] = true;
							}
							else
							{
								BG_Is_Transparent[c1] = true;
							}
						}

						if (!BG_Is_Transparent[c1])
						{
							if (ppu_BG_Priority[c1] < cur_layer_priority)
							{
								bg_pixel_s = bg_pixel_f;
								second_BG_layer = cur_BG_layer;
								second_layer_priority = cur_layer_priority;

								bg_pixel_f = bg_pixel[c1];
								cur_BG_layer = c1;
								cur_layer_priority = ppu_BG_Priority[c1];
							}
							else if (ppu_BG_Priority[c1] < second_layer_priority)
							{
								bg_pixel_s = bg_pixel[c1];
								second_BG_layer = c1;
								second_layer_priority = ppu_BG_Priority[c1];
							}
						}
					}

					if (BG_Go[2])
					{
						// calculate rotation and scaling
						ppu_X_RS = ppu_ROT_BG_X[ppu_Display_Cycle];
						ppu_Y_RS = ppu_ROT_BG_Y[ppu_Display_Cycle];			

						// adjust if wraparound is enabled
						if (ppu_BG_Overflow[2])
						{
							ppu_X_RS &= (BG_Scale_X[2] - 1);
							ppu_Y_RS &= (BG_Scale_Y[2] - 1);
						}

						// determine if pixel is in valid range, and pick out color if so
						if ((ppu_X_RS >= 0) && (ppu_Y_RS >= 0) && (ppu_X_RS < BG_Scale_X[2]) && (ppu_Y_RS < BG_Scale_Y[2]))
						{
							VRAM_ofst_X = ppu_X_RS >> 3;
							VRAM_ofst_Y = ppu_Y_RS >> 3;

							Tile_Addr = ppu_BG_Screen_Base[2] + VRAM_ofst_Y * BG_Num_Tiles[2] + VRAM_ofst_X;

							Tile_Addr = VRAM[Tile_Addr];

							Tile_Addr = Tile_Addr * 64 + ppu_BG_Char_Base[2];

							Tile_Addr = Tile_Addr + (ppu_X_RS & 7) + (ppu_Y_RS & 7) * 8;

							Pixel_Color = (VRAM[Tile_Addr] << 1);

							Pixel_Color_data = (ushort)(PALRAM[Pixel_Color] + (PALRAM[Pixel_Color + 1] << 8));

							bg_pixel[2] = (uint)(0xFF000000 |
										  ((Pixel_Color_data & 0x1F) << 19) |
										  ((Pixel_Color_data & 0x3E0) << 6) |
										  ((Pixel_Color_data & 0x7C00) >> 7));

							BG_Is_Transparent[2] = Pixel_Color == 0;

							BG_Has_Pixel[2] = true;
						}
						else
						{
							BG_Is_Transparent[2] = true;
						}

						if (!BG_Is_Transparent[2])
						{
							if (ppu_BG_Priority[2] < cur_layer_priority)
							{
								bg_pixel_s = bg_pixel_f;
								second_BG_layer = cur_BG_layer;
								second_layer_priority = cur_layer_priority;

								bg_pixel_f = bg_pixel[2];
								cur_BG_layer = 2;
								cur_layer_priority = ppu_BG_Priority[2];
							}
							else if (ppu_BG_Priority[2] < second_layer_priority)
							{
								bg_pixel_s = bg_pixel[2];
								second_BG_layer = 2;
								second_layer_priority = ppu_BG_Priority[2];
							}
						}
					}
					break;

				case 2:
					for (int c2 = 2; c2 < 4; c2++)
					{
						if (BG_Go[c2])
						{
							// calculate rotation and scaling
							ppu_X_RS = ppu_ROT_BG_X[(c2 & 1) * 240 + ppu_Display_Cycle];
							ppu_Y_RS = ppu_ROT_BG_Y[(c2 & 1) * 240 + ppu_Display_Cycle];
							
							// adjust if wraparound is enabled
							if (ppu_BG_Overflow[c2])
							{
								ppu_X_RS &= (BG_Scale_X[c2] - 1);
								ppu_Y_RS &= (BG_Scale_Y[c2] - 1);
							}
						
							// determine if pixel is in valid range, and pick out color if so
							if ((ppu_X_RS >= 0) && (ppu_Y_RS >= 0) && (ppu_X_RS < BG_Scale_X[c2]) && (ppu_Y_RS < BG_Scale_Y[c2]))
							{
								VRAM_ofst_X = ppu_X_RS >> 3;
								VRAM_ofst_Y = ppu_Y_RS >> 3;

								Tile_Addr = ppu_BG_Screen_Base[c2] + VRAM_ofst_Y * BG_Num_Tiles[c2] + VRAM_ofst_X;

								Tile_Addr = VRAM[Tile_Addr];

								Tile_Addr = Tile_Addr * 64 + ppu_BG_Char_Base[c2];

								Tile_Addr = Tile_Addr + (ppu_X_RS & 7) + (ppu_Y_RS & 7) * 8;

								Pixel_Color = (VRAM[Tile_Addr] << 1);

								Pixel_Color_data = (ushort)(PALRAM[Pixel_Color] + (PALRAM[Pixel_Color  + 1] << 8));

								bg_pixel[c2] = (uint)(0xFF000000 |
												((Pixel_Color_data & 0x1F) << 19) |
												((Pixel_Color_data & 0x3E0) << 6) |
												((Pixel_Color_data & 0x7C00) >> 7));

								BG_Is_Transparent[c2] = Pixel_Color == 0;

								BG_Has_Pixel[c2] = true;
							}
							else
							{
								BG_Is_Transparent[c2] = true;
							}
						}

						if (!BG_Is_Transparent[c2])
						{
							if (ppu_BG_Priority[c2] < cur_layer_priority)
							{
								bg_pixel_s = bg_pixel_f;
								second_BG_layer = cur_BG_layer;
								second_layer_priority = cur_layer_priority;

								bg_pixel_f = bg_pixel[c2];
								cur_BG_layer = c2;
								cur_layer_priority = ppu_BG_Priority[c2];
							}
							else if (ppu_BG_Priority[c2] < second_layer_priority)
							{
								bg_pixel_s = bg_pixel[c2];
								second_BG_layer = c2;
								second_layer_priority = ppu_BG_Priority[c2];
							}
						}
					}
					break;

				case 3:
					// Direct Bitmap only, BG2
					if (BG_Go[2])
					{
						// calculate rotation and scaling
						ppu_X_RS = ppu_ROT_BG_X[ppu_Display_Cycle];
						ppu_Y_RS = ppu_ROT_BG_Y[ppu_Display_Cycle];

						if ((ppu_X_RS < 240) && (ppu_Y_RS < 160) && (ppu_X_RS >= 0) && (ppu_Y_RS >= 0))
						{
							// no transparency possible
							cur_BG_layer = 2;
							cur_layer_priority = ppu_BG_Priority[2];
							BG_Has_Pixel[2] = true;

							// pixel color comes direct from VRAM
							int m3_ofst = ppu_X_RS * 2 + ppu_Y_RS * 240 * 2;

							Pixel_Color_data = VRAM[m3_ofst + 1];
							Pixel_Color_data <<= 8;
							Pixel_Color_data |= VRAM[m3_ofst];

							bg_pixel_f = (uint)(0xFF000000 | 
										 ((Pixel_Color_data & 0x1F) << 19) | 
										 ((Pixel_Color_data & 0x3E0) << 6) | 
										 ((Pixel_Color_data & 0x7C00) >> 7));
						}
					}
					break;
				case 4:
					// bitmaps, only BG2
					if (BG_Go[2])
					{
						// calculate rotation and scaling
						ppu_X_RS = ppu_ROT_BG_X[ppu_Display_Cycle];
						ppu_Y_RS = ppu_ROT_BG_Y[ppu_Display_Cycle];

						// pixel color palette direct mapped to VRAM
						if ((ppu_X_RS < 240) && (ppu_Y_RS < 160) && (ppu_X_RS >= 0) && (ppu_Y_RS >= 0))
						{
							// no transparency possible
							cur_BG_layer = 2;
							cur_layer_priority = ppu_BG_Priority[2];
							BG_Has_Pixel[2] = true;

							Pixel_Color = VRAM[ppu_Display_Frame * 0xA000 + ppu_Y_RS * 240 + ppu_X_RS];

							Pixel_Color <<= 1;

							Pixel_Color_data = PALRAM[Pixel_Color + 1];
							Pixel_Color_data <<= 8;
							Pixel_Color_data |= PALRAM[Pixel_Color];

							bg_pixel_f = (uint)(0xFF000000 |
										 ((Pixel_Color_data & 0x1F) << 19) |
										 ((Pixel_Color_data & 0x3E0) << 6) |
										 ((Pixel_Color_data & 0x7C00) >> 7));
						}

					}
					break;
				case 5:
					// bitmaps, only BG2
					if (BG_Go[2])
					{
						// calculate rotation and scaling
						ppu_X_RS = ppu_ROT_BG_X[ppu_Display_Cycle];
						ppu_Y_RS = ppu_ROT_BG_Y[ppu_Display_Cycle];

						// display split into 2 frames, outside of 160 x 128, display backdrop
						if ((ppu_X_RS < 160) && (ppu_Y_RS < 128) && (ppu_X_RS >= 0) && (ppu_Y_RS >= 0))
						{
							// no transparency possible
							cur_BG_layer = 2;
							cur_layer_priority = ppu_BG_Priority[2];
							BG_Has_Pixel[2] = true;

							// pixel color comes direct from VRAM
							int m3_ofst = ppu_X_RS * 2 + ppu_Y_RS * 160 * 2;

							Pixel_Color_data = VRAM[ppu_Display_Frame * 0xA000 + m3_ofst + 1];
							Pixel_Color_data <<= 8;
							Pixel_Color_data |= VRAM[ppu_Display_Frame * 0xA000 + m3_ofst];

							bg_pixel_f = (uint)(0xFF000000 |
										 ((Pixel_Color_data & 0x1F) << 19) |
										 ((Pixel_Color_data & 0x3E0) << 6) |
										 ((Pixel_Color_data & 0x7C00) >> 7));
						}
					}
					break;

				case 6:
					// invalid
					break;

				case 7:
					// invalid
					break;

			}

			// determine final pixel color, based on sprites and special effects
			if (OBJ_Go)
			{
				spr_pixel = ppu_Sprite_Pixels[ppu_Sprite_ofst_draw + ppu_Display_Cycle];

				//Console.WriteLine(ppu_Sprite_Priority[ppu_Sprite_ofst_draw + ppu_Display_Cycle] + " " + cur_layer_priority + " " + cur_BG_layer + " " + ppu_LY);
				// sprite pixel available, check ordering
				if (ppu_Sprite_Priority[ppu_Sprite_ofst_draw + ppu_Display_Cycle] <= cur_layer_priority)
				{
					// sprite pixel has higher priority than BG pixel
					if (ppu_Sprite_Semi_Transparent[ppu_Sprite_ofst_draw + ppu_Display_Cycle])
					{
						// semi transparent pixels with highest priority always enable alpha blending if possible, even if otherwise disabled.
						// alpha blend if possible
						if ((ppu_Special_FX & (1 << (cur_BG_layer + 8))) != 0)
						{
							// Alpha blending Sprite - BG
							bld_pixel_1 = spr_pixel;
							bld_pixel_2 = bg_pixel_f;

							Blend_Final_Pixel = true;
						}
						else
						{
							final_pixel = spr_pixel;

							if ((ppu_SFX_mode >= 2) && ppu_SFX_OBJ_Target_1 && Color_FX_Go)
							{
								Brighten_Final_Pixel = true;
							}
						}
					}
					else if (Color_FX_Go)
					{
						// Alpha blending as normal if enabled
						if (ppu_SFX_mode != 1)
						{
							final_pixel = spr_pixel;

							if ((ppu_SFX_mode != 0) && ppu_SFX_OBJ_Target_1)
							{
								Brighten_Final_Pixel = true;
							}
						}
						else
						{
							// alpha blend if possible
							if (((ppu_Special_FX & (1 << (cur_BG_layer + 8))) != 0) && ppu_SFX_OBJ_Target_1)
							{
								// Alpha blending Sprite - BG
								bld_pixel_1 = spr_pixel;
								bld_pixel_2 = bg_pixel_f;

								Blend_Final_Pixel = true;
							}
							else
							{
								final_pixel = spr_pixel;
							}
						}
					}
					else
					{
						final_pixel = spr_pixel;
					}
				}
				else
				{
					if (Color_FX_Go)
					{
						// BG pixel has higher priority than sprite pixel
						if (ppu_SFX_mode != 1)
						{
							final_pixel = bg_pixel_f;

							if ((ppu_SFX_mode != 0) && ((ppu_Special_FX & (1 << cur_BG_layer)) != 0))
							{
								Brighten_Final_Pixel = true;
							}
						}
						else
						{
							// alpha blend if possible
							// check if another bg layer has a higher priority pixel than the sprite
							if ((ppu_Special_FX & (1 << cur_BG_layer)) != 0)
							{
								if ((second_layer_priority < ppu_Sprite_Priority[ppu_Sprite_ofst_draw + ppu_Display_Cycle]) && ((ppu_Special_FX & (1 << second_BG_layer)) != 0))
								{
									// Alpha blending BG - BG
									bld_pixel_1 = bg_pixel_f;
									bld_pixel_2 = bg_pixel_s;

									Blend_Final_Pixel = true;
								}
								else if ((ppu_Sprite_Priority[ppu_Sprite_ofst_draw + ppu_Display_Cycle] <= second_layer_priority) && ppu_SFX_OBJ_Target_2)
								{
									// Alpha blending BG - Sprite
									bld_pixel_1 = bg_pixel_f;
									bld_pixel_2 = spr_pixel;

									Blend_Final_Pixel = true;
								}
								else
								{
									final_pixel = bg_pixel_f;
								}
							}
							else
							{
								final_pixel = bg_pixel_f;
							}
						}
					}
					else
					{
						final_pixel = bg_pixel_f;
					}
				}
			}
			else
			{
				// only BG pixels available, check brightness and 1st target for special effects
				if (Color_FX_Go)
				{
					if (ppu_SFX_mode != 1)
					{
						final_pixel = bg_pixel_f;

						if ((ppu_SFX_mode >= 2) && ((ppu_Special_FX & (1 << cur_BG_layer)) != 0))
						{
							Brighten_Final_Pixel = true;
						}
					}
					else
					{
						// alpha blend if possible
						// check if the top two layers are targets for blending
						if (((ppu_Special_FX & (1 << cur_BG_layer)) != 0) && ((ppu_Special_FX & (1 << (second_BG_layer + 8))) != 0))
						{
							// Alpha blending BG - BG
							bld_pixel_1 = bg_pixel_f;
							bld_pixel_2 = bg_pixel_s;

							Blend_Final_Pixel = true;
						}
						else
						{
							final_pixel = bg_pixel_f;
						}
					}
				}
				else
				{
					final_pixel = bg_pixel_f;
				}
			}

			if (Brighten_Final_Pixel)
			{
				R = (final_pixel >> 3) & 0x1F;
				G = (final_pixel >> 11) & 0x1F;
				B = (final_pixel >> 19) & 0x1F;

				if (ppu_SFX_mode == 2)
				{
					R = (uint)(R + (((31 - R) * ppu_SFX_BRT_Num) >> 4));
					G = (uint)(G + (((31 - G) * ppu_SFX_BRT_Num) >> 4));
					B = (uint)(B + (((31 - B) * ppu_SFX_BRT_Num) >> 4));
				}
				else
				{
					R = (uint)(R - ((R * ppu_SFX_BRT_Num) >> 4));
					G = (uint)(G - ((G * ppu_SFX_BRT_Num) >> 4));
					B = (uint)(B - ((B * ppu_SFX_BRT_Num) >> 4));
				}

				final_pixel = (uint)(0xFF000000 |
									 (R << 3) |
									 (G << 11) |
									 (B << 19));
			}
			else if (Blend_Final_Pixel)
			{
				R = (bld_pixel_1 >> 3) & 0x1F;
				G = (bld_pixel_1 >> 11) & 0x1F;
				B = (bld_pixel_1 >> 19) & 0x1F;

				R2 = (bld_pixel_2 >> 3) & 0x1F;
				G2 = (bld_pixel_2 >> 11) & 0x1F;
				B2 = (bld_pixel_2 >> 19) & 0x1F;

				R = (uint)(((R * ppu_SFX_Alpha_Num_1) >> 4) + ((R2 * ppu_SFX_Alpha_Num_2) >> 4));
				G = (uint)(((G * ppu_SFX_Alpha_Num_1) >> 4) + ((G2 * ppu_SFX_Alpha_Num_2) >> 4));
				B = (uint)(((B * ppu_SFX_Alpha_Num_1) >> 4) + ((B2 * ppu_SFX_Alpha_Num_2) >> 4));

				if (R > 31) { R = 31; }
				if (G > 31) { G = 31; }
				if (B > 31) { B = 31; }

				final_pixel = (uint)(0xFF000000 |
									 (R << 3) |
									 (G << 11) |
									 (B << 19));
			}

			// push pixel to display
			vid_buffer[ppu_Display_Cycle + ppu_LY * 240] = final_pixel;
		}

		public void ppu_Render_Sprites()
		{
			uint cur_x_pos = 0;
			uint cur_y_pos = 0;
			int spr_tile = 0;
			int x_scale = 0;
			int y_scale = 0;
			int tile_x_offset = 0;
			int tile_y_offset = 0;

			uint pix_color = 0;

			bool double_size = false;
			bool rot_scale = false;
			bool mosaic = false;

			int size_x_offset = 0;
			int size_y_offset = 0;

			int spr_mode = 0;

			int actual_x_index, actual_y_index;

			int base_offset = 0;

			int rel_x_offset, rel_y_offset;

			uint pal_scale = 0;
			int spr_mod;

			// account for processing time of rotated sprites, also the first 6 cycles are not used (probably pipeline reset)
			// when does reading from VRAM / OAM actually happen?
			if (ppu_Sprite_proc_time > 0)
			{
				ppu_Sprite_proc_time -= 1;
				if (ppu_Sprite_proc_time == 0)
				{
					if (ppu_New_Sprite)
					{
						// advance sprite variables if necessary
						ppu_New_Sprite = false;

						ppu_Sprite_Attr_0 = (ushort)(OAM[ppu_Current_Sprite * 8] | (OAM[ppu_Current_Sprite * 8 + 1] << 8));
						ppu_Sprite_Attr_1 = (ushort)(OAM[ppu_Current_Sprite * 8 + 2] | (OAM[ppu_Current_Sprite * 8 + 3] << 8));
						ppu_Sprite_Attr_2 = (ushort)(OAM[ppu_Current_Sprite * 8 + 4] | (OAM[ppu_Current_Sprite * 8 + 5] << 8));

						ppu_Sprite_X_Pos = ppu_Sprite_Attr_1 & 0x1FF;
						ppu_Sprite_Y_Pos = ppu_Sprite_Attr_0 & 0xFF;

						ppu_Sprite_X_Size = ppu_OBJ_Sizes_X[((ppu_Sprite_Attr_1 >> 14) & 3) * 4 + ((ppu_Sprite_Attr_0 >> 14) & 3)];
						ppu_Sprite_Y_Size = ppu_OBJ_Sizes_Y[((ppu_Sprite_Attr_1 >> 14) & 3) * 4 + ((ppu_Sprite_Attr_0 >> 14) & 3)];

						// check if the sprite is in range (in the Y direction)

						// check scaling, only in rotation and scaling mode
						double_size = (ppu_Sprite_Attr_0 & 0x200) == 0x200;
						rot_scale = (ppu_Sprite_Attr_0 & 0x100) == 0x100;

						if (double_size && rot_scale)
						{
							size_y_offset = ppu_Sprite_Y_Size;
							size_x_offset = ppu_Sprite_X_Size;
						}

						// NOTE: double_size means disable in the context of not being in rot_scale mode
						if (double_size && !rot_scale)
						{
							// sprite not displayed
							ppu_Sprite_proc_time = 1;
							ppu_Current_Sprite += 1;
							ppu_New_Sprite = true;

							if (ppu_Current_Sprite == 128)
							{
								ppu_Sprite_Eval_Finished = true;
							}
						}
						else if (ppu_LY != 227)
						{
							// account for screen wrapping
							// if the object would appear at the top of the screen, that is the only part that is drawn
							if (ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset > 0xFF)
							{
								if (((ppu_LY + 1) >= ((ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset) & 0xFF)))
								{
									// sprite vertically out of range
									ppu_Sprite_proc_time = rot_scale ? 10 : 1;
									ppu_Current_Sprite += 1;
									ppu_New_Sprite = true;

									if (ppu_Current_Sprite == 128)
									{
										ppu_Sprite_Eval_Finished = true;
									}
								}
							}
							else
							{
								if (((ppu_LY + 1) < ppu_Sprite_Y_Pos) || ((ppu_LY + 1) >= ((ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset) & 0xFF)))
								{
									// sprite vertically out of range
									ppu_Sprite_proc_time = rot_scale ? 10 : 1;
									ppu_Current_Sprite += 1;
									ppu_New_Sprite = true;

									if (ppu_Current_Sprite == 128)
									{
										ppu_Sprite_Eval_Finished = true;
									}
								}
							}

							cur_y_pos = (uint)((ppu_LY + 1 - ppu_Sprite_Y_Pos) & 0xFF);
						}
						else
						{
							// account for screen wrapping
							// if the object would appear at the top of the screen, that is the only part that is drawn
							if (ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset > 0xFF)
							{
								if (0 >= ((ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset) & 0xFF))
								{
									// sprite vertically out of range
									ppu_Sprite_proc_time = rot_scale ? 10 : 1;
									ppu_Current_Sprite += 1;
									ppu_New_Sprite = true;

									if (ppu_Current_Sprite == 128)
									{
										ppu_Sprite_Eval_Finished = true;
									}
								}
							}
							else
							{
								if ((0 < ppu_Sprite_Y_Pos) || (0 >= ((ppu_Sprite_Y_Pos + ppu_Sprite_Y_Size + size_y_offset) & 0xFF)))
								{
									// sprite vertically out of range
									ppu_Sprite_proc_time = rot_scale ? 10 : 1;
									ppu_Current_Sprite += 1;
									ppu_New_Sprite = true;

									if (ppu_Current_Sprite == 128)
									{
										ppu_Sprite_Eval_Finished = true;
									}
								}
							}

							cur_y_pos = (uint)((0 - ppu_Sprite_Y_Pos) & 0xFF);
						}

						// if the sprite is in range, add it's data to the sprite buffer
						if (!ppu_New_Sprite)
						{
							x_scale = ppu_Sprite_X_Size >> 3;
							y_scale = ppu_Sprite_Y_Size >> 3;

							if ((ppu_Sprite_Attr_0 & 0x100) == 0)
							{
								ppu_Sprite_proc_time = ppu_Sprite_X_Size;
							}
							else
							{
								ppu_Sprite_proc_time = 10 + ((ppu_Sprite_X_Size + size_x_offset) << 1);
							}

							base_offset = ppu_Current_Sprite * 16384;

							rel_y_offset = (int)cur_y_pos;

							spr_mode = (ppu_Sprite_Attr_0 >> 10) & 3;

							// GBA tek says lower bit of tile number should be ignored in some cases, but it ppears this is not the case?
							// more testing needed
							if ((ppu_Sprite_Attr_0 & 0x2000) == 0)
							{
								spr_mod = 0x3FF;
							}
							else
							{
								spr_mod = 0x3FF;
							}

							mosaic = (ppu_Sprite_Attr_0 & 0x1000) == 0x1000;

							for (int i = 0; i < ppu_Sprite_X_Size + size_x_offset; i++)
							{
								cur_x_pos = (uint)((ppu_Sprite_X_Pos + i) & 0x1FF);

								if (mosaic)
								{
									if (ppu_MOS_OBJ_X[cur_x_pos] < ppu_Sprite_X_Pos)
									{
										// lower pixels of sprite not aligned with mosaic grid, nothing to display (in x direction)
										rel_x_offset = 0;
										cur_x_pos = 255;
									}
									else
									{
										// calculate the pixel that is on a grid point, the grid is relative to the screen, not the sprite
										rel_x_offset = (ppu_MOS_OBJ_X[cur_x_pos] - ppu_Sprite_X_Pos) & 0x1FF;
									}

									if (ppu_MOS_OBJ_Y[ppu_LY] < ppu_Sprite_Y_Pos)
									{
										// lower pixels of sprite not aligned with mosaic grid, nothing to display (in y direction)
										rel_y_offset = 0;
										cur_x_pos = 255;
									}
									else
									{
										// calculate the pixel that is on a grid point, the grid is relative to the screen, not the sprite
										rel_y_offset = (ppu_MOS_OBJ_Y[ppu_LY] - ppu_Sprite_Y_Pos) & 0xFF;
									}
								}
								else
								{
									rel_x_offset = i;
								}

								// if sprite is in range horizontally
								if (cur_x_pos < 240)
								{					
									// if the sprite's position is not occupied by a higher priority sprite
									if (!ppu_Sprite_Pixel_Occupied[ppu_Sprite_ofst_eval + cur_x_pos] || (spr_mode == 2) || ppu_Sprite_Semi_Transparent[ppu_Sprite_ofst_eval + cur_x_pos])
									{
										spr_tile = ppu_Sprite_Attr_2 & spr_mod;

										// look up the actual pixel to be used in the sprite rotation tables
										actual_x_index = ppu_ROT_OBJ_X[base_offset + rel_x_offset + rel_y_offset * 128];
										actual_y_index = ppu_ROT_OBJ_Y[base_offset + rel_x_offset + rel_y_offset * 128];

										if ((actual_x_index < ppu_Sprite_X_Size) && (actual_y_index < ppu_Sprite_Y_Size))
										{
											// pick out the tile to use
											if ((ppu_Sprite_Attr_0 & 0x2000) == 0)
											{
												if (ppu_OBJ_Dim)
												{
													spr_tile += (actual_x_index >> 3) + (x_scale) * (int)(actual_y_index >> 3);
												}
												else
												{
													spr_tile += (actual_x_index >> 3) + (0x20) * (int)(actual_y_index >> 3);
												}

												spr_tile <<= 5;

												// pick out the correct pixel from the tile
												tile_x_offset = actual_x_index & 7;
												tile_y_offset = (int)(actual_y_index & 7);

												spr_tile += (tile_x_offset >> 1) + tile_y_offset * 4;

												spr_tile &= 0x7FFF;

												pix_color = (uint)VRAM[0x10000 + spr_tile];

												if ((tile_x_offset & 1) == 0)
												{
													pix_color &= 0xF;
												}
												else
												{
													pix_color = (pix_color >> 4) & 0xF;
												}

												pix_color *= 2;
												pix_color += (uint)(32 * (ppu_Sprite_Attr_2 >> 12));

												pal_scale = 0x1E;
											}
											else
											{
												spr_tile <<= 5;

												if (ppu_OBJ_Dim)
												{
													spr_tile += ((actual_x_index >> 3) + (x_scale) * (int)(actual_y_index >> 3)) << 6;
												}
												else
												{
													spr_tile += ((actual_x_index >> 3) << 6) + ((0x20) * (int)(actual_y_index >> 3) << 5);
												}

												// pick out the correct pixel from the tile
												tile_x_offset = (actual_x_index & 7);
												tile_y_offset = (int)(actual_y_index & 7);

												spr_tile += tile_x_offset + tile_y_offset * 8;

												spr_tile &= 0x7FFF;

												pix_color = (uint)(VRAM[0x10000 + spr_tile] << 1);

												pal_scale = 0x1FE;
											}

											// only allow upper half of vram sprite tiles to be used in modes 3-5
											if ((ppu_BG_Mode >= 3) && (spr_tile < 0x4000))
											{
												pix_color = 0;
											}

											if ((pix_color & pal_scale) != 0)
											{
												if (spr_mode < 2)
												{
													pix_color = (uint)(PALRAM[0x200 + pix_color] + (PALRAM[0x200 + pix_color + 1] << 8));

													pix_color = (uint)(0xFF000000 |
																((pix_color & 0x1F) << 19) |
																((pix_color & 0x3E0) << 6) |
																((pix_color & 0x7C00) >> 7));

													// Sprites of lower priority but can overwrite sprites of higher priority
													if (!ppu_Sprite_Pixel_Occupied[ppu_Sprite_ofst_eval + cur_x_pos] ||
														(((ppu_Sprite_Attr_2 >> 10) & 3) < ppu_Sprite_Priority[ppu_Sprite_ofst_eval + cur_x_pos]))
													{
														ppu_Sprite_Pixels[ppu_Sprite_ofst_eval + cur_x_pos] = pix_color;

														ppu_Sprite_Priority[ppu_Sprite_ofst_eval + cur_x_pos] = (ppu_Sprite_Attr_2 >> 10) & 3;

														ppu_Sprite_Pixel_Occupied[ppu_Sprite_ofst_eval + cur_x_pos] = true;

														ppu_Sprite_Semi_Transparent[ppu_Sprite_ofst_eval + cur_x_pos] = spr_mode == 1;
													}
												}
												else if (spr_mode == 2)
												{
													ppu_Sprite_Object_Window[ppu_Sprite_ofst_eval + cur_x_pos] = true;
												}
											}
										}
									}
								}
							}
						}				
					}
					else
					{
						// finished processing the sprite, go to the next one
						ppu_Sprite_proc_time = 1;
						ppu_Current_Sprite += 1;
						ppu_New_Sprite = true;

						if (ppu_Current_Sprite == 128)
						{
							ppu_Sprite_Eval_Finished = true;
						}
					}
				}
			}
		}

		// all sprites are located in the upper left corner of a 128 x 128 pixel region
		public void ppu_Calculate_Sprites_Pixels(uint addr, bool do_all)
		{
			int set_changed;
			
			if (do_all)
			{
				for (int i = 0; i < 128; i ++)
				{
					ppu_Do_Sprite_Calculation(i);
				}
			}
			else
			{
				// if the effected address is a sprite attribute, only update that one sprite
				// if it is a rotation parameter, update all sprites using that parameter
				if (((addr & 0xF) == 0x6) || ((addr & 0xF) == 0xE))
				{
					set_changed = (int)((addr >> 4) & 0x1F);
					
					for (int i = 0; i < 128; i++)
					{
						// update the sprite rotation / scaling if it is enabled
						if ((OAM[i * 8 + 1] & 0x1) == 1)
						{
							if (((OAM[i * 8 + 3] >> 1) & 0x1F) == set_changed)
							{
								ppu_Do_Sprite_Calculation(i);
							}				
						}
					}
				}
				else
				{
					// update only one sprite
					ppu_Do_Sprite_Calculation((int)((addr >> 3) & 0x7F));
				}
			}
		}

		public void ppu_Do_Sprite_Calculation(int i)
		{
			bool h_flip, v_flip;
			
			int base_ofst = 0;

			uint A, B, C, D;

			int i_A, i_B, i_C, i_D;

			double f_A, f_B, f_C, f_D;

			double fract_part = 0.5;

			double cur_x, cur_y;
			double sol_x, sol_y;

			int param_pick;

			base_ofst = i * 16384;

			ppu_Sprite_Attr_0 = (ushort)(OAM[i * 8] | (OAM[i * 8 + 1] << 8));
			ppu_Sprite_Attr_1 = (ushort)(OAM[i * 8 + 2] | (OAM[i * 8 + 3] << 8));

			ppu_Sprite_X_Size = ppu_OBJ_Sizes_X[((ppu_Sprite_Attr_1 >> 14) & 3) * 4 + ((ppu_Sprite_Attr_0 >> 14) & 3)];
			ppu_Sprite_Y_Size = ppu_OBJ_Sizes_Y[((ppu_Sprite_Attr_1 >> 14) & 3) * 4 + ((ppu_Sprite_Attr_0 >> 14) & 3)];

			if ((OAM[i * 8 + 1] & 0x1) == 1)
			{
				fract_part = 0.5;

				// rotation and scaling enabled
				// pick out parameters
				param_pick = (ppu_Sprite_Attr_1 >> 9) & 0x1F;

				A = (uint)(OAM[0x06 + 0x20 * param_pick] + (OAM[0x06 + 0x20 * param_pick + 1] << 8));
				B = (uint)(OAM[0x0E + 0x20 * param_pick] + (OAM[0x0E + 0x20 * param_pick + 1] << 8));
				C = (uint)(OAM[0x16 + 0x20 * param_pick] + (OAM[0x16 + 0x20 * param_pick + 1] << 8));
				D = (uint)(OAM[0x1E + 0x20 * param_pick] + (OAM[0x1E + 0x20 * param_pick + 1] << 8));

				i_A = (int)((A >> 8) & 0x7F);
				i_B = (int)((B >> 8) & 0x7F);
				i_C = (int)((C >> 8) & 0x7F);
				i_D = (int)((D >> 8) & 0x7F);

				if ((A & 0x8000) == 0x8000) { i_A |= unchecked((int)0xFFFFFF80); }
				if ((B & 0x8000) == 0x8000) { i_B |= unchecked((int)0xFFFFFF80); }
				if ((C & 0x8000) == 0x8000) { i_C |= unchecked((int)0xFFFFFF80); }
				if ((D & 0x8000) == 0x8000) { i_D |= unchecked((int)0xFFFFFF80); }

				// convert to floats
				f_A = i_A;
				f_B = i_B;
				f_C = i_C;
				f_D = i_D;

				for (int j = 7; j >= 0; j--)
				{
					if ((A & (1 << j)) == (1 << j)) { f_A += fract_part; }
					if ((B & (1 << j)) == (1 << j)) { f_B += fract_part; }
					if ((C & (1 << j)) == (1 << j)) { f_C += fract_part; }
					if ((D & (1 << j)) == (1 << j)) { f_D += fract_part; }

					fract_part *= 0.5;
				}

				if (((ppu_Sprite_Attr_0 >> 9) & 0x1) == 1)
				{
					for (int j = 0; j < 2 * ppu_Sprite_X_Size; j++)
					{
						cur_x = j - ppu_Sprite_X_Size;

						for (int k = 0; k < 2 * ppu_Sprite_Y_Size; k++)
						{
							cur_y = -k + ppu_Sprite_Y_Size;

							sol_x = f_A * cur_x - f_B * cur_y;
							sol_y = -f_C * cur_x + f_D * cur_y;

							sol_x += ppu_Sprite_X_Size >> 1;
							sol_y -= ppu_Sprite_Y_Size >> 1;

							sol_y = -sol_y;

							sol_x = Math.Floor(sol_x);
							sol_y = Math.Floor(sol_y);

							ppu_ROT_OBJ_X[base_ofst + j + k * 128] = (ushort)(sol_x);
							ppu_ROT_OBJ_Y[base_ofst + j + k * 128] = (ushort)(sol_y);
						}
					}
				}
				else
				{
					for (int j = 0; j < ppu_Sprite_X_Size; j++)
					{
						cur_x = j - (ppu_Sprite_X_Size >> 1);

						for (int k = 0; k < ppu_Sprite_Y_Size; k++)
						{
							cur_y = -k + (ppu_Sprite_Y_Size >> 1);

							sol_x = f_A * cur_x - f_B * cur_y;
							sol_y = -f_C * cur_x + f_D * cur_y;

							sol_x += ppu_Sprite_X_Size >> 1;
							sol_y -= ppu_Sprite_Y_Size >> 1;

							sol_y = -sol_y;

							sol_x = Math.Floor(sol_x);
							sol_y = Math.Floor(sol_y);

							ppu_ROT_OBJ_X[base_ofst + j + k * 128] = (ushort)(sol_x);
							ppu_ROT_OBJ_Y[base_ofst + j + k * 128] = (ushort)(sol_y);
						}
					}
				}
			}
			else if ((OAM[i * 8 + 1] & 0x2) == 0)
			{
				h_flip = ((ppu_Sprite_Attr_1 & 0x1000) == 0x1000);
				v_flip = ((ppu_Sprite_Attr_1 & 0x2000) == 0x2000);

				for (int j = 0; j < ppu_Sprite_X_Size; j++)
				{
					for (int k = 0; k < ppu_Sprite_Y_Size; k++)
					{
						// horizontal flip
						if (h_flip)
						{
							sol_x = ppu_Sprite_X_Size - 1 - j;
						}
						else
						{
							sol_x = j;
						}

						// vertical flip
						if (v_flip)
						{
							sol_y = ppu_Sprite_Y_Size - 1 - k;
						}
						else
						{
							sol_y = k;
						}

						ppu_ROT_OBJ_X[base_ofst + j + k * 128] = (ushort)sol_x;
						ppu_ROT_OBJ_Y[base_ofst + j + k * 128] = (ushort)sol_y;
					}
				}
			}
		}

		public void ppu_Calculate_BG_Rotations(int layer)
		{
			int base_ofst = 0;

			ushort A, B, C, D;

			int i_A, i_B, i_C, i_D;

			uint Ref_X, Ref_Y;

			int i_ref_x, i_ref_y;

			double f_A, f_B, f_C, f_D;

			double fract_part = 0.5;

			double f_ref_x, f_ref_y;

			double cur_x, cur_y;
			double sol_x, sol_y;

			base_ofst = (layer & 1)  * 240;

			fract_part = 0.5;

			A = ppu_BG_Rot_A[layer];
			B = ppu_BG_Rot_B[layer];
			C = ppu_BG_Rot_C[layer];
			D = ppu_BG_Rot_D[layer];

			i_A = (int)((A >> 8) & 0x7F);
			i_B = (int)((B >> 8) & 0x7F);
			i_C = (int)((C >> 8) & 0x7F);
			i_D = (int)((D >> 8) & 0x7F);

			if ((A & 0x8000) == 0x8000) { i_A |= unchecked((int)0xFFFFFF80); }
			if ((B & 0x8000) == 0x8000) { i_B |= unchecked((int)0xFFFFFF80); }
			if ((C & 0x8000) == 0x8000) { i_C |= unchecked((int)0xFFFFFF80); }
			if ((D & 0x8000) == 0x8000) { i_D |= unchecked((int)0xFFFFFF80); }

			// convert to floats
			f_A = i_A;
			f_B = i_B;
			f_C = i_C;
			f_D = i_D;

			for (int j = 7; j >= 0; j--)
			{
				if ((A & (1 << j)) == (1 << j)) { f_A += fract_part; }
				if ((B & (1 << j)) == (1 << j)) { f_B += fract_part; }
				if ((C & (1 << j)) == (1 << j)) { f_C += fract_part; }
				if ((D & (1 << j)) == (1 << j)) { f_D += fract_part; }

				fract_part *= 0.5;
			}

			fract_part = 0.5;

			Ref_X = ppu_BG_Ref_X[layer];
			Ref_Y = ppu_BG_Ref_Y[layer];

			i_ref_x = (int)((Ref_X >> 8) & 0x7FFFF);
			i_ref_y = (int)((Ref_Y >> 8) & 0x7FFFF);


			if ((Ref_X & 0x8000000) == 0x8000000) { i_ref_x |= unchecked((int)0xFFF80000); }
			if ((Ref_Y & 0x8000000) == 0x8000000) { i_ref_y |= unchecked((int)0xFFF80000); }

			f_ref_x = i_ref_x;
			f_ref_y = i_ref_y;

			for (int j = 7; j >= 0; j--)
			{
				if ((Ref_X & (1 << j)) == (1 << j)) { f_ref_x += fract_part; }
				if ((Ref_Y & (1 << j)) == (1 << j)) { f_ref_y += fract_part; }

				fract_part *= 0.5;
			}

			//Console.WriteLine(layer + " " + f_A + " " + f_B + " " + f_C + " " + f_D + " " + f_ref_x + " " + f_ref_y);

			if (ppu_BG_Mosaic[layer])
			{
				cur_y = -ppu_MOS_BG_Y[ppu_LY - ppu_ROT_REF_LY[layer]];

				for (int j = 0; j < 240; j++)
				{
					cur_x = ppu_MOS_BG_X[j];

					sol_x = f_A * cur_x - f_B * cur_y;
					sol_y = -f_C * cur_x + f_D * cur_y;

					sol_x = Math.Floor(sol_x + f_ref_x);
					sol_y = Math.Floor(-(sol_y - f_ref_y));

					ppu_ROT_BG_X[base_ofst + j] = (ushort)(sol_x);
					ppu_ROT_BG_Y[base_ofst + j] = (ushort)(sol_y);
				}
			}
			else
			{
				cur_y = -(ppu_LY - ppu_ROT_REF_LY[layer]);

				for (int j = 0; j < 240; j++)
				{
					cur_x = j;

					sol_x = f_A * cur_x - f_B * cur_y;
					sol_y = -f_C * cur_x + f_D * cur_y;

					sol_x = Math.Floor(sol_x + f_ref_x);
					sol_y = Math.Floor(-(sol_y - f_ref_y));

					ppu_ROT_BG_X[base_ofst + j] = (ushort)(sol_x);
					ppu_ROT_BG_Y[base_ofst + j] = (ushort)(sol_y);
				}
			}
		}


		public void ppu_Reset()
		{			
			ppu_X_RS = ppu_Y_RS = 0;
			
			ppu_BG_Ref_X[2] = ppu_BG_Ref_X[3] = 0;

			ppu_BG_Ref_Y[2] = ppu_BG_Ref_Y[3] = 0;

			ppu_ROT_REF_LY[2] = ppu_ROT_REF_LY[3] = 0;

			ppu_CTRL = ppu_Green_Swap = 0;

			ppu_VBL_IRQ_cd = ppu_HBL_IRQ_cd = ppu_LYC_IRQ_cd = 0;

			ppu_HBL_Check_cd = ppu_LYC_Check_cd = 0;

			for (int i = 0; i < 4; i++)
			{
				ppu_BG_CTRL[i] = 0;
				ppu_BG_X[0] = 0;
				ppu_BG_Y[0] = 0;

				ppu_BG_On_Update_Time[i] = 0;
			}

			ppu_BG_Rot_A[2] = ppu_BG_Rot_B[2] = ppu_BG_Rot_C[2] = ppu_BG_Rot_D[2] = 0;

			ppu_BG_Rot_A[3] = ppu_BG_Rot_B[3] = ppu_BG_Rot_C[3] = ppu_BG_Rot_D[3] = 0;

			ppu_WIN_Hor_0 = ppu_WIN_Hor_1 = ppu_WIN_Vert_0 = ppu_WIN_Vert_1 = 0;

			ppu_WIN_In = ppu_WIN_Out = ppu_Mosaic = ppu_Special_FX = ppu_Alpha = ppu_Bright = 0;

			ppu_STAT = 0;

			ppu_LYC = 0xFF;

			// based on music4.gba, initial state would either be Ly = 225 or 161.
			// based on console verification testing, it seems 161 is correct.
			ppu_LY = 161;

			ppu_Sprite_Line = 0;

			// 1 gives the correct value in music4.gba
			ppu_Cycle = 1;

			ppu_Display_Cycle = 0;

			ppu_In_VBlank = true;

			ppu_Delays = false;

			// reset sprite evaluation variables
			ppu_Current_Sprite = 0;
			ppu_New_Sprite = true;
			ppu_Sprite_Eval_Finished = false;

			for (int i = 0; i < 240 * 2; i++) 
			{ 
				ppu_Sprite_Pixels[i] = 0;
				ppu_Sprite_Priority[i] = 0;
				ppu_Sprite_Pixel_Occupied[i] = false;
				ppu_Sprite_Semi_Transparent[i] = false;
				ppu_Sprite_Object_Window[i] = false;
			}

			ppu_Sprite_Attr_0 = ppu_Sprite_Attr_1 = ppu_Sprite_Attr_2 = 0;

			ppu_Current_Sprite = ppu_Sprite_ofst_eval = 0;
			ppu_Sprite_ofst_draw = 240;

			ppu_Sprite_proc_time = 6;

			ppu_Sprite_X_Pos = ppu_Sprite_Y_Pos = ppu_Sprite_X_Size = ppu_Sprite_Y_Size = 0;

			// PPU power up
			ppu_CTRL_Write(0);

			// update derived values
			ppu_Calc_Win0();
			ppu_Calc_Win1();
			ppu_Update_Win_In(0);
			ppu_Update_Win_Out(0);
			ppu_BG_CTRL_Write(0);
			ppu_BG_CTRL_Write(1);
			ppu_BG_CTRL_Write(2);
			ppu_BG_CTRL_Write(3);
			ppu_Update_Special_FX(0);
			ppu_Update_Alpha(0);
			ppu_Update_Bright(0);
			ppu_Update_Mosaic(0);
		}

		public void ppu_SyncState(Serializer ser)
		{
			ser.Sync(nameof(ppu_BG_CTRL), ref ppu_BG_CTRL, false);
			ser.Sync(nameof(ppu_BG_X), ref ppu_BG_X, false);
			ser.Sync(nameof(ppu_BG_Y), ref ppu_BG_Y, false);

			ser.Sync(nameof(ppu_BG_Ref_X), ref ppu_BG_Ref_X, false);
			ser.Sync(nameof(ppu_BG_Ref_Y), ref ppu_BG_Ref_Y, false);

			ser.Sync(nameof(ppu_ROT_REF_LY), ref ppu_ROT_REF_LY, false);

			ser.Sync(nameof(ppu_BG_On_Update_Time), ref ppu_BG_On_Update_Time, false);

			ser.Sync(nameof(ppu_BG_Rot_A), ref ppu_BG_Rot_A, false);
			ser.Sync(nameof(ppu_BG_Rot_B), ref ppu_BG_Rot_B, false);
			ser.Sync(nameof(ppu_BG_Rot_C), ref ppu_BG_Rot_C, false);
			ser.Sync(nameof(ppu_BG_Rot_D), ref ppu_BG_Rot_D, false);

			ser.Sync(nameof(ppu_BG_On), ref ppu_BG_On, false);
			ser.Sync(nameof(ppu_BG_On_New), ref ppu_BG_On_New, false);

			ser.Sync(nameof(ppu_BG_Mode), ref ppu_BG_Mode);
			ser.Sync(nameof(ppu_Display_Frame), ref ppu_Display_Frame);

			ser.Sync(nameof(ppu_X_RS), ref ppu_X_RS);
			ser.Sync(nameof(ppu_Y_RS), ref ppu_Y_RS);

			ser.Sync(nameof(ppu_VBL_IRQ_cd), ref ppu_VBL_IRQ_cd);
			ser.Sync(nameof(ppu_HBL_IRQ_cd), ref ppu_HBL_IRQ_cd);
			ser.Sync(nameof(ppu_LYC_IRQ_cd), ref ppu_LYC_IRQ_cd);

			ser.Sync(nameof(ppu_HBL_Check_cd), ref ppu_HBL_Check_cd);
			ser.Sync(nameof(ppu_LYC_Check_cd), ref ppu_LYC_Check_cd);

			ser.Sync(nameof(ppu_CTRL), ref ppu_CTRL);
			ser.Sync(nameof(ppu_Green_Swap), ref ppu_Green_Swap);
			ser.Sync(nameof(ppu_Cycle), ref ppu_Cycle);
			ser.Sync(nameof(ppu_Display_Cycle), ref ppu_Display_Cycle);
			ser.Sync(nameof(ppu_Sprite_Eval_Time), ref ppu_Sprite_Eval_Time);

			ser.Sync(nameof(ppu_Transparent_Color), ref ppu_Transparent_Color);

			ser.Sync(nameof(ppu_WIN_Hor_0), ref ppu_WIN_Hor_0);
			ser.Sync(nameof(ppu_WIN_Hor_1), ref ppu_WIN_Hor_1);
			ser.Sync(nameof(ppu_WIN_Vert_0), ref ppu_WIN_Vert_0);
			ser.Sync(nameof(ppu_WIN_Vert_1), ref ppu_WIN_Vert_1);

			ser.Sync(nameof(ppu_WIN_In), ref ppu_WIN_In);
			ser.Sync(nameof(ppu_WIN_Out), ref ppu_WIN_Out);
			ser.Sync(nameof(ppu_Mosaic), ref ppu_Mosaic);
			ser.Sync(nameof(ppu_Special_FX), ref ppu_Special_FX);
			ser.Sync(nameof(ppu_Alpha), ref ppu_Alpha);
			ser.Sync(nameof(ppu_Bright), ref ppu_Bright);

			ser.Sync(nameof(ppu_STAT), ref ppu_STAT);
			ser.Sync(nameof(ppu_LYC), ref ppu_LYC);
			ser.Sync(nameof(ppu_LY), ref ppu_LY);
			ser.Sync(nameof(ppu_Sprite_Line), ref ppu_Sprite_Line);

			ser.Sync(nameof(ppu_HBL_Free), ref ppu_HBL_Free);
			ser.Sync(nameof(ppu_OBJ_Dim), ref ppu_OBJ_Dim);
			ser.Sync(nameof(ppu_Forced_Blank), ref ppu_Forced_Blank);
			ser.Sync(nameof(ppu_Any_Window_On), ref ppu_Any_Window_On);
			ser.Sync(nameof(ppu_OBJ_On), ref ppu_OBJ_On);
			ser.Sync(nameof(ppu_WIN0_On), ref ppu_WIN0_On);
			ser.Sync(nameof(ppu_WIN1_On), ref ppu_WIN1_On);
			ser.Sync(nameof(ppu_OBJ_WIN), ref ppu_OBJ_WIN);

			ser.Sync(nameof(ppu_In_VBlank), ref ppu_In_VBlank);
			ser.Sync(nameof(ppu_Delays), ref ppu_Delays);

			ser.Sync(nameof(ppu_Sprite_Pixels), ref ppu_Sprite_Pixels, false);
			ser.Sync(nameof(ppu_Sprite_Priority), ref ppu_Sprite_Priority, false);
			ser.Sync(nameof(ppu_Sprite_Pixel_Occupied), ref ppu_Sprite_Pixel_Occupied, false);
			ser.Sync(nameof(ppu_Sprite_Semi_Transparent), ref ppu_Sprite_Semi_Transparent, false);
			ser.Sync(nameof(ppu_Sprite_Object_Window), ref ppu_Sprite_Object_Window, false);

			ser.Sync(nameof(ppu_Current_Sprite), ref ppu_Current_Sprite);
			ser.Sync(nameof(ppu_Sprite_ofst_eval), ref ppu_Sprite_ofst_eval);
			ser.Sync(nameof(ppu_Sprite_ofst_draw), ref ppu_Sprite_ofst_draw);
			ser.Sync(nameof(ppu_Sprite_proc_time), ref ppu_Sprite_proc_time);

			ser.Sync(nameof(ppu_Sprite_X_Pos), ref ppu_Sprite_X_Pos);
			ser.Sync(nameof(ppu_Sprite_Y_Pos), ref ppu_Sprite_Y_Pos);
			ser.Sync(nameof(ppu_Sprite_X_Size), ref ppu_Sprite_X_Size);
			ser.Sync(nameof(ppu_Sprite_Y_Size), ref ppu_Sprite_Y_Size);

			ser.Sync(nameof(ppu_Sprite_Attr_0), ref ppu_Sprite_Attr_0);
			ser.Sync(nameof(ppu_Sprite_Attr_1), ref ppu_Sprite_Attr_1);
			ser.Sync(nameof(ppu_Sprite_Attr_2), ref ppu_Sprite_Attr_2);

			ser.Sync(nameof(ppu_New_Sprite), ref ppu_New_Sprite);
			ser.Sync(nameof(ppu_Sprite_Eval_Finished), ref ppu_Sprite_Eval_Finished);

			// update derived values
			ppu_Calc_Win0();
			ppu_Calc_Win1();
			ppu_Update_Win_In(ppu_WIN_In);
			ppu_Update_Win_Out(ppu_WIN_Out);
			ppu_BG_CTRL_Write(0);
			ppu_BG_CTRL_Write(1);
			ppu_BG_CTRL_Write(2);
			ppu_BG_CTRL_Write(3);
			ppu_Update_Special_FX(ppu_Special_FX);
			ppu_Update_Alpha(ppu_Alpha);
			ppu_Update_Bright(ppu_Bright);
			ppu_Update_Mosaic(ppu_Mosaic);
		}
	}

#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
}
