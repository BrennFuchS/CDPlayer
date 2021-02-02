using System;
using System.IO;
using MSCLoader;
using AudioLibrary;
using UnityEngine;

namespace CDplayer
{
    class AudioImport
    {
		public static AudioClip LoadAudioFromFile(string path, bool doStream, bool background)
		{
			Stream dataStream = new MemoryStream(File.ReadAllBytes(path));
			AudioFormat audioFormat = Manager.GetAudioFormat(path);
			string fileName = Path.GetFileName(path);
			if (audioFormat == AudioFormat.unknown)
			{
				audioFormat = AudioFormat.mp3;
			}
			try
			{
				return Manager.Load(dataStream, audioFormat, fileName, doStream, background, true);
			}
			catch (Exception ex)
			{
				ModConsole.Error(ex.Message);
				if (ModLoader.devMode)
				{
					ModConsole.Error(ex.ToString());
				}
				Console.WriteLine(ex);
				return null;
			}
		}
	}
}
