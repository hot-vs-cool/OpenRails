﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ORTS
{
	public class Profiler
	{
		public readonly string Name;
		public SmoothedData Wall { get; private set; }
		public SmoothedData CPU { get; private set; }
		public SmoothedData Wait { get; private set; }
		readonly Stopwatch TimeTotal;
		readonly Stopwatch TimeRunning;
		TimeSpan TimeCPU;
		TimeSpan LastCPU;
		readonly ProcessThread ProcessThread;

		public Profiler(string name)
		{
			Name = name;
			Wall = new SmoothedData();
			CPU = new SmoothedData();
			Wait = new SmoothedData();
			TimeTotal = new Stopwatch();
			TimeRunning = new Stopwatch();
			foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
			{
				if (thread.Id == AppDomain.GetCurrentThreadId())
				{
					ProcessThread = thread;
					break;
				}
			}
			TimeTotal.Start();
		}

		public void Start()
		{
			TimeRunning.Start();
			LastCPU = ProcessThread.TotalProcessorTime;
		}

		public void Stop()
		{
			TimeRunning.Stop();
			TimeCPU += ProcessThread.TotalProcessorTime - LastCPU;
		}

		public void Mark()
		{
			// Stop timers.
			var running = TimeRunning.IsRunning;
			TimeTotal.Stop();
			TimeRunning.Stop();
			// Calculate the Wall and CPU times from timers.
			Wall.Update(TimeTotal.ElapsedMilliseconds / 1000f, 100f * (float)TimeRunning.ElapsedMilliseconds / (float)TimeTotal.ElapsedMilliseconds);
			CPU.Update(TimeTotal.ElapsedMilliseconds / 1000f, 100f * (float)TimeCPU.TotalMilliseconds / (float)TimeTotal.ElapsedMilliseconds);
			Wait.Update(TimeTotal.ElapsedMilliseconds / 1000f, Math.Max(0, Wall.Value - CPU.Value));
			// Resume timers.
			TimeTotal.Reset();
			TimeRunning.Reset();
			TimeCPU = TimeSpan.Zero;
			TimeTotal.Start();
			if (running) TimeRunning.Start();
			LastCPU = ProcessThread.TotalProcessorTime;
		}
	}
}
