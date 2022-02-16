namespace dotNES
{
    public static class ConfigWrapper
    {
		private static CCfgSound cfgSound = new CCfgSound();
		public static CCfgSound GetCCfgSound()
        {
			return cfgSound;
		}
	}

    public class CCfgSound
    {
		public bool bEnable;
		public int nRate;
		public int nBits;
		public int nBufferSize;
		public int nFilterType;
		public bool bChangeTone;
		public bool bDisableVolumeEffect;
		public bool bExtraSoundEnable;

		//  0:Master
		//  1:Rectangle 1
		//  2:Rectangle 2
		//  3:Triangle
		//  4:Noise
		//  5:DPCM
		//  6:VRC6
		//  7:VRC7
		//  8:FDS
		//  9:MMC5
		// 10:N106
		// 11:FME7
		public short[] nVolume = new short[16];

		public CCfgSound()
        {
			bEnable = true;
			nRate = 22050;
			nBits = 16;
			nBufferSize = 4;

			nFilterType = 0;

			bChangeTone = false;

			bDisableVolumeEffect = false;
			bExtraSoundEnable = true;

			for (int i = 0; i < 16; i++)
			{
				nVolume[i] = 100;
			}
		}
	}
}
