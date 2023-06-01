﻿using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBALink
{
	public partial class GBAHawkLink : IInputPollable
	{
		public int LagCount
		{
			get => _lagCount;
			set => _lagCount = value;
		}

		public bool IsLagFrame
		{
			get => _isLag;
			set => _isLag = value;
		}

		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		public int _lagCount = 0;
		public bool _isLag = false;
	}
}
