using Silk.NET.Core.Native;
using Silk.NET.OpenAL;
using StbVorbisSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using static Shergen.Lua.Native;

namespace Shergen
{
    public class Audio : IDisposable
    {
        private AL al;
        private ALContext alc;
        private unsafe Device* device;
        private unsafe Context* context;

        public static Audio Instance { get; private set; }

        private Vorbis _vorbis;

        public class AudioChannel
        {
            public int ID;
            public float Volume = 1.0f;  // 0-1000 mapped
            public bool Is3D = false;
            public float MinDistance = 1.0f;
            public float MaxDistance = 100.0f;
        }

        private List<AudioChannel> channels = new();
        private int nextChannelID = 1;

        public class AudioSource
        {
            public uint SourceID;
            public int ChannelID;
            public bool Loop;
            public bool Finished;
            public LuaCallback Callback;
        }

        private List<AudioSource> activeSources = new();

        private Thread pollThread;
        private volatile bool pollRunning;
        public ConcurrentQueue<(LuaCallback cb, uint sourceID)> FinishedCallbacks = new();

        public unsafe void Init()
        {
            alc = ALContext.GetApi();
            al = AL.GetApi();

            device = alc.OpenDevice(null); // null = default device
            context = alc.CreateContext(device, null);
            alc.MakeContextCurrent(context);

            al.SetListenerProperty(ListenerFloat.Gain, 1.0f);

            al.SetListenerProperty(ListenerVector3.Position, new Vector3(0, 0, 0));
            al.SetListenerProperty(ListenerVector3.Velocity, new Vector3(0, 0, 0));


            pollRunning = true;
            pollThread = new Thread(() =>
            {
                while (pollRunning)
                {
                    lock (activeSources)
                    {
                        foreach (var src in activeSources)
                        {
                            if (src.Finished) continue;
                            al.GetSourceProperty(src.SourceID, GetSourceInteger.SourceState, out int state);
                            if (state == (int)SourceState.Stopped)
                            {
                                src.Finished = true;
                                if (!src.Callback.IsEmpty)
                                    FinishedCallbacks.Enqueue((src.Callback, src.SourceID));
                            }
                        }
                    }
                    Thread.Sleep(50);
                }
            })
            { IsBackground = true };
            pollThread.Start();
            Console.WriteLine("🔊 Audio Inicialized");

            Instance = this;
        }

        // ///////////////////////////////////////////////////////////////
        public int LoadSound(string path)
        {
            uint buffer = al.GenBuffer();

            string ext = Path.GetExtension(path).ToLower();
            try
            {
                if (ext == ".wav")
                    LoadWav(path, buffer);
                else if (ext == ".ogg")
                    LoadOgg(path, buffer);
                Console.WriteLine($"[AUDIO] File {path} loaded.");
                return (int)buffer;
            } catch (Exception e) 
            { Console.WriteLine("[AUDIO] ❌ " + e.Message);
                return (int)-1;
            }
        }
        public uint PlaySound(int bufferId)
        {
            uint source = al.GenSource();
            al.SetSourceProperty(source, SourceFloat.Gain, 1.0f);
            al.SetSourceProperty(source, SourceInteger.Buffer, bufferId);

            al.SetSourceProperty(source, SourceVector3.Position, new Vector3(0, 0, 0));
            al.SetSourceProperty(source, SourceBoolean.SourceRelative, true);


            al.SourcePlay(source);
            al.GetSourceProperty(source, GetSourceInteger.SourceState, out int state);

            return source;
        }

        // ///////////////////////////////////////////////////////////////

        void LoadWav(string path, uint buffer)
        {
            // Load WAV file using StbImageSharp

            var bytes = FileManager.ReadAllBytes(path);

            int channels = BitConverter.ToInt16(bytes, 22);
            int sampleRate = BitConverter.ToInt32(bytes, 24);
            int bitsPerSample = BitConverter.ToInt16(bytes, 34);

            int dataOffset = 44;
            for (int i = 12; i < bytes.Length - 4; i++)
            {
                if (bytes[i] == 'd' && bytes[i + 1] == 'a' && bytes[i + 2] == 't' && bytes[i + 3] == 'a')
                {
                    dataOffset = i + 8; // 4 bytes "data" + 4 bytes size
                    break;
                }
            }

            byte[] pcm = bytes[dataOffset..];

            BufferFormat format = (channels == 1)
                ? (bitsPerSample == 8 ? BufferFormat.Mono8 : BufferFormat.Mono16)
                : (bitsPerSample == 8 ? BufferFormat.Stereo8 : BufferFormat.Stereo16);

            al.BufferData<byte>(buffer, format, pcm, sampleRate);


            var err = al.GetError();
        }

        void LoadOgg(string path, uint buffer)
        {
            var bytes = FileManager.ReadAllBytes(path);

            var result = StbVorbis.decode_vorbis_from_memory(bytes, out int sampleRate, out int channels);

            BufferFormat format = channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

            al.BufferData<short>(buffer, format, result, sampleRate);
        }




        // -- Audio manager for Lua

        public int AddChannel()
        {
            var ch = new AudioChannel { ID = nextChannelID++ };
            channels.Add(ch);
            return ch.ID;
        }

        private AudioChannel GetChannel(int id)
            => channels.FirstOrDefault(c => c.ID == id);

        public void SetChannelVolume(int channelID, int volume)
        {
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            ch.Volume = Math.Clamp(volume, 0, 1500) / 1000f; ;

            lock (activeSources)
            {
                foreach (var src in activeSources)
                {
                    if (src.ChannelID == channelID)
                        al.SetSourceProperty(src.SourceID, SourceFloat.Gain, ch.Volume);
                }
            }
        }

        public int GetChannelVolume(int channelID)
        {
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            return ch != null ? (int)(ch.Volume * 1000f) : 0;
        }

        public void SetChannel3D(int channelID, bool value)
        {
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            ch.Is3D = value;
        }

        public bool GetChannel3D(int channelID)
        { 
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            return ch.Is3D;
        }

        public void SetChannelDistance(int channelID, int minDistance,  int maxDistance)
        {
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            ch.MinDistance = minDistance;
            ch.MaxDistance = maxDistance;

        }

        public (int min, int max) GetChannelDistance(int channelID)
        {
            var ch = GetChannel(channelID);
            if (ch == null) throw new InvalidOperationException($"Request channel (ID {channelID}) not found.");
            return ((int)ch.MinDistance, (int)ch.MaxDistance);
        }


        public int LoadSound(string path, int channelID, bool stream = false, bool loop = false, int pitch = 100, LuaCallback callback = default)
        {

            uint buffer = al.GenBuffer();
            string ext = Path.GetExtension(path).ToLower();
            if (ext == ".wav") LoadWav(path, buffer);
            else if (ext == ".ogg") LoadOgg(path, buffer);

            uint source = al.GenSource();
            var ch = GetChannel(channelID);

            al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
            al.SetSourceProperty(source, SourceFloat.Gain, ch?.Volume ?? 1.0f);
            al.SetSourceProperty(source, SourceBoolean.Looping, loop);
            al.SetSourceProperty(source, SourceFloat.Pitch, pitch / 100f);
            al.SetSourceProperty(source, SourceBoolean.SourceRelative, !(ch?.Is3D ?? false));
            al.SetSourceProperty(source, SourceFloat.MaxGain, 2.0f);
            activeSources.Add(new AudioSource
            {
                SourceID = source,
                ChannelID = channelID,
                Loop = loop,
                Callback = callback,
            });

            return (int)source;
        }

        public void PlaySound(int sourceID, bool fromStart = true)
        {
            var src = activeSources.FirstOrDefault(s => s.SourceID == (uint)sourceID);
            if (src != null) src.Finished = false;

            if (fromStart) al.SourceRewind((uint)sourceID);
            al.SourcePlay((uint)sourceID);
        }

        public void FreeSound(int sourceID)
        {
            uint src = (uint)sourceID;
            al.GetSourceProperty(src, GetSourceInteger.Buffer, out int bufferID);
            al.SourceStop(src);
            al.DeleteSource(src);
            if (bufferID > 0) al.DeleteBuffer((uint)bufferID);
            lock (activeSources)
                activeSources.RemoveAll(s => s.SourceID == src);
        }

        public void PauseSound(int sourceID)
        {
            al.SourcePause((uint)sourceID);
        }

        public void StopSound(int sourceID)
        {
            al.SourceStop((uint)sourceID);
            lock (activeSources)
            {
                var src = activeSources.FirstOrDefault(s => s.SourceID == (uint)sourceID);
                if (src != null) src.Finished = true;
            }
        }

        public bool IsPlaying(int sourceID)
        {
            al.GetSourceProperty((uint)sourceID, GetSourceInteger.SourceState, out int state);
            return state == (int)SourceState.Playing;
        }

        public float GetSoundTime(int sourceID)
        {
            al.GetSourceProperty((uint)sourceID, SourceFloat.SecOffset, out float seconds);
            return seconds;
        }

        public void SetSoundTime(int sourceID, float seconds)
        {
            al.SetSourceProperty((uint)sourceID, SourceFloat.SecOffset, seconds);
        }

        public void SetSoundPosition(int sourceID, float x, float y, float z)
        {
            al.SetSourceProperty((uint)sourceID, SourceVector3.Position, new Vector3(x, y, z));
        }

        public void SetSoundVelocity(int sourceID, float x, float y, float z)
        {
            al.SetSourceProperty((uint)sourceID, SourceVector3.Velocity, new Vector3(x, y, z));
        }

        public void SetSoundPitch(int sourceID, int pitch)
        {
            al.SetSourceProperty((uint)sourceID, SourceFloat.Pitch, pitch / 100f);
        }

        public void SetSoundLoop(int sourceID, bool loop)
        {
            al.SetSourceProperty((uint)sourceID, SourceBoolean.Looping, loop);
            lock (activeSources)
            {
                var src = activeSources.FirstOrDefault(s => s.SourceID == (uint)sourceID);
                if (src != null) src.Loop = loop;
            }
        }

        // Lua bridge

        public static int AddChannel(IntPtr L)
        {
            PushLuaInteger(L, Instance.AddChannel());
            return 1;
        }

        public static int SetChannelVolume(IntPtr L)
        {
            Instance.SetChannelVolume(ToLuaInteger(L, 1), ToLuaInteger(L, 2));
            return 0;
        }

        public static int GetChannelVolume(IntPtr L)
        {
            PushLuaInteger(L, Instance.GetChannelVolume(ToLuaInteger(L, 1)));
            return 1;
        }

        public static int SetChannel3D(IntPtr L)
        {
            Instance.SetChannel3D(ToLuaInteger(L, 1), ToLuaBoolean(L, 2));
            return 0;
        }

        public static int GetChannel3D(IntPtr L)
        {
            PushLuaBoolean(L, Instance.GetChannel3D(ToLuaInteger(L, 1)));
            return 1;
        }

        public static int SetChannelDistance(IntPtr L)
        {
            Instance.SetChannelDistance(ToLuaInteger(L, 1), ToLuaInteger(L, 2), ToLuaInteger(L, 3));
            return 0;
        }

        public static int GetChannelDistance(IntPtr L)
        {
            var (min, max) = Instance.GetChannelDistance(ToLuaInteger(L, 1));
            PushLuaInteger(L, min);
            PushLuaInteger(L, max);
            return 2;
        }


        public static int LoadSound(IntPtr L)
        {
            try
            {
                string path = ToLuaString(L, 1);
                int channelID = GetTop(L) >= 2 && !IsLuaNil(L, 2) ? ToLuaInteger(L, 2) : 0;
                bool stream = GetTop(L) >= 3 && !IsLuaNil(L, 3) && ToLuaBoolean(L, 3);
                bool loop = GetTop(L) >= 4 && !IsLuaNil(L, 4) && ToLuaBoolean(L, 4);
                int pitch = GetTop(L) >= 5 && !IsLuaNil(L, 5) ? ToLuaInteger(L, 5) : 100;

                LuaCallback cb = default;
                if (GetTop(L) >= 6 && !IsLuaNil(L, 6))
                    cb = Utils.ReadCallback(L, 6);

                int id = Instance.LoadSound(path, channelID, stream, loop, pitch, cb);
                PushLuaInteger(L, id);
                return 1;
            }
            catch (Exception e)
            {
                //PushLuaError(L, $"[AUDIO] LoadSound: {e.Message}");
                Console.WriteLine($"❌ [AUDIO] LoadSound: {e.Message}");
                return 0;
            }
        }

        public static int PlaySound(IntPtr L)
        {
            int sourceID = ToLuaInteger(L, 1);
            bool fromStart = GetTop(L) >= 2 && !IsLuaNil(L, 2) && ToLuaBoolean(L, 2);
            Instance.PlaySound(sourceID, fromStart);
            return 0;
        }

        public static int PauseSound(IntPtr L)
        {
            Instance.PauseSound(ToLuaInteger(L, 1));
            return 0;
        }

        public static int StopSound(IntPtr L)
        {
            Instance.StopSound(ToLuaInteger(L, 1));
            return 0;
        }

        public static int FreeSound(IntPtr L)
        {
            Instance.FreeSound(ToLuaInteger(L, 1));
            return 0;
        }

        public static int IsPlaying(IntPtr L)
        {
            bool playing = Instance.IsPlaying(ToLuaInteger(L, 1));
            PushLuaBoolean(L, playing);
            return 1;
        }

        public static int GetSoundTime(IntPtr L)
        {
            float time = Instance.GetSoundTime(ToLuaInteger(L, 1));
            PushLuaNumber(L, time);
            return 1;
        }

        public static int SetSoundTime(IntPtr L)
        {
            Instance.SetSoundTime(ToLuaInteger(L, 1), (float)ToLuaNumber(L, 2));
            return 0;
        }

        public static int SetSoundPosition(IntPtr L)
        {
            Instance.SetSoundPosition(ToLuaInteger(L, 1), (float)ToLuaNumber(L, 2), (float)ToLuaNumber(L, 3), (float)ToLuaNumber(L, 4));
            return 0;
        }

        public static int SetSoundVelocity(IntPtr L)
        {
            Instance.SetSoundVelocity(ToLuaInteger(L, 1), (float)ToLuaNumber(L, 2), (float)ToLuaNumber(L, 3), (float)ToLuaNumber(L, 4));
            return 0;
        }

        public static int SetSoundPitch(IntPtr L)
        {
            Instance.SetSoundPitch(ToLuaInteger(L, 1), ToLuaInteger(L, 2));
            return 0;
        }

        public static int SetSoundLoop(IntPtr L)
        {
            Instance.SetSoundLoop(ToLuaInteger(L, 1), ToLuaBoolean(L, 2));
            return 0;
        }


        //dispose
        public unsafe void Dispose()
        {
            if (context != null)
            {
                alc.DestroyContext(context);
            }
            if (device != null)
            {
                alc.CloseDevice(device);
            }
            context = null;
            device = null;
        }



    }
}
